using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    public partial class Options
    {
        internal const string FileName = "settings.json";

        public static Options Default { get; } = new()
        {
            Stations = new Dictionary<string, string>
            {
                ["DarkElectro"] = @"http://79.120.39.202:8000/status.xsl?mount=/darkelectro&_=",
                ["SymphoMetal"] = @"http://79.120.77.11:8000/status.xsl?mount=/symphometal&_=",
            },
            Directory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "ObsNowPlaying"),
            UpdateIntervalInSeconds = 5,
        };

        [JsonIgnore]
        public bool FirstRun { get; set; }

        internal static string FullPath { get; } = Path.Combine(AppContext.BaseDirectory, FileName);

        private static JsonSerializerOptions JsonOptions { get; } = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };

        public bool AreInvalid(out string message)
        {
            message = string.Empty;
            if (FirstRun)
            {
                message = $"First launch detected. Verify settings at {FullPath} and restart the program.";
                return true;
            }
            if (string.IsNullOrWhiteSpace(Directory))
            {
                message = CreateMessage($"{nameof(Directory)} is missing");
                return true;
            }
            if (Stations is null)
            {
                message = CreateMessage($"{nameof(Stations)} is missing");
                return true;
            }
            foreach (var (station, url) in Stations)
            {
                if (string.IsNullOrWhiteSpace(station))
                {
                    message = CreateMessage($"{nameof(Stations)} - {station} is not a name");
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var _))
                {
                    message = CreateMessage($"{nameof(Stations)} - {url} is not a valid url");
                    return true;
                }
            }

            return false;

            static string CreateMessage(string reason)
                => $"{reason}, check {FullPath} or delete it to reset to default.";
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(FullPath))
            {
                await using var fs = new FileStream(
                    FullPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous);
                await JsonSerializer.SerializeAsync(fs, Default, JsonOptions, cancellationToken);
                FirstRun = true;
            }
        }

        public override string ToString() => JsonSerializer.Serialize(this, JsonOptions);
    }
}