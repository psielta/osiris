// Voice client: opens the cookie-authenticated WebSocket to /assistant/voice, streams microphone PCM16
// (16 kHz) up, plays the assistant's PCM16 (24 kHz) down, and forwards control JSON events to the caller.
// The Gemini key never reaches the browser — this talks only to the Osiris backend, which proxies the
// Gemini Live session. Continuous streaming relies on the server-side VAD for turn-taking.
(function () {
    const SCRIPT_BASE = '/js/voice';

    async function create(options) {
        const onEvent = options.onEvent || function () {};
        const onError = options.onError || function () {};

        let stopped = false;
        let muted = false;
        let micStream = null;
        let captureCtx = null;
        let playbackCtx = null;
        let playbackNode = null;
        let socket = null;

        function cleanup() {
            stopped = true;
            try { if (socket && socket.readyState <= 1) socket.close(); } catch (e) {}
            try { if (micStream) micStream.getTracks().forEach(t => t.stop()); } catch (e) {}
            try { if (captureCtx) captureCtx.close(); } catch (e) {}
            try { if (playbackCtx) playbackCtx.close(); } catch (e) {}
        }

        try {
            const scheme = location.protocol === 'https:' ? 'wss' : 'ws';
            socket = new WebSocket(`${scheme}://${location.host}/assistant/voice`);
            socket.binaryType = 'arraybuffer';

            // Playback path (24 kHz). Created up front so audio plays as soon as it arrives.
            playbackCtx = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 24000 });
            await playbackCtx.audioWorklet.addModule(`${SCRIPT_BASE}/playback-worklet.js`);
            playbackNode = new AudioWorkletNode(playbackCtx, 'osiris-playback');
            playbackNode.connect(playbackCtx.destination);

            socket.onmessage = (event) => {
                if (typeof event.data !== 'string') {
                    playbackNode.port.postMessage(event.data); // PCM16 24 kHz chunk
                    return;
                }
                let msg;
                try { msg = JSON.parse(event.data); } catch (e) { return; }
                if (msg.type === 'status' && msg.value === 'interrupted') {
                    playbackNode.port.postMessage('flush'); // barge-in: drop queued audio
                }
                onEvent(msg);
            };

            socket.onerror = () => { if (!stopped) onError('Falha na conexão de voz.'); };
            socket.onclose = () => { if (!stopped) onEvent({ type: 'status', value: 'closed' }); };

            await new Promise((resolve, reject) => {
                socket.onopen = resolve;
                setTimeout(() => reject(new Error('timeout')), 10000);
            });

            // Capture path (16 kHz): browser resamples the mic to 16 kHz inside this context.
            micStream = await navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true } });
            captureCtx = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 16000 });
            await captureCtx.audioWorklet.addModule(`${SCRIPT_BASE}/capture-worklet.js`);
            const source = captureCtx.createMediaStreamSource(micStream);
            const captureNode = new AudioWorkletNode(captureCtx, 'osiris-capture');
            captureNode.port.onmessage = (e) => {
                if (!muted && socket && socket.readyState === WebSocket.OPEN) {
                    socket.send(e.data);
                }
            };
            source.connect(captureNode);
            // Keep the capture graph alive without echoing the mic to the speakers (gain 0).
            const sink = captureCtx.createGain();
            sink.gain.value = 0;
            captureNode.connect(sink);
            sink.connect(captureCtx.destination);

            onEvent({ type: 'status', value: 'listening' });
        } catch (error) {
            cleanup();
            onError(error && error.name === 'NotAllowedError'
                ? 'Permissão de microfone negada.'
                : 'Não foi possível iniciar a voz.');
            return { stop: function () {}, setMuted: function () {} };
        }

        function setMuted(value) {
            muted = value;
            // Also flip the track so the browser shows the mic as muted at the source.
            try { if (micStream) micStream.getAudioTracks().forEach(t => { t.enabled = !value; }); } catch (e) {}
        }

        return { stop: cleanup, setMuted };
    }

    window.OsirisVoice = { create };
})();
