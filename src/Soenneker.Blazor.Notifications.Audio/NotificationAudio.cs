using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;
using Soenneker.Blazor.Notifications.Audio.Models;
using System;
using Soenneker.Blazor.Notifications.Audio.Abstract;

namespace Soenneker.Blazor.Notifications.Audio;

public sealed class NotificationAudio(IAudioInterop interop) : INotificationAudio
{
    private readonly IAudioInterop _interop = interop ?? throw new ArgumentNullException(nameof(interop));

    public ValueTask ConfigureLimits(AudioResourceLimits limits, CancellationToken cancellationToken = default) =>
        _interop.ConfigureLimits(limits, cancellationToken);

    public ValueTask Initialize(CancellationToken cancellationToken = default) =>
        _interop.Initialize(cancellationToken);

    public ValueTask<bool> Unlock(CancellationToken cancellationToken = default) =>
        _interop.Unlock(cancellationToken);

    public ValueTask<string> Play(NotificationSound? sound = null, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        _interop.Play(sound, options, cancellationToken);

    public ValueTask<string> PlayUrl(string url, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        _interop.PlayUrl(url, options, cancellationToken);

    public ValueTask<string> PlayTones(IReadOnlyList<AudioTone> tones, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        _interop.PlayTones(tones, options, cancellationToken);

    public ValueTask RegisterUrl(string name, string url, CancellationToken cancellationToken = default) =>
        _interop.RegisterUrl(name, url, cancellationToken);

    public ValueTask RegisterTones(string name, IReadOnlyList<AudioTone> tones, CancellationToken cancellationToken = default) =>
        _interop.RegisterTones(name, tones, cancellationToken);

    public ValueTask<string> PlayRegistered(string name, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        _interop.PlayRegistered(name, options, cancellationToken);

    public ValueTask<bool> Unregister(string name, CancellationToken cancellationToken = default) =>
        _interop.Unregister(name, cancellationToken);

    public ValueTask Preload(string url, CancellationToken cancellationToken = default) =>
        _interop.Preload(url, cancellationToken);

    public ValueTask ClearCache(CancellationToken cancellationToken = default) =>
        _interop.ClearCache(cancellationToken);

    public ValueTask<bool> Pause(string id, CancellationToken cancellationToken = default) =>
        _interop.Pause(id, cancellationToken);

    public ValueTask<bool> Resume(string id, CancellationToken cancellationToken = default) =>
        _interop.Resume(id, cancellationToken);

    public ValueTask<bool> Seek(string id, double position, CancellationToken cancellationToken = default) =>
        _interop.Seek(id, position, cancellationToken);

    public ValueTask<bool> Stop(string id, double fadeOut = 0, CancellationToken cancellationToken = default) =>
        _interop.Stop(id, fadeOut, cancellationToken);

    public ValueTask StopAll(double fadeOut = 0, CancellationToken cancellationToken = default) =>
        _interop.StopAll(fadeOut, cancellationToken);

    public ValueTask<bool> SetVolume(string id, double volume, double ramp = 0, CancellationToken cancellationToken = default) =>
        _interop.SetVolume(id, volume, ramp, cancellationToken);

    public ValueTask<bool> SetPan(string id, double pan, CancellationToken cancellationToken = default) =>
        _interop.SetPan(id, pan, cancellationToken);

    public ValueTask<bool> SetPlaybackRate(string id, double playbackRate, CancellationToken cancellationToken = default) =>
        _interop.SetPlaybackRate(id, playbackRate, cancellationToken);

    public ValueTask SetMasterVolume(double volume, CancellationToken cancellationToken = default) =>
        _interop.SetMasterVolume(volume, cancellationToken);

    public ValueTask SetMuted(bool muted, CancellationToken cancellationToken = default) =>
        _interop.SetMuted(muted, cancellationToken);

    public ValueTask SetMaxConcurrent(int count, CancellationToken cancellationToken = default) =>
        _interop.SetMaxConcurrent(count, cancellationToken);

    public ValueTask<AudioPlaybackState?> GetState(string id, CancellationToken cancellationToken = default) =>
        _interop.GetState(id, cancellationToken);

    public ValueTask WaitForEnd(string id, CancellationToken cancellationToken = default) =>
        _interop.WaitForEnd(id, cancellationToken);

}
