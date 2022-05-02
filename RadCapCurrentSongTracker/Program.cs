using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadCapCurrentSongTracker.Service;

namespace RadCapCurrentSongTracker;

public class Program
{
    public static async Task Main() => await CreateHostBuilder(Array.Empty<string>()).Build().RunAsync();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
           .UseConsoleLifetime()
           .ConfigureHostConfiguration(
                builder =>
                {
                    builder.Sources.Clear();
                    builder.AddJsonFile(Settings.FullPath, true, false);
                })
           .ConfigureLogging(builder => builder.ClearProviders())
           .ConfigureServices((hostContext, services) => services.Configure<Settings>(hostContext.Configuration).AddHostedSongWriter())
           .UseDefaultServiceProvider(
                options =>
                {
#if DEBUG
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
#endif
                });
}
