using System;
using System.Text.Json;
using System.Threading.Tasks;
using Soenneker.Blazor.Notifications.Audio.Enums;
using Soenneker.Blazor.Notifications.Audio.Models;
using Soenneker.Blazor.Notifications.Audio.Options;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class AudioEnumValueTests
{
    [Test]
    public void EnumValuesPreserveTheBrowserWireFormat()
    {
        if (NotificationSound.List.Count != 12 || AudioWaveform.List.Count != 4)
            throw new InvalidOperationException("Preset catalog does not match the browser engine.");
        for (int i = 0; i < NotificationSound.List.Count; i++)
            if (NotificationSound.FromValue(i).Value != i)
                throw new InvalidOperationException("Invalid sound mapping.");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using JsonDocument tone = JsonDocument.Parse(JsonSerializer.Serialize(new AudioTone { Waveform = AudioWaveform.Triangle }, options));
        if (tone.RootElement.GetProperty("waveform").GetInt32() != 1)
            throw new InvalidOperationException("Waveforms must serialize as their numeric values.");
        AudioPlaybackState state = JsonSerializer.Deserialize<AudioPlaybackState>("{\"status\":\"paused\"}", options)!;
        if (state.Status != AudioPlaybackStatus.Paused)
            throw new InvalidOperationException("Playback status must deserialize from its string value.");
    }

    [Test]
    public async Task DefaultPlayUsesDingValue()
    {
        object? suppliedValue = null;
        var engine = new ScriptedJsReference
        {
            Handler = (method, args, _) =>
            {
                if (method == "play") suppliedValue = args![2];
                return ValueTask.FromResult<object?>(null);
            }
        };
        var module = new ScriptedJsReference { Handler = (_, _, _) => ValueTask.FromResult<object?>(engine) };
        await using var interop = new AudioInterop(new StubModuleImport(module));
        await interop.Play();
        if (suppliedValue is not int value || value != NotificationSound.Ding.Value)
            throw new InvalidOperationException("Default playback must use Ding's underlying value.");
    }
}
