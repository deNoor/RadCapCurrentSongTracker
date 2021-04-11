using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RadCapCurrentSongTracker
{
    public class CurrentSongWriter
    {
        private const int MinUpdateIntervalInSeconds = 1;
        private const int MaxUpdateIntervalInSeconds = 60;
        private const string Txt = ".txt";

#pragma warning disable IDE0052 // Remove unread private members
        // Use for alternative way — parsing from playlist.
        private static readonly Regex _currentSongPatternHistory = new(
            @"<tr>(?:<td>(?'SongTime'[\d:]+?)<\/td>)<td>(?'SongName'[^\n\r\t\0]+?)(?:<td><b>Current Song<\/b><\/td>)<\/tr>"
           ,
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(1));
#pragma warning restore IDE0052 // Remove unread private members

        private static readonly Regex _currentSongPatternMountData = new(
            @"<tr><td>Current Song:<\/td><td .+?>(?'SongName'.+?)<\/td><\/tr>",
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(1));

        private readonly HttpClient _httpClient;
        private readonly TimeSpan _updateInterval;
        private readonly string _directory;
        private readonly Dictionary<string, string> _stations;

        public CurrentSongWriter(IOptions<Options> options, HttpClient httpClient)
        {
            _updateInterval = ToSafeUpdateInterval(options.Value.UpdateIntervalInSeconds);
            _directory = options.Value.Directory!;
            _stations = options.Value.Stations!;
            _httpClient = httpClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            EnsureDirectory();
            await RunAllAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => ResetFilesAsync(cancellationToken);

        private static TimeSpan ToSafeUpdateInterval(double? seconds)
        {
            seconds = Math.Max(
                MinUpdateIntervalInSeconds,
                Math.Min(seconds ?? MinUpdateIntervalInSeconds, MaxUpdateIntervalInSeconds));
            return TimeSpan.FromSeconds(seconds.Value);
        }

        private async Task RunAllAsync(CancellationToken cancellationToken)
        {
            var tasks = _stations.Select(x => RunSongUpdaterAsync(x, cancellationToken));
            await await Task.WhenAny(tasks);
        }

        private async Task ResetFilesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(_directory))
            {
                return;
            }
            var tasks = Directory.EnumerateFiles(_directory)
               .Select(x => File.WriteAllTextAsync(x, string.Empty, cancellationToken));
            await Task.WhenAll(tasks);
            Console.WriteLine("Song names are cleared.");
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
        }

        private async Task RunSongUpdaterAsync(KeyValuePair<string, string> data, CancellationToken token)
        {
            var (station, urlTemplate) = data;
            var oldSongName = string.Empty;
            var path = Path.ChangeExtension(Path.Combine(_directory, station), Txt);
            while (!token.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                var requestUrl = urlTemplate + now.ToUnixTimeMilliseconds();
                try
                {
                    var info = await _httpClient.GetStringAsync(requestUrl, token);
                    var match = _currentSongPatternMountData.Match(info);
                    var songName = WebUtility.HtmlDecode(match.Groups["SongName"].Value);
                    if (songName != oldSongName)
                    {
                        await File.WriteAllTextAsync(path, songName, token);
                        oldSongName = songName;
                        Console.WriteLine($"{ReportStation()} -> {songName}");
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"{ReportStation()} Task cancelled.");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(ReportStation());
                    Console.WriteLine(e.ToString());
                }
                await Task.Delay(_updateInterval, token);

                string ReportStation() => $"{now.LocalDateTime:HH':'mm':'ss} {station}";
            }
        }
    }

    public static class Extensions
    {
        public static IServiceCollection AddCurrentSongWriter(this IServiceCollection services)
        {
            services.AddTransient<CurrentSongWriter>();
            return services;
        }
    }
}