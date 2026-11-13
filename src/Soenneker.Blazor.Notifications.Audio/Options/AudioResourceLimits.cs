namespace Soenneker.Blazor.Notifications.Audio.Options;

/// <summary>Per-scope resource limits. Configure before loading sounds. Changing limits does not interrupt existing playback or downloads.</summary>
public sealed class AudioResourceLimits
{
    /// <summary>Maximum cached URL buffers (0 disables caching; default 32).</summary>
    public int MaxCachedSounds { get; set; } = 32;
    /// <summary>Maximum decoded URL cache size in bytes (default 32 MiB). Active playback buffers are not included.</summary>
    public int MaxCacheBytes { get; set; } = 32 * 1024 * 1024;
    /// <summary>Maximum encoded size per download in bytes (default 16 MiB).</summary>
    public int MaxDownloadBytes { get; set; } = 16 * 1024 * 1024;
    /// <summary>Maximum simultaneous URL downloads/decodes (default 8).</summary>
    public int MaxDownloads { get; set; } = 8;
    /// <summary>Download timeout in seconds (default 30). Browser decoding cannot be interrupted.</summary>
    public double DownloadTimeout { get; set; } = 30;
    /// <summary>Maximum decoded sound duration in seconds (default 120).</summary>
    public double MaxSoundDuration { get; set; } = 120;
}
