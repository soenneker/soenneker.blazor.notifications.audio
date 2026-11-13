using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Soenneker.Blazor.MockJsRuntime.Registrars;
using Soenneker.TestHosts.Unit;
using Soenneker.Utils.Test;
using Soenneker.Blazor.Notifications.Audio.Registrars;

namespace Soenneker.Blazor.Notifications.Audio.Tests;

public sealed class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        SetupIoC(Services);

        return base.InitializeAsync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: false);
        });

        IConfiguration config = TestUtil.BuildConfig();
        services.AddSingleton(config);

        services.AddNotificationAudioAsScoped();
        services.AddMockJsRuntimeAsScoped();
    }
}
