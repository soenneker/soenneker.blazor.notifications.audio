using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.Notifications.Audio.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Registrars;

namespace Soenneker.Blazor.Notifications.Audio.Registrars;

/// <summary>
/// Registration for the interop and utility services.
/// </summary>
public static class NotificationAudioRegistrar
{
    /// <summary>
    /// Adds <see cref="IAudioInterop"/> and <see cref="INotificationAudio"/> as scoped services.
    /// </summary>
    public static IServiceCollection AddNotificationAudioAsScoped(this IServiceCollection services)
    {
        services.AddModuleImportUtilAsScoped()
                .TryAddScoped<IAudioInterop, AudioInterop>();

        services.TryAddScoped<INotificationAudio, NotificationAudio>();

        return services;
    }
}
