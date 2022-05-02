using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RadCapCurrentSongTracker.Service;

public class Worker : BackgroundService
{
    private readonly IOptions<Settings> _options;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly CurrentSongUpdater _songNameWriter;

    public Worker(IOptions<Settings> options, IHostApplicationLifetime appLifetime, CurrentSongUpdater songNameWriter)
    {
        _options = options;
        _appLifetime = appLifetime;
        _songNameWriter = songNameWriter;
    }

    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        await _options.Value.InitAsync(stoppingToken);
        if (_options.Value.AreInvalid(out var reason))
        {
            Console.WriteLine(reason);
            Console.ReadKey(true);
            _appLifetime.StopApplication();
            return;
        }
        await base.StartAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await _songNameWriter.StopAsync(stoppingToken);
        await base.StopAsync(stoppingToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(
            async () =>
            {
                try
                {
                    await WorkerMain(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine($"{Environment.NewLine}Unexpected error. Application will be closed.");
                    Console.ReadKey(true);
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            },
            stoppingToken);

    private async Task WorkerMain(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Press any key to stop.{Environment.NewLine}");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var token = cts.Token;
        var stopByUserTask = Task.Run(() => Console.ReadKey(true), token);
        var songNameWriterTask = _songNameWriter.RunAsync(CancellationToken.None);
        var stoppingTask = await Task.WhenAny(songNameWriterTask, stopByUserTask);
        if (stoppingTask == stopByUserTask)
        {
            cts.Cancel();
            Console.WriteLine($"{Environment.NewLine}Stopped by you.");
        }
        await stoppingTask;
    }
}

internal static partial class Extensions
{
    public static IServiceCollection AddHostedSongWriter(this IServiceCollection services) =>
        services.AddHostedService<Worker>().AddCurrentSongWriter();
}
