using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    public partial class Options
    {
        internal const string FileName = "setting.json";

        public static Options Default { get; } = new()
        {
            Stations = new Dictionary<string, string>
            {
                ["DarkElectro"] = @"http://79.120.39.202:8000/status.xsl?mount=/darkelectro&_=",
                ["SymphoMetal"] = @"http://79.120.77.11:8000/status.xsl?mount=/symphometal&_=",
            },
            Directory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "ObsNowPlaying"),
            UpdateIntervalInSeconds = 5,
            FirstStart = true,
        };

        [JsonIgnore]
        public bool FirstStart { get; set; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };

        public static bool AreInvalid(Options? options, out string message)
        {
            if (options is null)
            {
                message = $"Unable to read program settings from {Path.GetFullPath(FileName)}";
                return true;
            }
            if (string.IsNullOrWhiteSpace(options.Directory))
            {
                message = InvalidPropertyMessage(nameof(Directory));
                return true;
            }
            if (options.Stations?.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value)) != false)
            {
                message = InvalidPropertyMessage(nameof(Stations));
                return true;
            }
            if (options.FirstStart)
            {
                message =
                    $"First launch detected. Verify settings at {Path.GetFullPath(FileName)} and restart the program.";
                return true;
            }
            message = string.Empty;
            return false;

            static string InvalidPropertyMessage(string propertyName)
                => $"Invalid {propertyName}, check {Path.GetFullPath(FileName)}";
        }

        public static async Task<Options?> InitAsync()
        {
            Options? options;
            if (!File.Exists(FileName))
            {
                await using var fs = new FileStream(
                    FileName,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous);

                await JsonSerializer.SerializeAsync(fs, Default, _jsonOptions);
                options = Default;
            }
            else
            {
                await using var fs = new FileStream(
                    FileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    4096,
                    FileOptions.Asynchronous);

                options = await JsonSerializer.DeserializeAsync<Options?>(fs, _jsonOptions);
            }

            return options;
        }
    }
}