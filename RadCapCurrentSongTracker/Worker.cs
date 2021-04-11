using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RadCapCurrentSongTracker
{
    public class Worker : BackgroundService
    {
        private readonly IOptions<Options> _options;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly CurrentSongWriter _songNameWriter;

        public Worker(
            IOptions<Options> options,
            IHostApplicationLifetime appLifetime,
            CurrentSongWriter songNameWriter)
        {
            _options = options;
            _appLifetime = appLifetime;
            _songNameWriter = songNameWriter;
        }

        public override async Task StartAsync(CancellationToken forceStoppingToken)
        {
            forceStoppingToken.ThrowIfCancellationRequested();
            await _options.Value.InitAsync(forceStoppingToken);
            if (_options.Value.AreInvalid(out var reason))
            {
                Console.WriteLine(reason);
                Console.ReadKey(true);
                _appLifetime.StopApplication();
                return;
            }

            await base.StartAsync(forceStoppingToken);
        }

        public override async Task StopAsync(CancellationToken forceStoppingToken)
        {
            await _songNameWriter.StopAsync(forceStoppingToken);
            await base.StopAsync(forceStoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken forceStoppingToken)
        {
            try
            {
                Console.WriteLine($"Press any key to stop.{Environment.NewLine}");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(forceStoppingToken);
                var token = cts.Token;
                var stopByUserTask = Task.Run(() => Console.ReadKey(true), token);
                var songNameWriterTask = _songNameWriter.StartAsync(token);
                var stoppingTask = await Task.WhenAny(songNameWriterTask, stopByUserTask);
                if (stoppingTask == stopByUserTask)
                {
                    cts.Cancel();
                    Console.WriteLine($"{Environment.NewLine}Stopped by you.");
                }
                await stoppingTask;
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
            _appLifetime.StopApplication();
        }
    }
}