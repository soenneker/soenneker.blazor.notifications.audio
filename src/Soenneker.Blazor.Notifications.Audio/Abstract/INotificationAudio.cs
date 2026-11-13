using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;
using Soenneker.Blazor.Notifications.Audio.Models;

namespace Soenneker.Blazor.Notifications.Audio.Abstract;

/// <summary>Scoped notification audio with built-in sounds, custom URLs and tone synthesis, and independent playback controls.</summary>
/// <remarks>Use after interactive rendering, not during prerendering. Initialize early to capture user gestures.
/// Each scope owns its browser audio context. Invalid settings and browser playback/load failures throw.
/// Cancellation of controls cancels the interop wait; it does not roll back controls already applied in JavaScript.</remarks>
public interface INotificationAudio
{
    /// <summary>Sets download and cache limits. Evicts cached buffers as necessary; current sounds continue playing.</summary>
    ValueTask ConfigureLimits(AudioResourceLimits limits, CancellationToken cancellationToken = default);

    /// <summary>Loads the module and installs gesture listeners. Call after first interactive render so a later browser gesture can unlock audio.</summary>
    ValueTask Initialize(CancellationToken cancellationToken = default);

    /// <summary>Requests audio activation and returns whether the context is running. Browsers require a user gesture; playback fails clearly while locked.</summary>
    ValueTask<bool> Unlock(CancellationToken cancellationToken = default);

    /// <summary>Plays a built-in sound and returns its unique playback ID. Cancellation during startup stops that playback.</summary>
    ValueTask<string> Play(NotificationSound? sound = null, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Downloads, decodes, caches and plays a URL (including data/blob URLs). Cross-origin URLs require CORS; formats depend on the browser.</summary>
    ValueTask<string> PlayUrl(string url, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Synthesizes and plays a sequence of tones or chords, returning its playback ID.</summary>
    ValueTask<string> PlayTones(IReadOnlyList<AudioTone> tones, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Registers a reusable custom sound URL without downloading it. Replacing a registration does not interrupt active sounds.</summary>
    ValueTask RegisterUrl(string name, string url, CancellationToken cancellationToken = default);

    /// <summary>Registers a reusable custom synthesized sound, validating and copying its tones.</summary>
    ValueTask RegisterTones(string name, IReadOnlyList<AudioTone> tones, CancellationToken cancellationToken = default);

    /// <summary>Plays a previously registered custom sound. Unknown names throw.</summary>
    ValueTask<string> PlayRegistered(string name, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a custom registration without interrupting active sounds. Returns whether it existed.</summary>
    ValueTask<bool> Unregister(string name, CancellationToken cancellationToken = default);

    /// <summary>Downloads and decodes a sound without playing it. Failed loads can be retried; concurrent requests share a load.</summary>
    ValueTask Preload(string url, CancellationToken cancellationToken = default);

    /// <summary>Evicts decoded URL and preset buffers without stopping active sounds or removing registrations. URL cache defaults to 32 entries and 32 MiB.</summary>
    ValueTask ClearCache(CancellationToken cancellationToken = default);

    /// <summary>Pauses a playing or scheduled sound. Returns false if absent or already paused/stopping.</summary>
    ValueTask<bool> Pause(string id, CancellationToken cancellationToken = default);

    /// <summary>Resumes a paused sound immediately from its saved position. Returns false if absent or not paused.</summary>
    ValueTask<bool> Resume(string id, CancellationToken cancellationToken = default);

    /// <summary>Seeks in source seconds within the sound. Preserves paused state; scheduled sounds start immediately.</summary>
    ValueTask<bool> Seek(string id, double position, CancellationToken cancellationToken = default);

    /// <summary>Stops a playback or cancels pending startup. Optional fade-out is 0 to 30 audio-clock seconds.</summary>
    ValueTask<bool> Stop(string id, double fadeOut = 0, CancellationToken cancellationToken = default);

    /// <summary>Stops all playbacks and cancels pending starts, optionally fading active sounds out.</summary>
    ValueTask StopAll(double fadeOut = 0, CancellationToken cancellationToken = default);

    /// <summary>Sets playback volume (0 to 1), optionally ramping over 0 to 30 seconds.</summary>
    ValueTask<bool> SetVolume(string id, double volume, double ramp = 0, CancellationToken cancellationToken = default);

    /// <summary>Sets stereo pan from -1 (left) to 1 (right).</summary>
    ValueTask<bool> SetPan(string id, double pan, CancellationToken cancellationToken = default);

    /// <summary>Sets playback speed (0.25 to 4), preserving current position. Pitch changes with speed.</summary>
    ValueTask<bool> SetPlaybackRate(string id, double playbackRate, CancellationToken cancellationToken = default);

    /// <summary>Sets master volume (0 to 1) for current and future sounds; does not unmute.</summary>
    ValueTask SetMasterVolume(double volume, CancellationToken cancellationToken = default);

    /// <summary>Mutes or unmutes all sounds without stopping their clocks or losing volume settings.</summary>
    ValueTask SetMuted(bool muted, CancellationToken cancellationToken = default);

    /// <summary>Sets the active playback limit (1 to 128; default 32). New playbacks fail at capacity; existing sounds are retained.</summary>
    ValueTask SetMaxConcurrent(int count, CancellationToken cancellationToken = default);

    /// <summary>Gets an active playback snapshot, or null when finished, stopped, or still loading.</summary>
    ValueTask<AudioPlaybackState?> GetState(string id, CancellationToken cancellationToken = default);

    /// <summary>Waits until an active playback ends or is stopped. Returns immediately if absent. Canceling this wait does not stop playback.</summary>
    ValueTask WaitForEnd(string id, CancellationToken cancellationToken = default);

}
