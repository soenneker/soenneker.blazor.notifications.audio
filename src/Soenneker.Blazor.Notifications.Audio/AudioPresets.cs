using System;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;

namespace Soenneker.Blazor.Notifications.Audio;

/// <summary>Editable C# definitions of the built-in sounds.</summary>
public static class AudioPresets
{
    /// <summary>Creates an independent, mutable tone array for a built-in sound (Ding by default).
    /// Modify the tones, then pass them to INotificationAudio.PlayTones or RegisterTones.
    /// Changes do not affect future preset copies or built-in playback.</summary>
    public static AudioTone[] Create(NotificationSound? sound = null)
    {
        return (sound ?? NotificationSound.Ding).Value switch
        {
            0 => [Tone(1046.5, 0, .7), Tone(2093, 0, .45, .09)],
            1 => [Tone(659.25, 0, .4), Tone(830.61, .12, .4), Tone(987.77, .24, .6)],
            2 => [Tone(880, 0, 1, .32), Tone(1760, 0, .7, .12), Tone(2380, 0, .4, .04)],
            3 => [Tone(650, 0, .09, .5, 0, 180)],
            4 => [Tone(1600, 0, .025, .22, 2)],
            5 => [Tone(740, 0, .16), Tone(1108.73, .12, .3)],
            6 => [Tone(523.25, 0, .2), Tone(659.25, .12, .2), Tone(783.99, .24, .45)],
            7 => [Tone(440, 0, .2, .25, 1), Tone(440, .28, .2, .25, 1)],
            8 => [Tone(330, 0, .23, .28, 1), Tone(220, .19, .4, .3, 1)],
            9 => [Tone(523.25, 0, .5, .22), Tone(659.25, 0, .5, .18), Tone(783.99, 0, .65, .18), Tone(1046.5, .25, .6, .2)],
            10 => [Tone(440, 0, .15), Tone(554.37, .1, .15), Tone(659.25, .2, .15), Tone(880, .3, .3)],
            11 => [Tone(880, 0, .15), Tone(659.25, .1, .15), Tone(554.37, .2, .15), Tone(440, .3, .3)],
            12 => CreateTelephoneRing(),
            _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, "Unknown notification sound.")
        };
    }

    internal static AudioTone[][] CreateAll()
    {
        var sounds = new AudioTone[13][];
        for (int i = 0; i < sounds.Length; i++)
            sounds[i] = Create(NotificationSound.FromValue(i));
        return sounds;
    }

    private static AudioTone Tone(double frequency, double start = 0, double duration = .24, double volume = .35,
        int waveform = 0, double? endFrequency = null) => new()
    {
        Frequency = frequency, Start = start, Duration = duration, Volume = volume,
        Waveform = AudioWaveform.FromValue(waveform), EndFrequency = endFrequency,
        Attack = .004, Release = duration - .004
    };

    private static AudioTone[] CreateTelephoneRing()
    {
        var tones = new AudioTone[17];
        (double Frequency, double Start)[] notes = [(587.33, 0), (880, .24), (659.25, .78), (880, 1.02)];
        int index = 0;
        foreach (var (frequency, start) in notes)
        {
            tones[index++] = new AudioTone { Frequency = frequency, Start = start, Duration = .85, Volume = .16, Attack = .012, Release = .83 };
            tones[index++] = new AudioTone { Frequency = frequency / 2, Start = start, Duration = .65, Volume = .045, Attack = .018, Release = .63 };
            tones[index++] = new AudioTone { Frequency = frequency * 2, Start = start, Duration = .18, Volume = .035, Attack = .006, Release = .17 };
            tones[index++] = new AudioTone { Frequency = frequency, Start = start + .16, Duration = .7, Volume = .025, Attack = .025, Release = .67 };
        }
        // Preserve a four-second cycle, including silence between rings.
        tones[index] = Tone(440, 2, 2, 0);
        return tones;
    }
}
