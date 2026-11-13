using Soenneker.Gen.EnumValues;

namespace Soenneker.Blazor.Notifications.Audio.Enums;

/// <summary>Original, locally synthesized notification sounds; no audio downloads are required.</summary>
[EnumValue]
public sealed partial class NotificationSound
{
    /// <summary>A bright two-partial ding.</summary>
    public static readonly NotificationSound Ding = new(0);
    /// <summary>A rising three-note chime.</summary>
    public static readonly NotificationSound Chime = new(1);
    /// <summary>A longer bell with layered partials.</summary>
    public static readonly NotificationSound Bell = new(2);
    /// <summary>A short downward pitch sweep.</summary>
    public static readonly NotificationSound Pop = new(3);
    /// <summary>A brief percussive click.</summary>
    public static readonly NotificationSound Click = new(4);
    /// <summary>A two-note incoming-message cue.</summary>
    public static readonly NotificationSound Message = new(5);
    /// <summary>A rising major arpeggio.</summary>
    public static readonly NotificationSound Success = new(6);
    /// <summary>Two repeated warning tones.</summary>
    public static readonly NotificationSound Warning = new(7);
    /// <summary>A low descending error cue.</summary>
    public static readonly NotificationSound Error = new(8);
    /// <summary>A major chord with a final high note.</summary>
    public static readonly NotificationSound Complete = new(9);
    /// <summary>Four ascending notes.</summary>
    public static readonly NotificationSound Ascending = new(10);
    /// <summary>Four descending notes.</summary>
    public static readonly NotificationSound Descending = new(11);
}
