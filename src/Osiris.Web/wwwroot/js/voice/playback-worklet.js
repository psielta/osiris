// Playback worklet: the AudioContext is created at 24 kHz (the Live API output rate), so we just enqueue
// incoming PCM16 frames (converted to Float32) and drain them sample-by-sample into the output. A 'flush'
// message clears the queue for barge-in (when the user interrupts the assistant).
class PlaybackProcessor extends AudioWorkletProcessor {
    constructor() {
        super();
        this.queue = [];
        this.current = null;
        this.offset = 0;
        this.port.onmessage = (event) => {
            if (event.data === 'flush') {
                this.queue = [];
                this.current = null;
                this.offset = 0;
                return;
            }
            const int16 = new Int16Array(event.data);
            const float32 = new Float32Array(int16.length);
            for (let i = 0; i < int16.length; i++) {
                float32[i] = int16[i] / 0x8000;
            }
            this.queue.push(float32);
        };
    }

    process(_inputs, outputs) {
        const output = outputs[0][0];
        if (!output) {
            return true;
        }
        for (let i = 0; i < output.length; i++) {
            if (!this.current || this.offset >= this.current.length) {
                this.current = this.queue.shift() || null;
                this.offset = 0;
            }
            output[i] = this.current ? this.current[this.offset++] : 0;
        }
        return true;
    }
}

registerProcessor('osiris-playback', PlaybackProcessor);
