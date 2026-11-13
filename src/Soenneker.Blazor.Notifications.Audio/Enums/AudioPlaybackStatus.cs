using Soenneker.Gen.EnumValues;

namespace Soenneker.Blazor.Notifications.Audio.Enums;

/// <summary>The current state of an active playback.</summary>
[EnumValue<string>]
public sealed partial class AudioPlaybackStatus
{
    /// <summary>Waiting for the requested start time.</summary>
    public static readonly AudioPlaybackStatus Scheduled = new("scheduled");
    /// <summary>Playback is advancing with the audio clock.</summary>
    public static readonly AudioPlaybackStatus Playing = new("playing");
    /// <summary>Playback is paused at a saved position.</summary>
    public static readonly AudioPlaybackStatus Paused = new("paused");
    /// <summary>Playback is fading out before stopping.</summary>
    public static readonly AudioPlaybackStatus Stopping = new("stopping");
}
