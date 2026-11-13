using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;
using Soenneker.Blazor.Notifications.Audio.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Notifications.Audio.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;

namespace Soenneker.Blazor.Notifications.Audio;

public sealed class AudioInterop : IAudioInterop
{
    private const string ModulePath = "./_content/Soenneker.Blazor.Notifications.Audio/js/audiointerop.js";
    private readonly IModuleImportUtil _modules;
    private readonly AsyncInitializer _initializer;
    private IJSObjectReference? _engine;
    private readonly object _sync = new();
    private Task? _disposeTask;
    private volatile bool _disposed;

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(AudioPlaybackOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(AudioTone))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(AudioResourceLimits))]
    public AudioInterop(IModuleImportUtil modules)
    {
        _modules = modules;
        _initializer = new AsyncInitializer(CreateEngine);
    }

    public ValueTask ConfigureLimits(AudioResourceLimits limits, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return Invoke("configureLimits", cancellationToken, limits);
    }

    private async ValueTask CreateEngine(CancellationToken cancellationToken)
    {
        IJSObjectReference module = await _modules.GetContentModuleReference(ModulePath, cancellationToken);
        // Once creation starts, receive its reference even if the caller cancels, so it can always be disposed.
        _engine = await module.InvokeAsync<IJSObjectReference>("create");
    }

    public async ValueTask Initialize(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _initializer.Init(cancellationToken);
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private async ValueTask<T> Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string method, CancellationToken cancellationToken, params object?[] args)
    {
        await Initialize(cancellationToken);
        return await _engine!.InvokeAsync<T>(method, cancellationToken, args);
    }

    private async ValueTask Invoke(string method, CancellationToken cancellationToken, params object?[] args)
    {
        await Initialize(cancellationToken);
        await _engine!.InvokeVoidAsync(method, cancellationToken, args);
    }

    private async ValueTask<string> Start(string kind, object value, AudioPlaybackOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        await Initialize(cancellationToken);
        string id = Guid.NewGuid().ToString("N");
        try
        {
            await _engine!.InvokeVoidAsync("play", cancellationToken, id, kind, value, options);
            cancellationToken.ThrowIfCancellationRequested();
            return id;
        }
        catch
        {
            try { await _engine!.InvokeVoidAsync("stop", id, 0); }
            catch (JSException) { }
            catch (ObjectDisposedException) { }
            throw;
        }
    }

    public ValueTask<bool> Unlock(CancellationToken cancellationToken = default) =>
        Invoke<bool>("unlock", cancellationToken);

    public ValueTask<string> Play(NotificationSound? sound = null, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        Start("preset", (sound ?? NotificationSound.Ding).Value, options, cancellationToken);

    public ValueTask<string> PlayUrl(string url, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        Start("url", url, options, cancellationToken);

    public ValueTask<string> PlayTones(IReadOnlyList<AudioTone> tones, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        Start("tones", tones, options, cancellationToken);

    public ValueTask RegisterUrl(string name, string url, CancellationToken cancellationToken = default) =>
        Invoke("registerUrl", cancellationToken, name, url);

    public ValueTask RegisterTones(string name, IReadOnlyList<AudioTone> tones, CancellationToken cancellationToken = default) =>
        Invoke("registerTones", cancellationToken, name, tones);

    public ValueTask<string> PlayRegistered(string name, AudioPlaybackOptions? options = null, CancellationToken cancellationToken = default) =>
        Start("registered", name, options, cancellationToken);

    public ValueTask<bool> Unregister(string name, CancellationToken cancellationToken = default) =>
        Invoke<bool>("unregister", cancellationToken, name);

    public ValueTask Preload(string url, CancellationToken cancellationToken = default) =>
        Invoke("preload", cancellationToken, url);

    public ValueTask ClearCache(CancellationToken cancellationToken = default) =>
        Invoke("clearCache", cancellationToken);

    public ValueTask<bool> Pause(string id, CancellationToken cancellationToken = default) =>
        Invoke<bool>("pause", cancellationToken, id);

    public ValueTask<bool> Resume(string id, CancellationToken cancellationToken = default) =>
        Invoke<bool>("resume", cancellationToken, id);

    public ValueTask<bool> Seek(string id, double position, CancellationToken cancellationToken = default) =>
        Invoke<bool>("seek", cancellationToken, id, position);

    public ValueTask<bool> Stop(string id, double fadeOut = 0, CancellationToken cancellationToken = default) =>
        Invoke<bool>("stop", cancellationToken, id, fadeOut);

    public ValueTask StopAll(double fadeOut = 0, CancellationToken cancellationToken = default) =>
        Invoke("stopAll", cancellationToken, fadeOut);

    public ValueTask<bool> SetVolume(string id, double volume, double ramp = 0, CancellationToken cancellationToken = default) =>
        Invoke<bool>("setVolume", cancellationToken, id, volume, ramp);

    public ValueTask<bool> SetPan(string id, double pan, CancellationToken cancellationToken = default) =>
        Invoke<bool>("setPan", cancellationToken, id, pan);

    public ValueTask<bool> SetPlaybackRate(string id, double playbackRate, CancellationToken cancellationToken = default) =>
        Invoke<bool>("setPlaybackRate", cancellationToken, id, playbackRate);

    public ValueTask SetMasterVolume(double volume, CancellationToken cancellationToken = default) =>
        Invoke("setMasterVolume", cancellationToken, volume);

    public ValueTask SetMuted(bool muted, CancellationToken cancellationToken = default) =>
        Invoke("setMuted", cancellationToken, muted);

    public ValueTask SetMaxConcurrent(int count, CancellationToken cancellationToken = default) =>
        Invoke("setMaxConcurrent", cancellationToken, count);

    public ValueTask<AudioPlaybackState?> GetState(string id, CancellationToken cancellationToken = default) =>
        Invoke<AudioPlaybackState?>("getState", cancellationToken, id);

    public ValueTask WaitForEnd(string id, CancellationToken cancellationToken = default) =>
        Invoke("waitForEnd", cancellationToken, id);

    public ValueTask DisposeAsync()
    {
        lock (_sync)
            return new ValueTask(_disposeTask ??= DisposeCore());
    }

    private async Task DisposeCore()
    {
        _disposed = true;
        // AsyncInitializer disposal waits for an in-flight initialization before releasing its state.
        await _initializer.DisposeAsync();
        if (_engine is null)
            return;
        try { await _engine.InvokeVoidAsync("dispose"); }
        catch (JSDisconnectedException) { }
        finally
        {
            try { await _engine.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }
}
