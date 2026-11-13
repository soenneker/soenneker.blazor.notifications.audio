using Soenneker.Gen.EnumValues;

namespace Soenneker.Blazor.Notifications.Audio.Enums;

/// <summary>The oscillator shape used to synthesize a custom tone.</summary>
[EnumValue]
public sealed partial class AudioWaveform
{
    /// <summary>A single-frequency sine wave.</summary>
    public static readonly AudioWaveform Sine = new(0);
    /// <summary>A soft wave with odd harmonics.</summary>
    public static readonly AudioWaveform Triangle = new(1);
    /// <summary>A bright square wave with odd harmonics.</summary>
    public static readonly AudioWaveform Square = new(2);
    /// <summary>A bright wave with both odd and even harmonics.</summary>
    public static readonly AudioWaveform Sawtooth = new(3);
}
