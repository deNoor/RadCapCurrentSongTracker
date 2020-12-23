using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    public partial class Options
    {
        private const string FileName = "setting.json";

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
                    "ObsNowPlaying"), // @"Z:\ObsNowPlaying", // AppDomain.CurrentDomain.BaseDirectory
            UpdateIntervalInSeconds = 5,
        };

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };

        public static async Task<Options> InitAsync()
        {
            Options options;
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

                options = await JsonSerializer.DeserializeAsync<Options>(fs, _jsonOptions);
            }

            return options;
        }
    }
}