// All presets are original synthesized tones. No third-party recordings or network dependencies.
const tone = (frequency, start = 0, duration = .24, volume = .35, waveform = 0, endFrequency = null) =>
    ({ frequency, start, duration, volume, waveform, endFrequency, attack: .004, release: duration - .004 });
const presets = [
    [tone(1046.5, 0, .7), tone(2093, 0, .45, .09)], // Ding
    [tone(659.25, 0, .4), tone(830.61, .12, .4), tone(987.77, .24, .6)], // Chime
    [tone(880, 0, 1, .32), tone(1760, 0, .7, .12), tone(2380, 0, .4, .04)], // Bell
    [tone(650, 0, .09, .5, 0, 180)], // Pop
    [tone(1600, 0, .025, .22, 2)], // Click
    [tone(740, 0, .16), tone(1108.73, .12, .3)], // Message
    [tone(523.25, 0, .2), tone(659.25, .12, .2), tone(783.99, .24, .45)], // Success
    [tone(440, 0, .2, .25, 1), tone(440, .28, .2, .25, 1)], // Warning
    [tone(330, 0, .23, .28, 1), tone(220, .19, .4, .3, 1)], // Error
    [tone(523.25, 0, .5, .22), tone(659.25, 0, .5, .18), tone(783.99, 0, .65, .18), tone(1046.5, .25, .6, .2)], // Complete
    [tone(440, 0, .15), tone(554.37, .1, .15), tone(659.25, .2, .15), tone(880, .3, .3)],
    [tone(880, 0, .15), tone(659.25, .1, .15), tone(554.37, .2, .15), tone(440, .3, .3)]
];

function number(value, min, max, name) {
    if (!Number.isFinite(value) || value < min || value > max)
        throw new RangeError(`${name} must be finite and between ${min} and ${max}.`);
    return value;
}
function text(value, name) {
    if (typeof value !== "string" || !value.trim()) throw new TypeError(`${name} must not be empty.`);
    return value;
}
function validateTones(tones) {
    if (!Array.isArray(tones) || tones.length === 0 || tones.length > 128)
        throw new RangeError("Provide between 1 and 128 tones.");
    let totalDuration = 0;
    return tones.map(t => {
        const result = {
            frequency: number(t.frequency ?? 880, 20, 20000, "frequency"),
            endFrequency: t.endFrequency == null ? null : number(t.endFrequency, 20, 20000, "endFrequency"),
            start: number(t.start ?? 0, 0, 30, "start"),
            duration: number(t.duration ?? .25, .001, 30, "duration"),
            volume: number(t.volume ?? .5, 0, 1, "volume"),
            waveform: number(t.waveform ?? 0, 0, 3, "waveform"),
            attack: number(t.attack ?? .005, 0, 30, "attack"),
            release: number(t.release ?? .08, 0, 30, "release")
        };
        if (!Number.isInteger(result.waveform) || result.start + result.duration > 30 || result.attack + result.release > result.duration)
            throw new RangeError("Invalid waveform, sequence longer than 30 seconds, or attack + release longer than duration.");
        totalDuration += result.duration;
        if (totalDuration > 60) throw new RangeError("Combined tone durations must not exceed 60 seconds.");
        return result;
    });
}
function options(value) {
    const o = value ?? {};
    if (o.key != null) text(o.key, "key");
    return {
        volume: number(o.volume ?? 1, 0, 1, "volume"), pan: number(o.pan ?? 0, -1, 1, "pan"),
        playbackRate: number(o.playbackRate ?? 1, .25, 4, "playbackRate"), loop: !!o.loop,
        loopStart: number(o.loopStart ?? 0, 0, Number.MAX_VALUE, "loopStart"),
        loopEnd: o.loopEnd == null ? null : number(o.loopEnd, 0, Number.MAX_VALUE, "loopEnd"),
        offset: number(o.offset ?? 0, 0, Number.MAX_VALUE, "offset"),
        delay: number(o.delay ?? 0, 0, 3600, "delay"), fadeIn: number(o.fadeIn ?? 0, 0, 30, "fadeIn"),
        key: o.key ?? null, replaceExisting: !!o.replaceExisting
    };
}

