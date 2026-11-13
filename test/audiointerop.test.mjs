import test from 'node:test';
import assert from 'node:assert/strict';
import { AudioEngine } from '../src/Soenneker.Blazor.Notifications.Audio/wwwroot/js/audiointerop.js';

function parameter(value = 1) {
    return { value, setValueAtTime(v) { this.value = v; }, linearRampToValueAtTime(v) { this.value = v; }, cancelScheduledValues() {}, cancelAndHoldAtTime() {} };
}
function node() { return { connect() {}, disconnect() { this.disconnected = true; }, gain: parameter(), pan: parameter(0) }; }
function context() {
    return {
        currentTime: 0, sampleRate: 48000, state: 'running', destination: {},
        createGain: node, createStereoPanner: node,
        createBuffer(channels, length, sampleRate) {
            const data = new Float32Array(length);
            return { duration: length / sampleRate, length, sampleRate, numberOfChannels: channels, getChannelData: () => data };
        },
        createBufferSource() {
            return { ...node(), playbackRate: parameter(), start(time, offset) { this.startTime = time; this.offset = offset; }, stop(time) { this.stopTime = time; } };
        },
        async resume() { this.state = 'running'; }, async close() { this.state = 'closed'; },
        async decodeAudioData() { return this.createBuffer(1, 48000, 48000); }
    };
}
const listeners = new Set();
globalThis.addEventListener = (event, listener) => listeners.add(listener);
globalThis.removeEventListener = (event, listener) => listeners.delete(listener);
globalThis.AudioContext = function () { return context(); };
globalThis.document = { baseURI: 'https://example.test/app/' };

async function withEngine(action) { const e = new AudioEngine(); try { await action(e); } finally { await e.dispose(); } }

test('all twelve presets synthesize finite, non-silent, peak-limited distinct buffers', () => withEngine(async e => {
    const fingerprints = new Set();
    for (let i = 0; i < 12; i++) {
        const buffer = await e.buffer('preset', i), data = buffer.getChannelData(0);
        assert.ok(data.every(Number.isFinite));
        const peak = data.reduce((max, v) => Math.max(max, Math.abs(v)), 0);
        assert.ok(peak > .01 && peak <= 1);
        fingerprints.add(`${data.length}:${data[127]}`);
    }
    assert.equal(fingerprints.size, 12);
}));

test('pause/resume and rate changes preserve source position', () => withEngine(async e => {
    await e.play('a', 'preset', 2, { loop: true });
    e.context.currentTime = .2;
    assert.equal(e.getState('a').position, .2);
    e.setPlaybackRate('a', 2);
    e.context.currentTime = .3;
    assert.ok(Math.abs(e.getState('a').position - .4) < 1e-9);
    assert.equal(e.pause('a'), true);
    e.context.currentTime = 5;
    assert.ok(Math.abs(e.getState('a').position - .4) < 1e-9);
    assert.equal(await e.resume('a'), true);
    e.context.currentTime = 5.1;
    assert.ok(Math.abs(e.getState('a').position - .6) < 1e-9);
    e.seek('a', .1);
    assert.equal(e.getState('a').position, .1);
}));

test('loop boundaries, scheduled delay and seek while paused', () => withEngine(async e => {
    await e.play('a', 'preset', 2, { loop: true, loopStart: .2, loopEnd: .6, delay: 2 });
    e.context.currentTime = 1;
    assert.equal(e.getState('a').status, 'scheduled');
    assert.equal(e.getState('a').position, 0);
    e.setPlaybackRate('a', 2);
    e.context.currentTime = 2.4;
    assert.ok(Math.abs(e.getState('a').position - .4) < 1e-9);
    e.pause('a'); e.seek('a', .3);
    assert.equal(e.getState('a').status, 'paused');
    assert.equal(e.getState('a').position, .3);
}));

test('master mute preserves master and per-playback volumes', () => withEngine(async e => {
    await e.play('a', 'preset', 0, { volume: .4 });
    e.setMasterVolume(.3); e.setMuted(true); e.setMasterVolume(.6);
    assert.equal(e.master.gain.value, 0);
    e.setMuted(false);
    assert.equal(e.master.gain.value, .6);
    assert.equal(e.getState('a').volume, .4);
}));

test('stopping and natural completion release nodes, slots, and waiters', () => withEngine(async e => {
    await e.play('a', 'preset', 0);
    const p = e.active.get('a');
    const waiting = e.waitForEnd('a');
    p.source.onended(); await waiting;
    assert.equal(e.getState('a'), null);
    assert.equal(p.gain.disconnected, true);
    await e.play('b', 'preset', 0);
    const source = e.active.get('b').source;
    e.stop('b', .2);
    assert.equal(e.getState('b').status, 'stopping');
    assert.equal(source.stopTime, .2);
    source.onended();
    assert.equal(e.getState('b'), null);
}));

test('same-key replacement is opt-in and capacity errors do not leak', () => withEngine(async e => {
    e.setMaxConcurrent(1);
    await e.play('a', 'preset', 0, { key: 'message', loop: true });
    await assert.rejects(e.play('b', 'preset', 0), /Maximum/);
    assert.equal(e.pending.size, 0);
    await e.play('c', 'preset', 1, { key: 'message', replaceExisting: true });
    assert.equal(e.getState('a'), null);
    assert.ok(e.getState('c'));
}));

