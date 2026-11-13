using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class ScriptedJsReference : IJSObjectReference
{
    public List<string> Calls { get; } = [];
    public int DisposeCount { get; private set; }
    public Func<string, object?[]?, CancellationToken, ValueTask<object?>> Handler { get; set; } = (_, _, _) => ValueTask.FromResult<object?>(null);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, default, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Calls.Add(identifier);
        object? value = await Handler(identifier, args, cancellationToken);
        return value is null ? default! : (TValue)value;
    }

    public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
}
