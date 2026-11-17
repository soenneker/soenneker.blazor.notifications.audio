using System;
using System.Linq;
using System.Threading.Tasks;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Options;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class AudioPresetTests
{
    [Test]
    public void PresetsAreIndependentAndPreserveTelephoneCadence()
    {
        AudioTone[] tones = AudioPresets.Create(NotificationSound.TelephoneRing);
        if (tones.Length != 17 || tones.Max(t => t.Start + t.Duration) != 4 || tones[^1].Volume != 0)
            throw new InvalidOperationException("Telephone cadence must include its silent pause.");
        tones[0].Frequency = 100;
        tones[^1].Duration = 1;
        AudioTone[] fresh = AudioPresets.Create(NotificationSound.TelephoneRing);
        if (fresh[0].Frequency != 587.33 || fresh[^1].Duration != 2)
            throw new InvalidOperationException("Editing a preset must not modify future copies.");
    }

    [Test]
    public async Task InitializationSuppliesEveryCSharpPresetToTheBrowser()
    {
        AudioTone[][]? supplied = null;
        var engine = new ScriptedJsReference();
        var module = new ScriptedJsReference
        {
            Handler = (_, args, _) =>
            {
                supplied = args![0] as AudioTone[][];
                return ValueTask.FromResult<object?>(engine);
            }
        };
        await using var interop = new AudioInterop(new StubModuleImport(module));
        await interop.Initialize();
        if (supplied is null || supplied.Length != NotificationSound.List.Count)
            throw new InvalidOperationException("All C# definitions must reach the browser.");
        foreach (AudioTone[] tones in supplied)
        {
            if (tones.Length == 0 || tones.Any(t => t.Attack + t.Release > t.Duration) || tones.Sum(t => t.Duration) > 60)
                throw new InvalidOperationException("Preset envelopes must be valid.");
        }
        if (supplied[12][0].Frequency != 587.33 || supplied[12].Max(t => t.Start + t.Duration) != 4)
            throw new InvalidOperationException("Telephone mapping must be preserved.");
    }
}