test('validation rejects non-finite numbers, invalid loops, and invalid tone envelopes', () => withEngine(async e => {
    for (const setting of [{ volume: NaN }, { pan: 2 }, { playbackRate: 0 }, { delay: -1 }, { offset: 3 }, { loopStart: .5, loopEnd: .1 }])
        await assert.rejects(e.play('a', 'preset', 0, setting), RangeError);
    for (const tones of [[], [{ frequency: -1 }], [{ duration: .01, release: .5 }], [{ waveform: 1.5 }], [{ start: 30 }]])
        assert.throws(() => e.synthesize(tones), RangeError);
    await assert.rejects(e.play('a', 'preset', 99), RangeError);
    assert.equal(e.active.size, 0); assert.equal(e.pending.size, 0);
}));

test('custom registrations copy tones and remain independent of playing sounds', () => withEngine(async e => {
    const tones = [{ frequency: 440 }];
    e.registerTones('custom', tones); tones[0].frequency = -10;
    await e.play('a', 'registered', 'custom');
    assert.equal(e.unregister('custom'), true);
    assert.ok(e.getState('a'));
    await assert.rejects(e.play('b', 'registered', 'custom'), /Unknown registered/);
}));

test('URL loads are shared, failed loads retry, and clearing does not stop active playback', () => withEngine(async e => {
    let requests = 0;
    globalThis.fetch = async () => { requests++; return { ok: requests > 1, status: 503, arrayBuffer: async () => new ArrayBuffer(8) }; };
    await assert.rejects(e.preload('sound.wav'), /503/);
    await Promise.all([e.preload('sound.wav'), e.preload('./sound.wav')]);
    assert.equal(requests, 2);
    await e.play('a', 'url', 'sound.wav');
    e.clearCache(); assert.ok(e.getState('a'));
    await e.preload('sound.wav'); assert.equal(requests, 3);
}));

test('stopping a pending URL start prevents late playback', () => withEngine(async e => {
    let release;
    globalThis.fetch = () => new Promise(resolve => { release = resolve; });
    const playing = e.play('a', 'url', 'slow.wav');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(e.stop('a'), true);
    release({ ok: true, arrayBuffer: async () => new ArrayBuffer(8) });
    await assert.rejects(playing, /canceled/);
    assert.equal(e.active.size, 0);
}));

test('stopping during asynchronous resume does not resurrect a sound', () => withEngine(async e => {
    await e.play('a', 'preset', 0); e.pause('a');
    const resuming = e.resume('a'); e.stop('a');
    assert.equal(await resuming, false);
    assert.equal(e.active.size, 0);
}));

test('locked browser activation fails promptly rather than hanging', () => withEngine(async e => {
    e.context.state = 'suspended'; e.context.resume = () => new Promise(() => {});
    await assert.rejects(e.play('a', 'preset', 0), /locked/);
    assert.equal(e.pending.size, 0);
}));

test('scopes are isolated and disposal aborts loading, resolves waiters, and removes listeners', async () => {
    const first = new AudioEngine(), second = new AudioEngine();
    try {
        await first.play('a', 'preset', 0, { loop: true });
        const waiter = first.waitForEnd('a');
        let signal;
        globalThis.fetch = (url, options) => new Promise((resolve, reject) => {
            signal = options.signal; signal.addEventListener('abort', () => reject(new Error('aborted')));
        });
        const preload = first.preload('slow.wav');
        const rejected = assert.rejects(preload, /aborted/);
        await first.dispose(); await waiter; await rejected;
        assert.equal(signal.aborted, true);
        assert.equal(first.context.state, 'closed');
        assert.equal(listeners.has(first.gesture), false);
        assert.equal(second.context.state, 'running');
        await first.dispose();
        await assert.rejects(first.play('x', 'preset', 0), /disposed/);
    } finally { await first.dispose(); await second.dispose(); }
});

test('resource limits bound cache bytes and reject oversized sounds', () => withEngine(async e => {
    globalThis.fetch = async () => ({ ok: true, arrayBuffer: async () => new ArrayBuffer(8) });
    e.configureLimits({ maxCacheBytes: 200000 });
    await e.preload('one.wav'); await e.preload('two.wav');
    assert.equal(e.cache.size, 1);
    assert.ok(e.cache.has('https://example.test/app/two.wav'));
    e.configureLimits({ maxCachedSounds: 0 });
    assert.equal(e.cache.size, 0);
    e.configureLimits({ maxDownloadBytes: 4 });
    await assert.rejects(e.preload('large.wav'), /size limit/);
    e.configureLimits({ maxDownloadBytes: 32, maxSoundDuration: .5 });
    await assert.rejects(e.preload('long.wav'), /duration limit/);
    assert.equal(e.downloads.size, 0);
}));

test('streamed downloads enforce size limits before decoding', () => withEngine(async e => {
    let canceled = false, decoded = false;
    e.context.decodeAudioData = async () => { decoded = true; };
    e.configureLimits({ maxDownloadBytes: 4 });
    globalThis.fetch = async () => ({ ok: true, body: { getReader: () => ({
        read: async () => ({ done: false, value: new Uint8Array(8) }),
        cancel: async () => { canceled = true; }, releaseLock() {}
    }) } });
    await assert.rejects(e.preload('stream.wav'), /size limit/);
    assert.equal(canceled, true); assert.equal(decoded, false);
}));

test('download concurrency and timeout release capacity', () => withEngine(async e => {
    e.configureLimits({ maxDownloads: 1, downloadTimeout: .1 });
    globalThis.fetch = (url, { signal }) => new Promise((resolve, reject) => {
        signal.addEventListener('abort', () => reject(signal.reason));
    });
    const first = e.preload('slow.wav');
    await assert.rejects(e.preload('second.wav'), /Maximum concurrent/);
    await assert.rejects(first, /timed out/);
    assert.equal(e.downloads.size, 0);
    assert.equal(e.cache.size, 0);
}));
