namespace Soenneker.Blazor.Notifications.Audio.Options;

/// <summary>Settings for one playback. All time values are seconds. Options are copied when playback starts.</summary>
public sealed class AudioPlaybackOptions
{
    /// <summary>Playback gain from 0 to 1, multiplied by master volume.</summary>
    public double Volume { get; set; } = 1;
    /// <summary>Stereo position from -1 (left) to 1 (right).</summary>
    public double Pan { get; set; }
    /// <summary>Speed multiplier from 0.25 to 4. Changing speed also changes pitch.</summary>
    public double PlaybackRate { get; set; } = 1;
    /// <summary>Whether to repeat until stopped. Looping sounds consume a playback slot until stopped.</summary>
    public bool Loop { get; set; }
    /// <summary>Loop start offset in source seconds.</summary>
    public double LoopStart { get; set; }
    /// <summary>Loop end offset in source seconds; null means the end of the sound.</summary>
    public double? LoopEnd { get; set; }
    /// <summary>Initial position in source seconds.</summary>
    public double Offset { get; set; }
    /// <summary>Delay before starting, in audio-clock seconds (0 to 3600).</summary>
    public double Delay { get; set; }
    /// <summary>Fade-in duration in audio-clock seconds (0 to 30), also applied on resume and seek.</summary>
    public double FadeIn { get; set; }
    /// <summary>Optional grouping key for replacing an earlier notification, such as an incoming-call sound.</summary>
    public string? Key { get; set; }
    /// <summary>Stops existing playbacks with the same nonempty key when this sound is ready to start.</summary>
    public bool ReplaceExisting { get; set; }
}
