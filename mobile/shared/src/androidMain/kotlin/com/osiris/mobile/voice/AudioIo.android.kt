package com.osiris.mobile.voice

import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioRecord
import android.media.AudioTrack
import android.media.MediaRecorder
import android.media.audiofx.AcousticEchoCanceler
import android.media.audiofx.AutomaticGainControl
import android.media.audiofx.NoiseSuppressor
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicBoolean

private const val CAPTURE_SAMPLE_RATE = 16_000
private const val PLAYBACK_SAMPLE_RATE = 24_000

/**
 * Android microphone capture: PCM16 mono 16 kHz via [AudioRecord], sourced from VOICE_COMMUNICATION so
 * the platform applies its echo canceller against the assistant's playback. AEC/NS/AGC effects are
 * attached to the record session when available so the model does not hear its own voice.
 */
actual class VoiceAudioCapture actual constructor() {
    private val running = AtomicBoolean(false)
    private var record: AudioRecord? = null
    private var thread: Thread? = null
    private var aec: AcousticEchoCanceler? = null
    private var noiseSuppressor: NoiseSuppressor? = null
    private var gainControl: AutomaticGainControl? = null

    actual fun start(onChunk: (ByteArray) -> Unit) {
        if (running.get()) return

        val minBuffer = AudioRecord.getMinBufferSize(
            CAPTURE_SAMPLE_RATE,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
        )
        val bufferSize = maxOf(minBuffer, CAPTURE_SAMPLE_RATE) // >= ~0.5s of headroom

        @Suppress("MissingPermission") // RECORD_AUDIO is requested at runtime before this is called.
        val recorder = AudioRecord(
            MediaRecorder.AudioSource.VOICE_COMMUNICATION,
            CAPTURE_SAMPLE_RATE,
            AudioFormat.CHANNEL_IN_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
            bufferSize,
        )
        if (recorder.state != AudioRecord.STATE_INITIALIZED) {
            recorder.release()
            throw IllegalStateException("Microfone indisponível.")
        }

        enableEffects(recorder.audioSessionId)
        record = recorder
        running.set(true)
        recorder.startRecording()

        val frameBytes = CAPTURE_SAMPLE_RATE / 10 * 2 // ~100 ms PCM16 frame
        thread = Thread {
            val buffer = ByteArray(frameBytes)
            while (running.get()) {
                val read = recorder.read(buffer, 0, buffer.size)
                if (read > 0) {
                    onChunk(buffer.copyOf(read))
                }
            }
        }.apply {
            isDaemon = true
            name = "osiris-voice-capture"
            start()
        }
    }

    actual fun stop() {
        running.set(false)
        thread?.let { runCatching { it.join(250) } }
        thread = null
        record?.let { recorder ->
            runCatching { if (recorder.recordingState == AudioRecord.RECORDSTATE_RECORDING) recorder.stop() }
            runCatching { recorder.release() }
        }
        record = null
        runCatching { aec?.release() }; aec = null
        runCatching { noiseSuppressor?.release() }; noiseSuppressor = null
        runCatching { gainControl?.release() }; gainControl = null
    }

    private fun enableEffects(sessionId: Int) {
        if (AcousticEchoCanceler.isAvailable()) {
            aec = AcousticEchoCanceler.create(sessionId)?.apply { enabled = true }
        }
        if (NoiseSuppressor.isAvailable()) {
            noiseSuppressor = NoiseSuppressor.create(sessionId)?.apply { enabled = true }
        }
        if (AutomaticGainControl.isAvailable()) {
            gainControl = AutomaticGainControl.create(sessionId)?.apply { enabled = true }
        }
    }
}

/**
 * Android playback: PCM16 mono 24 kHz via a streaming [AudioTrack]. Chunks are queued and drained by a
 * dedicated worker thread (AudioTrack.write blocks), routed through VOICE_COMMUNICATION so it pairs with
 * the capture echo canceller. [flush] drops queued + buffered audio for barge-in.
 */
actual class VoiceAudioPlayback actual constructor() {
    private val running = AtomicBoolean(false)
    private val queue = LinkedBlockingQueue<ByteArray>()
    private var track: AudioTrack? = null
    private var thread: Thread? = null

    actual fun start() {
        if (running.get()) return

        val minBuffer = AudioTrack.getMinBufferSize(
            PLAYBACK_SAMPLE_RATE,
            AudioFormat.CHANNEL_OUT_MONO,
            AudioFormat.ENCODING_PCM_16BIT,
        )
        val audioTrack = AudioTrack.Builder()
            .setAudioAttributes(
                AudioAttributes.Builder()
                    .setUsage(AudioAttributes.USAGE_VOICE_COMMUNICATION)
                    .setContentType(AudioAttributes.CONTENT_TYPE_SPEECH)
                    .build(),
            )
            .setAudioFormat(
                AudioFormat.Builder()
                    .setSampleRate(PLAYBACK_SAMPLE_RATE)
                    .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                    .setChannelMask(AudioFormat.CHANNEL_OUT_MONO)
                    .build(),
            )
            .setBufferSizeInBytes(maxOf(minBuffer, PLAYBACK_SAMPLE_RATE * 2))
            .setTransferMode(AudioTrack.MODE_STREAM)
            .build()

        track = audioTrack
        running.set(true)
        audioTrack.play()

        thread = Thread {
            while (running.get()) {
                val chunk = runCatching { queue.poll(100, TimeUnit.MILLISECONDS) }.getOrNull() ?: continue
                var offset = 0
                while (offset < chunk.size && running.get()) {
                    val written = audioTrack.write(chunk, offset, chunk.size - offset, AudioTrack.WRITE_BLOCKING)
                    if (written <= 0) break
                    offset += written
                }
            }
        }.apply {
            isDaemon = true
            name = "osiris-voice-playback"
            start()
        }
    }

    actual fun write(pcm: ByteArray) {
        if (running.get()) queue.offer(pcm)
    }

    actual fun flush() {
        queue.clear()
        track?.let {
            runCatching {
                it.pause()
                it.flush()
                it.play()
            }
        }
    }

    actual fun stop() {
        running.set(false)
        queue.clear()
        thread?.let { runCatching { it.join(250) } }
        thread = null
        track?.let {
            runCatching { it.pause() }
            runCatching { it.flush() }
            runCatching { it.release() }
        }
        track = null
    }
}
