package com.osiris.mobile.voice

/**
 * Captures microphone audio as little-endian PCM16, mono, 16 kHz — the format the Gemini Live relay
 * expects on the upstream. Each platform supplies the actual (AudioRecord on Android).
 */
expect class VoiceAudioCapture() {
    /** Starts recording; [onChunk] is invoked on a background thread with raw PCM16 frames. */
    fun start(onChunk: (ByteArray) -> Unit)

    /** Stops recording and releases the microphone. Safe to call when already stopped. */
    fun stop()
}

/**
 * Plays little-endian PCM16, mono, 24 kHz audio streamed from the assistant. Each platform supplies the
 * actual (AudioTrack on Android). [flush] drops any queued audio for barge-in when the user interrupts.
 */
expect class VoiceAudioPlayback() {
    /** Opens the output device and starts the playback worker. */
    fun start()

    /** Queues a PCM16 chunk for playback. */
    fun write(pcm: ByteArray)

    /** Drops all queued/buffered audio immediately (used on barge-in). */
    fun flush()

    /** Stops playback and releases the output device. Safe to call when already stopped. */
    fun stop()
}
