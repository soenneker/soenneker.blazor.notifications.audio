using Soenneker.Blazor.Notifications.Audio.Enums;

namespace Soenneker.Blazor.Notifications.Audio.Models;

/// <summary>A snapshot of an active playback. Finished/stopped playbacks are removed and return null instead.</summary>
public sealed class AudioPlaybackState
{
    /// <summary>The unique playback ID returned by a play call.</summary>
    public string Id { get; set; } = "";
    /// <summary>One of scheduled, playing, paused, or stopping.</summary>
    public AudioPlaybackStatus Status { get; set; } = AudioPlaybackStatus.Scheduled;
    /// <summary>Current position in source seconds, accounting for speed and loops.</summary>
    public double Position { get; set; }
    /// <summary>Total source duration in seconds.</summary>
    public double Duration { get; set; }
    /// <summary>Target per-playback volume, excluding master volume and any in-progress fade.</summary>
    public double Volume { get; set; }
    /// <summary>Stereo position from -1 to 1.</summary>
    public double Pan { get; set; }
    /// <summary>Current speed multiplier.</summary>
    public double PlaybackRate { get; set; }
    /// <summary>Whether this playback repeats.</summary>
    public bool Loop { get; set; }
    /// <summary>Optional replacement-group key supplied when playback started.</summary>
    public string? Key { get; set; }
}
