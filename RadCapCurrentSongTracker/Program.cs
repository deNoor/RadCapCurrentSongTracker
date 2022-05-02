using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RadCapCurrentSongTracker;

public class Program
{
    public static async Task Main() => await CreateHostBuilder(Array.Empty<string>()).Build().RunAsync();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
           .ConfigureHostConfiguration(
                builder =>
                {
                    builder.Sources.Clear();
                    builder.AddJsonFile(Settings.FullPath, true, false);
                })
           .ConfigureServices(
                (hostContext, services) =>
                {
                    services.AddLogging(c => c.ClearProviders());
                    services.Configure<Settings>(hostContext.Configuration);
                    services.AddCurrentSongWriter();
                    services.AddHostedService<Worker>();
                });
}
