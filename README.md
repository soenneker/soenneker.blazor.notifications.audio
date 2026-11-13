[![](https://img.shields.io/nuget/v/soenneker.blazor.notifications.audio.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.notifications.audio/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.notifications.audio/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.notifications.audio/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.blazor.notifications.audio.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.blazor.notifications.audio/)
[![](https://img.shields.io/badge/Demo-Live-blueviolet?style=for-the-badge&logo=github)](https://soenneker.github.io/soenneker.blazor.notifications.audio)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.blazor.notifications.audio/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.blazor.notifications.audio/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Blazor.Notifications.Audio
### Audio notifications for Blazor applications.

## Installation

```bash
dotnet add package Soenneker.Blazor.Notifications.Audio
```

## Setup

```csharp
using Soenneker.Blazor.Notifications.Audio.Registrars;

builder.Services.AddNotificationAudioAsScoped();
```

Inject `INotificationAudio` into an interactive component and initialize after render:

```razor
@using Soenneker.Blazor.Notifications.Audio.Abstract
@using Soenneker.Blazor.Notifications.Audio.Enums
@inject INotificationAudio Audio

<button @onclick="Notify">Notify</button>

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await Audio.Initialize();
    }

    private async Task Notify() => await Audio.Play(NotificationSound.Ding);
}
```

Initialization installs native gesture listeners so audio can unlock on the next pointer, touch, or keyboard interaction, including in Blazor Server. `Unlock()` reports whether activation succeeded. Browsers can block playback until the user interacts; `Play` throws a `JSException` with an actionable message when locked. Initialize before that interaction. Do not call during prerendering. No script tags or external sound assets are needed.

## Built-in sounds

`Ding`, `Chime`, `Bell`, `Pop`, `Click`, `Message`, `Success`, `Warning`, `Error`, `Complete`, `Ascending`, and `Descending` are original synthesized sounds. Each sound is rendered once per scope and reused until the cache is cleared.

```csharp
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;

string id = await Audio.Play(NotificationSound.Message, new AudioPlaybackOptions
{
    Volume = 0.7,
    Pan = -0.2,
    PlaybackRate = 1.1,
    FadeIn = 0.01,
    Key = "incoming-message",
    ReplaceExisting = true
});
```

Keys are optional. Replacement stops active sounds with the same key only after the new sound has loaded and validated. Otherwise sounds overlap independently. Playback rate changes both speed and pitch.

The sound, waveform and playback-status types use `Soenneker.Gen.EnumValues`: use `.List` for catalogs, `.Name` for labels, `.Value` for wire values, and `FromName`/`FromValue` for lookups. `Play()` defaults to `NotificationSound.Ding`.

## Custom audio files

```csharp
await Audio.Preload("sounds/new-order.mp3");
await Audio.RegisterUrl("new-order", "sounds/new-order.mp3");
string id = await Audio.PlayRegistered("new-order");

// Or play directly, including a data or blob URL:
string other = await Audio.PlayUrl("sounds/custom.wav");
```

Relative URLs resolve against the document base URI. Cross-origin audio requires CORS. Supported codecs depend on the browser; WAV, MP3 and other browser-decodable formats can be used. URLs are downloaded and decoded in full, so this is a notification library rather than a streaming music player. Blob URLs remain caller-owned; revoke them when no longer needed.

Loads share a cached promise, failures can be retried, and decoded buffers use an LRU cache. `Unregister(name)` removes a registration; `ClearCache()` evicts cached URL and preset buffers. Neither interrupts current sounds. Unregistering a URL does not evict its shared cached buffer.

## Compose your own sounds

```csharp
await Audio.RegisterTones("my-chime", new[]
{
    new AudioTone { Frequency = 523.25, Duration = 0.35, Release = 0.25 },
    new AudioTone { Frequency = 659.25, Start = 0.12, Duration = 0.35, Release = 0.25 },
    new AudioTone { Frequency = 783.99, Start = 0.24, Duration = 0.5, Release = 0.4 }
});
await Audio.PlayRegistered("my-chime");

await Audio.PlayTones(new[]
{
    new AudioTone
    {
        Frequency = 880,
        EndFrequency = 220,
        Duration = 0.4,
        Waveform = AudioWaveform.Triangle,
        Volume = 0.4,
        Attack = 0.01,
        Release = 0.3
    }
});
```

Tones support sine, triangle, square and sawtooth waves, linear frequency sweeps, amplitude, attack, release, and start offsets. Overlapping tones form chords; sums above full scale are normalized. Square/triangle/sawtooth synthesis limits harmonics to reduce aliasing. Attack plus release must fit within duration. A sequence may span at most 30 seconds, contain at most 128 tones, and have at most 60 seconds of combined tone durations. Frequency must be 20–20000 Hz and below the browser's Nyquist frequency.

## Playback controls

Every play call returns an ID. Controls return `false` for absent or inapplicable playback instead of creating it again.

```csharp
string id = await Audio.Play(NotificationSound.Bell, new AudioPlaybackOptions
{
    Loop = true,
    LoopStart = 0.1,
    LoopEnd = 0.8,
    Offset = 0.1,
    Delay = 0.2
});

await Audio.Pause(id);
await Audio.Seek(id, 0.3);
await Audio.SetPlaybackRate(id, 1.5);
await Audio.SetPan(id, 0.5);
await Audio.SetVolume(id, 0.25, ramp: 0.1);
await Audio.Resume(id);
var state = await Audio.GetState(id);
await Audio.Stop(id, fadeOut: 0.2);
await Audio.WaitForEnd(id);

await Audio.SetMasterVolume(0.5);
await Audio.SetMuted(true);
await Audio.StopAll();
```

Times are seconds. Positions and loop bounds refer to the source; delays and fades use the audio clock. Pausing a scheduled sound cancels its remaining delay; resuming or seeking an unpaused sound starts immediately. Seeking to the source end completes playback. For loops, an offset at or beyond the loop end restarts at the loop start. Fade-in is applied on start, resume, and seek. A suspended browser audio clock also suspends scheduled starts and fades.

`GetState` returns `null` once playback finishes or stops, and while it is still loading. `WaitForEnd` waits for an active playback to finish or stop and returns immediately when absent. Muting preserves volumes and playback positions. The library does not persist user preferences; applications can store and reapply them.

## Resource management

Each scoped interop instance owns an isolated audio context. DI disposal stops all sounds, resolves completion waiters, aborts downloads, releases nodes and buffers, and removes gesture listeners. The shared imported module remains owned by the module importer.

```csharp
await Audio.SetMaxConcurrent(16); // default 32; allowed 1–128
await Audio.ConfigureLimits(new AudioResourceLimits
{
    MaxCachedSounds = 32,
    MaxCacheBytes = 32 * 1024 * 1024,
    MaxDownloadBytes = 16 * 1024 * 1024,
    MaxDownloads = 8,
    DownloadTimeout = 30,
    MaxSoundDuration = 120
});
```

Limits reject new work instead of silently evicting active playbacks. Paused, looping and scheduled sounds consume playback slots. At most 128 starts can be pending. Cache limits exclude buffers retained by active playback and built-in presets. Setting `MaxCachedSounds` or `MaxCacheBytes` to zero disables retention; this also disables load deduplication for evicted entries. Changes to limits affect future loads; browser decoding cannot be interrupted once started. Decoded duration is checked after decoding.

Cancellation during playback startup removes that pending/active playback. Canceling a preload or completion wait cancels the .NET wait; a shared download or existing playback can continue. Canceling a control does not roll back a change already applied in JavaScript. Use `Stop` or `StopAll` for explicit playback cancellation. Download timeouts, format/network errors, invalid parameters and capacity failures are surfaced as exceptions rather than silently suppressed.

## Validation

```shell
node --test --test-timeout=10000 test/audiointerop.test.mjs
dotnet test --project test/Soenneker.Blazor.Notifications.Audio.Tests -- --treenode-filter "/*/*/NotificationAudioTests|AudioEnumValueTests/*"
```

The demo includes all presets, custom URL playback and registration, tone composition, master controls and playback inspection. `test/audiointerop.browser.js` can be evaluated in the running demo page after a user gesture to exercise real Web Audio synthesis, decoding, playback controls and cleanup. Browser autoplay rules still apply.
