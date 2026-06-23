// Capture worklet: the AudioContext is created at 16 kHz, so the browser already resampled the mic to
// 16 kHz mono. We only convert Float32 [-1,1] to little-endian PCM16 and post the bytes to the main
// thread, which streams them over the WebSocket as the Live API input format (audio/pcm;rate=16000).
class CaptureProcessor extends AudioWorkletProcessor {
    process(inputs) {
        const input = inputs[0];
        if (input && input[0] && input[0].length) {
            const samples = input[0];
            const pcm = new Int16Array(samples.length);
            for (let i = 0; i < samples.length; i++) {
                const s = Math.max(-1, Math.min(1, samples[i]));
                pcm[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
            }
            this.port.postMessage(pcm.buffer, [pcm.buffer]);
        }
        return true;
    }
}

registerProcessor('osiris-capture', CaptureProcessor);
