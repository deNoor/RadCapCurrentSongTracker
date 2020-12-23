using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    internal class Program
    {
        private static async Task Main()
        {
            await (await CurrentSongWriter.NewAsync()).RunAll();
        }
    }
}