// All presets are original synthesized tones. No third-party recordings or network dependencies.
const tone = (frequency, start = 0, duration = .24, volume = .35, waveform = 0, endFrequency = null) =>
    ({ frequency, start, duration, volume, waveform, endFrequency, attack: .004, release: duration - .004 });
export const presets = [
    [tone(1046.5, 0, .7), tone(2093, 0, .45, .09)], // Ding
    [tone(659.25, 0, .4), tone(830.61, .12, .4), tone(987.77, .24, .6)], // Chime
    [tone(880, 0, 1, .32), tone(1760, 0, .7, .12), tone(2380, 0, .4, .04)], // Bell
    [tone(650, 0, .09, .5, 0, 180)], // Pop
    [tone(1600, 0, .025, .22, 2)], // Click
    [tone(740, 0, .16), tone(1108.73, .12, .3)], // Message
    [tone(523.25, 0, .2), tone(659.25, .12, .2), tone(783.99, .24, .45)], // Success
    [tone(440, 0, .2, .25, 1), tone(440, .28, .2, .25, 1)], // Warning
    [tone(330, 0, .23, .28, 1), tone(220, .19, .4, .3, 1)], // Error
    [tone(523.25, 0, .5, .22), tone(659.25, 0, .5, .18), tone(783.99, 0, .65, .18), tone(1046.5, .25, .6, .2)], // Complete
    [tone(440, 0, .15), tone(554.37, .1, .15), tone(659.25, .2, .15), tone(880, .3, .3)],
    [tone(880, 0, .15), tone(659.25, .1, .15), tone(554.37, .2, .15), tone(440, .3, .3)],
    // TelephoneRing: a rounded mallet motif (D-A-E-A), in two answering pairs.
    // Brief upper partials add definition; the quieter delayed notes give a soft echo.
    [[587.33, 0], [880, .24], [659.25, .78], [880, 1.02]].flatMap(([frequency, start]) => [
        { ...tone(frequency, start, .85, .16), attack: .012, release: .83 },
        { ...tone(frequency / 2, start, .65, .045), attack: .018, release: .63 },
        { ...tone(frequency * 2, start, .18, .035), attack: .006, release: .17 },
        { ...tone(frequency, start + .16, .7, .025), attack: .025, release: .67 }
    ]).concat([tone(440, 2, 2, 0)])
];

