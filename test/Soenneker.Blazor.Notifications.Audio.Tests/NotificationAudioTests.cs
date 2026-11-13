using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Blazor.Notifications.Audio.Abstract;
using Soenneker.Blazor.Notifications.Audio.Registrars;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class NotificationAudioTests
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [Test]
    public async Task ConcurrentInitializationCreatesOneEngineAndDisposesItOnce()
    {
        var engine = new ScriptedJsReference();
        var module = new ScriptedJsReference { Handler = async (_, _, _) => { await Task.Yield(); return engine; } };
        var modules = new StubModuleImport(module);
        var interop = new AudioInterop(modules);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => interop.Initialize().AsTask()));
        Require(modules.Imports == 1 && module.Calls.Count == 1, "Initialization must be shared.");
        await Task.WhenAll(interop.DisposeAsync().AsTask(), interop.DisposeAsync().AsTask());
        Require(engine.Calls.SequenceEqual(["dispose"]) && engine.DisposeCount == 1, "Engine must be disposed exactly once.");
        Require(module.DisposeCount == 0, "The shared module belongs to the module importer.");
    }

    [Test]
    public async Task FailedInitializationCanBeRetried()
    {
        var engine = new ScriptedJsReference();
        int attempt = 0;
        var module = new ScriptedJsReference { Handler = (_, _, _) => ++attempt == 1 ? throw new JSException("import failed") : ValueTask.FromResult<object?>(engine) };
        await using var interop = new AudioInterop(new StubModuleImport(module));
        try { await interop.Initialize(); throw new InvalidOperationException("Expected failure."); }
        catch (JSException) { }
        await interop.Initialize();
        Require(attempt == 2, "A failed initialization must not poison the scope.");
    }

    [Test]
    public async Task CancellationStopsTheExactPendingPlayback()
    {
        using var cancellation = new CancellationTokenSource();
        string? started = null, stopped = null;
        var engine = new ScriptedJsReference
        {
            Handler = async (method, args, token) =>
            {
                if (method == "play")
                {
                    started = (string)args![0]!;
                    cancellation.Cancel();
                    await Task.Delay(Timeout.Infinite, token);
                }
                if (method == "stop") stopped = (string)args![0]!;
                return null;
            }
        };
        var module = new ScriptedJsReference { Handler = (_, _, _) => ValueTask.FromResult<object?>(engine) };
        await using var interop = new AudioInterop(new StubModuleImport(module));
        try { await interop.Play(cancellationToken: cancellation.Token); throw new InvalidOperationException("Expected cancellation."); }
        catch (OperationCanceledException) { }
        Require(started is not null && stopped == started, "Canceled playback must be cleaned up by ID.");
    }

    [Test]
    public async Task DisposalDuringCreationWaitsForAndCleansUpTheEngine()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var engine = new ScriptedJsReference();
        var module = new ScriptedJsReference { Handler = async (_, _, _) => { entered.SetResult(); await release.Task; return engine; } };
        var interop = new AudioInterop(new StubModuleImport(module));
        Task initializing = interop.Initialize().AsTask();
        await entered.Task;
        Task disposing = interop.DisposeAsync().AsTask();
        Require(!disposing.IsCompleted, "Disposal must wait for an in-flight creation.");
        release.SetResult();
        try { await initializing; throw new InvalidOperationException("Expected disposed failure."); }
        catch (ObjectDisposedException) { }
        await disposing;
        Require(engine.DisposeCount == 1 && engine.Calls.Contains("dispose"), "Created engine must not leak.");
    }

    [Test]
    public async Task DisconnectedDisposalIsSafe()
    {
        var engine = new ScriptedJsReference { Handler = (_, _, _) => throw new JSDisconnectedException("circuit closed") };
        var module = new ScriptedJsReference { Handler = (_, _, _) => ValueTask.FromResult<object?>(engine) };
        var interop = new AudioInterop(new StubModuleImport(module));
        await interop.Initialize();
        await interop.DisposeAsync();
        Require(engine.DisposeCount == 1, "Object reference must still be released after disconnect.");
    }

    [Test]
    public async Task RegistrationIsIdempotentAndScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, UnusedJsRuntime>();
        services.AddNotificationAudioAsScoped().AddNotificationAudioAsScoped();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope first = provider.CreateAsyncScope();
        await using AsyncServiceScope second = provider.CreateAsyncScope();
        var a = first.ServiceProvider.GetRequiredService<INotificationAudio>();
        Require(ReferenceEquals(a, first.ServiceProvider.GetRequiredService<INotificationAudio>()), "Same scope must share a service.");
        Require(!ReferenceEquals(a, second.ServiceProvider.GetRequiredService<INotificationAudio>()), "Different scopes must be isolated.");
        Require(services.Count(d => d.ServiceType == typeof(IAudioInterop)) == 1, "Repeated registration must not duplicate interop.");
    }
}
