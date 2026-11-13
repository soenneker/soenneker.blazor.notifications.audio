using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Blazor.Utils.ModuleImport.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Dtos;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class StubModuleImport(IJSObjectReference module) : IModuleImportUtil
{
    public int Imports { get; private set; }
    public ValueTask<IJSObjectReference> GetContentModuleReference(string path, CancellationToken cancellationToken = default)
    {
        Imports++;
        return ValueTask.FromResult(module);
    }
    public ValueTask<ModuleImportItem> GetContentModule(string path, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<ModuleImportItem> GetExternalModule(string url, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<IJSObjectReference> GetExternalModuleReference(string url, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public ValueTask<bool> DisposeContentModule(string name) => throw new InvalidOperationException("Interop must not evict the shared module.");
    public ValueTask<bool> DisposeExternalModule(string url) => throw new NotSupportedException();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
