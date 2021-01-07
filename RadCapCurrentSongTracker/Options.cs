using System.Collections.Generic;

namespace RadCapCurrentSongTracker
{
    public partial class Options
    {
        public Dictionary<string, string>? Stations { get; set; }
        public double UpdateIntervalInSeconds { get; set; }
        public string? Directory { get; set; }
    }
}