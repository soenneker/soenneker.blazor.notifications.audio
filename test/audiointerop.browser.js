// Evaluate in the demo page after a user gesture. Uses real browser Web Audio, not test doubles.
(async () => {
    const { AudioEngine } = await import(`/_content/Soenneker.Blazor.Notifications.Audio/js/audiointerop.js?verify=${Date.now()}`);
    const engine = new AudioEngine();
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
    const passed = [];
    try {
        check(await engine.unlock(), 'Context did not unlock');
        engine.setMasterVolume(0);
        for (let i = 0; i < 13; i++) {
            const buffer = await engine.buffer('preset', i);
            check(buffer instanceof AudioBuffer && buffer.duration > 0, 'Invalid preset buffer');
            await engine.play(`preset-${i}`, 'preset', i);
        }
        engine.stopAll(); passed.push('13 real AudioBuffer presets');
        const ring = await engine.buffer('preset', 12);
        check(ring.duration === 4, 'Telephone ring must include its four-second cadence');
        const ringSamples = ring.getChannelData(0);
        const hasSignal = (start, end) => ringSamples.slice(Math.round(start * ring.sampleRate), Math.round(end * ring.sampleRate)).some(value => Math.abs(value) > .01);
        check(hasSignal(0, .5) && hasSignal(.78, 1.5), 'Telephone ring must contain both note pairs');
        check(!hasSignal(2, 4), 'Telephone ring must pause between cycles');
        await engine.play('ring', 'preset', 12, { loop: true, key: 'incoming-call', replaceExisting: true });
        await engine.play('replacement-ring', 'preset', 12, { loop: true, key: 'incoming-call', replaceExisting: true });
        check(engine.getState('ring') === null, 'Replacement must stop the previous ring');
        check(engine.active.get('replacement-ring').source.loop, 'Telephone ring must loop');
        engine.stop('replacement-ring');
        check(engine.getState('replacement-ring') === null, 'Answering must stop the ring');
        passed.push('telephone ring cadence, looping, replacement and stop');
        await engine.play('control', 'preset', 2, { loop: true, volume: .4, pan: -.5, fadeIn: .02 });
        await delay(80);
        check(engine.getState('control').position > 0, 'Audio clock did not advance');
        engine.pause('control');
        const position = engine.getState('control').position;
        await delay(60);
        check(engine.getState('control').position === position, 'Paused position advanced');
        engine.seek('control', .2);
        engine.setPlaybackRate('control', 2);
        engine.setPan('control', .8);
        engine.setVolume('control', .2, .01);
        await engine.resume('control'); await delay(60);
        check(engine.getState('control').position > .2, 'Resumed position did not advance');
        engine.stop('control', .02);
        await Promise.race([engine.waitForEnd('control'), delay(1000).then(() => { throw new Error('Fade did not complete'); })]);
        check(engine.getState('control') === null, 'Fade leaked playback');
        passed.push('native pause/resume/seek/rate/pan/volume/fade/completion');
        const buffer = await engine.buffer('preset', 0);
        const samples = buffer.getChannelData(0);
        const bytes = new Uint8Array(44 + samples.length * 2), view = new DataView(bytes.buffer);
        const word = (offset, value) => [...value].forEach((c, i) => view.setUint8(offset + i, c.charCodeAt(0)));
        word(0, 'RIFF'); view.setUint32(4, bytes.length - 8, true); word(8, 'WAVE'); word(12, 'fmt ');
        view.setUint32(16, 16, true); view.setUint16(20, 1, true); view.setUint16(22, 1, true);
        view.setUint32(24, buffer.sampleRate, true); view.setUint32(28, buffer.sampleRate * 2, true);
        view.setUint16(32, 2, true); view.setUint16(34, 16, true); word(36, 'data'); view.setUint32(40, samples.length * 2, true);
        for (let i = 0; i < samples.length; i++) view.setInt16(44 + i * 2, Math.round(samples[i] * 32767), true);
        const url = URL.createObjectURL(new Blob([bytes], { type: 'audio/wav' }));
        try {
            engine.configureLimits({ maxDownloadBytes: bytes.length + 1 });
            await engine.preload(url); engine.registerUrl('custom', url);
            await engine.play('url', 'registered', 'custom');
            check(engine.getState('url').duration > .6, 'URL decoding failed');
            engine.stopAll();
        } finally { URL.revokeObjectURL(url); }
        passed.push('real streamed WAV fetch/decode/cache/registered playback');
        engine.registerTones('chord', [{ frequency: 440 }, { frequency: 660, waveform: 1 }]);
        await engine.play('chord', 'registered', 'chord');
        await engine.waitForEnd('chord');
        check(engine.getState('chord') === null, 'Natural completion leaked playback');
        passed.push('custom chord and natural completion');
        await engine.dispose();
        check(engine.context.state === 'closed' && engine.active.size === 0, 'Dispose leaked audio context');
        passed.push('context and node disposal');
        return { passed };
    } finally { await engine.dispose(); }
})()
