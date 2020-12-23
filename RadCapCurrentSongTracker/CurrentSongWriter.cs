using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    public class CurrentSongWriter
    {
        private const int MinUpdateIntervalInSeconds = 1;
        private const int MaxUpdateIntervalInSeconds = 1;

        private const string Txt = ".txt";

        private static readonly HttpClient _httpClient = new();

        // Use for alternative way — parsing from playlist.
        private static readonly Regex _currentSongPatternHistory = new(
            @"<tr>(?:<td>(?'SongTime'[\d:]+?)<\/td>)<td>(?'SongName'[^\n\r\t\0]+?)(?:<td><b>Current Song<\/b><\/td>)<\/tr>",
            RegexOptions.Compiled);

        private static readonly Regex _currentSongPatternMountData = new(
            @"<tr><td>Current Song:<\/td><td .+?>(?'SongName'.+?)<\/td><\/tr>",
            RegexOptions.Compiled);

        private readonly TimeSpan _updateInterval;

        private readonly string _directory;

        private readonly Dictionary<string, string> _stations;

        public CurrentSongWriter(Options options)
        {
            options ??= Options.Default;
            _updateInterval = SafeUpdateInterval(options.UpdateIntervalInSeconds);
            _directory = options.Directory;
            _stations = options.Stations ?? Options.Default.Stations;
        }

        internal static async Task<CurrentSongWriter> NewAsync() => new(await Options.InitAsync());

        internal async Task RunAll()
        {
            EnsureDirectory();
            Console.WriteLine("Press any key to stop.");
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            var stopByUserTask = Task.Run(Console.ReadKey, token);
            var tasks = _stations.Keys.Select(x => RunSongUpdaterAsync(x, token)).Append(stopByUserTask);
            var stoppingTask = await Task.WhenAny(tasks);
            if (stoppingTask == stopByUserTask)
            {
                cts.Cancel();
                await ResetFilesAsync();
                Console.WriteLine("\nStopped.");
                return;
            }
            await stoppingTask;
        }

        private static TimeSpan SafeUpdateInterval(double? seconds)
        {
            seconds = Math.Max(
                MinUpdateIntervalInSeconds,
                Math.Min(seconds ?? MinUpdateIntervalInSeconds, MaxUpdateIntervalInSeconds));
            return TimeSpan.FromSeconds(seconds.Value);
        }

        private async Task ResetFilesAsync()
        {
            var tasks = Directory.EnumerateFiles(_directory).Select(x => File.WriteAllTextAsync(x, string.Empty));
            await Task.WhenAll(tasks);
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
            var fileName = station + Txt;
            var oldSongName = string.Empty;
            var urlTemplate = _stations[station];
            var path = Path.Combine(_directory, fileName);
            while (!token.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                var requestUrl = urlTemplate + now.ToUnixTimeMilliseconds();
                try
                {
                    var info = await _httpClient.GetStringAsync(requestUrl, token);
                    var match = _currentSongPatternMountData.Match(info);
                    var songName = match.Groups["SongName"].Value;
                    if (songName != oldSongName)
                    {
                        await File.WriteAllTextAsync(path, songName, token);
                        oldSongName = songName;
                        Console.WriteLine($"{ReportStation()} -> {songName}");
                    }
                    await Task.Delay(_updateInterval, token);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"{ReportStation()} Task cancelled.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(ReportStation());
                    Console.WriteLine(e.ToString());
                }

                string ReportStation() => $"{now.LocalDateTime.TimeOfDay:hh':'mm':'ss} {station}";
            }
        }
    }
}