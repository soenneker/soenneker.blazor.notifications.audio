using System;

namespace Soenneker.Blazor.Notifications.Audio.Abstract;

/// <summary>Owns the browser audio engine and its lifetime. Disposal stops sounds, cancels downloads, and removes gesture listeners.</summary>
public interface IAudioInterop : INotificationAudio, IAsyncDisposable;
