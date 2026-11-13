using Soenneker.Blazor.Notifications.Audio.Enums;

namespace Soenneker.Blazor.Notifications.Audio.Options;

/// <summary>A tone in a custom sound. Times are seconds; overlapping tones form chords. Sequences are limited to 30 seconds, 128 tones, and 60 seconds of combined tone duration.</summary>
public sealed class AudioTone
{
    /// <summary>Starting frequency in Hz, from 20 to 20000 (also limited by the browser sample rate).</summary>
    public double Frequency { get; set; } = 880;
    /// <summary>Optional ending frequency for a linear pitch sweep.</summary>
    public double? EndFrequency { get; set; }
    /// <summary>Offset from the beginning of the sequence, in seconds.</summary>
    public double Start { get; set; }
    /// <summary>Tone length, including attack and release, in seconds.</summary>
    public double Duration { get; set; } = 0.25;
    /// <summary>Amplitude from 0 to 1. Overlapping tones are peak-normalized if necessary.</summary>
    public double Volume { get; set; } = 0.5;
    /// <summary>Oscillator shape. Sine produces the softest sound.</summary>
    public AudioWaveform Waveform { get; set; } = AudioWaveform.Sine;
    /// <summary>Attack length in seconds. Attack plus release must not exceed duration.</summary>
    public double Attack { get; set; } = 0.005;
    /// <summary>Release length in seconds.</summary>
    public double Release { get; set; } = 0.08;
}