export class AudioEngine {
    constructor() {
        const Context = globalThis.AudioContext ?? globalThis.webkitAudioContext;
        if (!Context) throw new Error("Web Audio is not supported by this browser.");
        this.context = new Context();
        this.master = this.context.createGain();
        this.master.connect(this.context.destination);
        this.volume = 1;
        this.muted = false;
        this.maxConcurrent = 32;
        this.limits = { maxCachedSounds: 32, maxCacheBytes: 32 * 1024 * 1024, maxDownloadBytes: 16 * 1024 * 1024,
            maxDownloads: 8, downloadTimeout: 30, maxSoundDuration: 120 };
        this.active = new Map();
        this.pending = new Map();
        this.cache = new Map();
        this.presetBuffers = new Map();
        this.registered = new Map();
        this.downloads = new Set();
        this.disposed = false;
        this.gesture = () => { if (this.context.state === "suspended" || this.context.state === "interrupted") void this.context.resume().catch(() => {}); };
        // The resume call must occur inside the native gesture, including for Blazor Server.
        for (const event of ["pointerdown", "keydown", "touchend"])
            globalThis.addEventListener(event, this.gesture, { capture: true, passive: true });
    }
    check() { if (this.disposed) throw new Error("Notification audio has been disposed."); }
    configureLimits(limits) {
        this.check();
        if (!limits) throw new TypeError("Resource limits are required.");
        const next = { ...this.limits, ...limits };
        for (const name of ["maxCachedSounds", "maxCacheBytes", "maxDownloadBytes", "maxDownloads"]) {
            number(next[name], name === "maxDownloads" || name === "maxDownloadBytes" ? 1 : 0, 2147483647, name);
            if (!Number.isInteger(next[name])) throw new RangeError(`${name} must be an integer.`);
        }
        number(next.downloadTimeout, .1, 3600, "downloadTimeout");
        number(next.maxSoundDuration, .001, 3600, "maxSoundDuration");
        this.limits = next; this.trimCache();
    }
    trimCache() {
        let bytes = 0;
        for (const entry of this.cache.values()) bytes += entry.bytes;
        for (const [key, entry] of this.cache) {
            if (this.cache.size <= this.limits.maxCachedSounds && bytes <= this.limits.maxCacheBytes) break;
            this.cache.delete(key); bytes -= entry.bytes;
        }
    }
    async unlock() {
        this.check();
        // resume() can remain pending indefinitely under autoplay policy; never leave an interop call hanging.
        let timer;
        try {
            await Promise.race([this.context.resume(), new Promise(resolve => { timer = setTimeout(resolve, 250); })]);
        } finally { clearTimeout(timer); }
        return !this.disposed && this.context.state === "running";
    }
    async ready() {
        this.check();
        if (this.context.state !== "running" && !await this.unlock())
            throw new Error("Notification audio is locked. Initialize after render, then interact with the page to enable sounds.");
        this.check();
    }
    synthesize(input) {
        const tones = validateTones(input);
        const rate = this.context.sampleRate;
        const duration = Math.max(...tones.map(t => t.start + t.duration));
        const buffer = this.context.createBuffer(1, Math.ceil(duration * rate), rate);
        const samples = buffer.getChannelData(0);
        for (const t of tones) {
            if (Math.max(t.frequency, t.endFrequency ?? t.frequency) >= rate / 2)
                throw new RangeError("Tone frequency must be below half the audio sample rate.");
            const start = Math.round(t.start * rate), length = Math.round(t.duration * rate);
            // Limit harmonics to Nyquist to avoid harsh aliases on square/triangle/sawtooth tones.
            const harmonics = Math.min(64, Math.floor((rate / 2 - 1) / Math.max(t.frequency, t.endFrequency ?? t.frequency)));
            for (let i = 0; i < length && start + i < samples.length; i++) {
                const time = i / rate;
                const phase = 2 * Math.PI * (t.frequency * time + ((t.endFrequency ?? t.frequency) - t.frequency) * time * time / (2 * t.duration));
                let wave = 0;
                if (t.waveform === 0) wave = Math.sin(phase);
                else for (let h = 1; h <= harmonics; h++) {
                    if (t.waveform === 1 && h % 2) wave += 8 / (Math.PI ** 2) * (-1) ** ((h - 1) / 2) * Math.sin(h * phase) / (h * h);
                    if (t.waveform === 2 && h % 2) wave += 4 / Math.PI * Math.sin(h * phase) / h;
                    if (t.waveform === 3) wave += 2 / Math.PI * (-1) ** (h + 1) * Math.sin(h * phase) / h;
                }
                const attack = t.attack === 0 ? 1 : Math.min(1, time / t.attack);
                const release = t.release === 0 ? 1 : Math.min(1, (t.duration - time) / t.release);
                samples[start + i] += wave * t.volume * attack * release;
            }
        }
        let peak = 1;
        for (const sample of samples) peak = Math.max(peak, Math.abs(sample));
        if (peak > 1) for (let i = 0; i < samples.length; i++) samples[i] /= peak;
        return buffer;
    }
    load(url) {
        this.check(); text(url, "url");
        const key = new URL(url, globalThis.document?.baseURI ?? globalThis.location?.href).href;
        let entry = this.cache.get(key);
        if (entry) { this.cache.delete(key); this.cache.set(key, entry); return entry.promise; }
        if (this.downloads.size >= this.limits.maxDownloads) throw new Error("Maximum concurrent audio downloads reached.");
        const limits = this.limits;
        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(new Error("Audio download timed out.")), limits.downloadTimeout * 1000);
        this.downloads.add(controller);
        entry = { bytes: 0, promise: null };
        entry.promise = (async () => {
            try {
                const response = await fetch(key, { signal: controller.signal });
                if (!response.ok) throw new Error(`Audio download failed (${response.status}): ${key}`);
                const declaredSize = Number(response.headers?.get("content-length"));
                if (declaredSize > limits.maxDownloadBytes) throw new RangeError("Audio exceeds the download size limit.");
                let data;
                if (response.body?.getReader) {
                    const reader = response.body.getReader(), chunks = [];
                    let size = 0;
                    try {
                        while (true) {
                            const { done, value } = await reader.read();
                            if (done) break;
                            size += value.byteLength;
                            if (size > limits.maxDownloadBytes) throw new RangeError("Audio exceeds the download size limit.");
                            chunks.push(value);
                        }
                    } finally { await reader.cancel().catch(() => {}); reader.releaseLock(); }
                    const bytes = new Uint8Array(size);
                    let offset = 0;
                    for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.byteLength; }
                    data = bytes.buffer;
                } else data = await response.arrayBuffer();
                if (data.byteLength > limits.maxDownloadBytes) throw new RangeError("Audio exceeds the download size limit.");
                clearTimeout(timer);
                this.check();
                const buffer = await this.context.decodeAudioData(data);
                this.check();
                if (buffer.duration > limits.maxSoundDuration) throw new RangeError("Audio exceeds the decoded duration limit.");
                entry.bytes = buffer.length * buffer.numberOfChannels * 4;
                this.trimCache();
                return buffer;
            } catch (error) {
                controller.abort();
                if (this.cache.get(key) === entry) this.cache.delete(key);
                throw error;
            } finally { clearTimeout(timer); this.downloads.delete(controller); }
        })();
        this.cache.set(key, entry); this.trimCache();
        return entry.promise;
    }
    async preload(url) { await this.load(url); }
    clearCache() { this.check(); this.cache.clear(); this.presetBuffers.clear(); }
    registerUrl(name, url) { this.check(); this.registered.set(text(name, "name"), { kind: "url", value: text(url, "url") }); }
    registerTones(name, tones) { this.check(); this.registered.set(text(name, "name"), { kind: "tones", value: validateTones(tones) }); }
    unregister(name) { this.check(); return this.registered.delete(text(name, "name")); }
    async buffer(kind, value) {
        if (kind === "url") return this.load(value);
        if (kind === "tones") return this.synthesize(value);
        if (kind === "preset") {
            if (!Number.isInteger(value) || !presets[value]) throw new RangeError("Unknown notification sound.");
            if (!this.presetBuffers.has(value)) this.presetBuffers.set(value, this.synthesize(presets[value]));
            return this.presetBuffers.get(value);
        }
        if (kind === "registered") {
            const sound = this.registered.get(text(value, "name"));
            if (!sound) throw new Error(`Unknown registered sound: ${value}`);
            return this.buffer(sound.kind, sound.value);
        }
        throw new Error("Unknown sound source.");
    }
    async play(id, kind, value, settings) {
        this.check(); text(id, "id");
        if (this.pending.has(id) || this.active.has(id)) throw new Error("Playback ID is already in use.");
        if (this.pending.size >= 128) throw new Error("Maximum pending audio playbacks reached.");
        const o = options(settings);
        const marker = {};
        this.pending.set(id, marker);
        try {
            await this.ready();
            if (this.pending.get(id) !== marker) throw new Error("Audio playback was canceled.");
            const buffer = await this.buffer(kind, value);
            this.check();
            if (this.pending.get(id) !== marker) throw new Error("Audio playback was canceled.");
            o.loopEnd ??= buffer.duration;
            if (o.offset >= buffer.duration || o.loopStart >= o.loopEnd || o.loopEnd > buffer.duration)
                throw new RangeError("Offset and loop bounds must be inside the sound; loop start must precede loop end.");
            if (o.replaceExisting && o.key)
                for (const [otherId, other] of this.active) if (other.options.key === o.key) this.stop(otherId);
            if (this.active.size >= this.maxConcurrent) throw new Error("Maximum concurrent audio playbacks reached.");
            const gain = this.context.createGain(), pan = this.context.createStereoPanner();
            gain.connect(pan); pan.connect(this.master); pan.pan.value = o.pan;
            let resolve;
            const ended = new Promise(done => { resolve = done; });
            const p = { id, buffer, options: o, gain, pan, offset: o.offset, source: null, paused: false, stopping: false, ended, resolve };
            this.active.set(id, p);
            try { this.start(p, o.delay); } catch (error) { this.finish(p); throw error; }
        } finally { if (this.pending.get(id) === marker) this.pending.delete(id); }
    }
    start(p, delay = 0) {
        const now = this.context.currentTime;
        if (p.options.loop && p.offset >= p.options.loopEnd) p.offset = p.options.loopStart;
        const source = this.context.createBufferSource();
        source.buffer = p.buffer;
        source.loop = p.options.loop; source.loopStart = p.options.loopStart; source.loopEnd = p.options.loopEnd;
        source.playbackRate.value = p.options.playbackRate;
        source.connect(p.gain);
        p.source = source; p.started = now + delay; p.paused = false;
        p.gain.gain.cancelScheduledValues(now);
        p.gain.gain.setValueAtTime(p.options.fadeIn > 0 ? 0 : p.options.volume, now);
        p.gain.gain.setValueAtTime(p.options.fadeIn > 0 ? 0 : p.options.volume, p.started);
        if (p.options.fadeIn > 0) p.gain.gain.linearRampToValueAtTime(p.options.volume, p.started + p.options.fadeIn);
        source.onended = () => { if (p.source === source) this.finish(p); };
        source.start(p.started, p.offset);
    }
    position(p) {
        let value = p.offset + (p.paused ? 0 : Math.max(0, this.context.currentTime - p.started) * p.options.playbackRate);
        if (p.options.loop && value >= p.options.loopEnd)
            value = p.options.loopStart + (value - p.options.loopStart) % (p.options.loopEnd - p.options.loopStart);
        return Math.min(value, p.buffer.duration);
    }
    detach(p) {
        if (!p.source) return;
        p.source.onended = null;
        try { p.source.stop(); } finally { p.source.disconnect(); p.source = null; }
    }
    finish(p) {
        this.detach(p); p.gain.disconnect(); p.pan.disconnect();
        this.active.delete(p.id); p.resolve();
    }
    pause(id) {
        this.check(); const p = this.active.get(id);
        if (!p || p.paused || p.stopping) return false;
        p.offset = this.position(p); this.detach(p); p.paused = true;
        return true;
    }
    async resume(id) {
        this.check(); const p = this.active.get(id);
        if (!p || !p.paused || p.stopping) return false;
        await this.ready();
        if (this.active.get(id) !== p || !p.paused || p.stopping) return false;
        this.start(p); return true;
    }
    seek(id, position) {
        this.check(); const p = this.active.get(id);
        if (!p || p.stopping) return false;
        number(position, 0, p.buffer.duration, "position");
        if (position === p.buffer.duration) { this.finish(p); return true; }
        p.offset = position;
        if (!p.paused) { this.detach(p); this.start(p); }
        return true;
    }
    stop(id, fadeOut = 0) {
        this.check(); number(fadeOut, 0, 30, "fadeOut");
        const pending = this.pending.delete(id), p = this.active.get(id);
        if (!p) return pending;
        if (fadeOut === 0 || p.paused || this.context.state !== "running" || p.started > this.context.currentTime) this.finish(p);
        else {
            const now = this.context.currentTime;
            p.stopping = true;
            p.gain.gain.cancelAndHoldAtTime(now);
            p.gain.gain.linearRampToValueAtTime(0, now + fadeOut);
            p.source.stop(now + fadeOut);
        }
        return true;
    }
    stopAll(fadeOut = 0) {
        this.check(); number(fadeOut, 0, 30, "fadeOut"); this.pending.clear();
        for (const id of this.active.keys()) this.stop(id, fadeOut);
    }
    setVolume(id, volume, ramp = 0) {
        this.check(); number(volume, 0, 1, "volume"); number(ramp, 0, 30, "ramp");
        const p = this.active.get(id); if (!p || p.stopping) return false;
        p.options.volume = volume;
        const now = this.context.currentTime;
        p.gain.gain.cancelAndHoldAtTime(now);
        p.gain.gain.linearRampToValueAtTime(volume, now + ramp);
        return true;
    }
    setPan(id, pan) {
        this.check(); number(pan, -1, 1, "pan"); const p = this.active.get(id); if (!p || p.stopping) return false;
        p.options.pan = pan; p.pan.pan.setValueAtTime(pan, this.context.currentTime); return true;
    }
    setPlaybackRate(id, rate) {
        this.check(); number(rate, .25, 4, "playbackRate"); const p = this.active.get(id); if (!p || p.stopping) return false;
        p.offset = this.position(p); p.started = Math.max(p.started, this.context.currentTime); p.options.playbackRate = rate;
        p.source?.playbackRate.setValueAtTime(rate, this.context.currentTime); return true;
    }
    setMasterVolume(volume) { this.check(); this.volume = number(volume, 0, 1, "volume"); this.master.gain.setValueAtTime(this.muted ? 0 : volume, this.context.currentTime); }
    setMuted(muted) { this.check(); this.muted = !!muted; this.master.gain.setValueAtTime(this.muted ? 0 : this.volume, this.context.currentTime); }
    setMaxConcurrent(count) { this.check(); number(count, 1, 128, "count"); if (!Number.isInteger(count)) throw new RangeError("count must be an integer."); this.maxConcurrent = count; }
    getState(id) {
        this.check(); const p = this.active.get(id); if (!p) return null;
        return { id, status: p.stopping ? "stopping" : p.paused ? "paused" : p.started > this.context.currentTime ? "scheduled" : "playing",
            position: this.position(p), duration: p.buffer.duration, volume: p.options.volume, pan: p.options.pan,
            playbackRate: p.options.playbackRate, loop: p.options.loop, key: p.options.key };
    }
    async waitForEnd(id) { this.check(); await this.active.get(id)?.ended; }
    async dispose() {
        if (this.disposed) return;
        this.stopAll(); this.disposed = true;
        for (const event of ["pointerdown", "keydown", "touchend"]) globalThis.removeEventListener(event, this.gesture, true);
        for (const controller of this.downloads) controller.abort();
        this.cache.clear(); this.presetBuffers.clear(); this.registered.clear(); this.master.disconnect();
        await this.context.close();
    }
}

export function create() { return new AudioEngine(); }
