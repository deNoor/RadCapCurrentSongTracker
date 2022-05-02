using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RadCapCurrentSongTracker.Service;

public class CurrentSongUpdater
{
    private const int MinUpdateIntervalInSeconds = 1;
    private const int MaxUpdateIntervalInSeconds = 60;
    private const string Txt = ".txt";

    private readonly ILogger<CurrentSongUpdater> _logger;
    private readonly SongTracker _songTracker;
    private readonly TimeSpan _updateInterval;
    private readonly string _directory;
    private readonly Dictionary<string, string> _stations;

    public CurrentSongUpdater(ILogger<CurrentSongUpdater> logger, SongTracker songTracker, IOptions<Settings> options)
    {
        _logger = logger;
        _songTracker = songTracker;
        _updateInterval = ToSafeUpdateInterval(options.Value.UpdateIntervalInSeconds);
        _directory = options.Value.Directory!;
        _stations = options.Value.Stations!;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        EnsureDirectory();
        await RunAllAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => ResetFilesAsync(cancellationToken);

    private static TimeSpan ToSafeUpdateInterval(double? seconds)
    {
        seconds = Math.Max(MinUpdateIntervalInSeconds, Math.Min(seconds ?? MinUpdateIntervalInSeconds, MaxUpdateIntervalInSeconds));
        return TimeSpan.FromSeconds(seconds.Value);
    }

    private async Task RunAllAsync(CancellationToken cancellationToken)
    {
        var tasks = _stations.Keys.Select(station => RunSongUpdaterAsync(station, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task ResetFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_directory))
        {
            return;
        }
        var tasks = Directory.EnumerateFiles(_directory).Select(x => File.WriteAllTextAsync(x, string.Empty, cancellationToken));
        await Task.WhenAll(tasks);
        _logger.LogInformation("Song names are cleared.");
    }

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_directory))
        {
            Directory.CreateDirectory(_directory);
        }
    }

    private async Task RunSongUpdaterAsync(string station, CancellationToken token)
    {
        var oldSongName = string.Empty;
        var path = Path.ChangeExtension(Path.Combine(_directory, station), Txt);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var songName = await _songTracker.NowPlaying(station, token);
                if (songName != oldSongName)
                {
                    await File.WriteAllTextAsync(path, songName, token);
                    oldSongName = songName;
                    _logger.LogInformation($"{station} -> {songName}");
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation($"{station} Task cancelled.");
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, station);
            }
            await Task.Delay(_updateInterval, token);
        }
    }
}

internal static partial class Extensions
{
    public static IServiceCollection AddCurrentSongWriter(this IServiceCollection services) =>
        services.AddTransient<CurrentSongUpdater>().AddSongTracker();
}
