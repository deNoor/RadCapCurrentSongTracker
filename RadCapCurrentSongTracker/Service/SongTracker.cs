using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RadCapCurrentSongTracker.Service;

public class SongTracker
{
#pragma warning disable IDE0052 // Remove unread private members
    // Use for alternative way — parsing from playlist.
    private static readonly Regex _currentSongPatternHistory = new(
        @"<tr>(?:<td>(?'SongTime'[\d:]+?)<\/td>)<td>(?'SongName'[^\n\r\t\0]+?)(?:<td><b>Current Song<\/b><\/td>)<\/tr>",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));
#pragma warning restore IDE0052 // Remove unread private members

    private static readonly Regex _currentSongPatternMountData = new(
        @"<tr><td>Current Song:<\/td><td .+?>(?'SongName'.+?)<\/td><\/tr>",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Settings _settings;

    public SongTracker(IHttpClientFactory httpClientFactory, IOptions<Settings> options)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
    }

    public async Task<string> NowPlaying(string station, CancellationToken token)
    {
        var songName = string.Empty;
        try
        {
            var urlTemplate = _settings.Stations![station];
            var now = DateTimeOffset.UtcNow;
            var requestUrl = urlTemplate + now.ToUnixTimeMilliseconds();
            using var httpClient = _httpClientFactory.CreateClient(nameof(SongTracker));
            var info = await httpClient.GetStringAsync(requestUrl, token);
            var match = _currentSongPatternMountData.Match(info);
            songName = WebUtility.HtmlDecode(match.Groups["SongName"].Value);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return songName;
    }
}

internal static partial class Extensions
{
    public static IServiceCollection AddSongTracker(this IServiceCollection services) =>
        services.AddTransient<SongTracker>()
           .AddHttpClient(nameof(SongTracker), client => client.DefaultRequestVersion = HttpVersion.Version20)
           .Services;
}
