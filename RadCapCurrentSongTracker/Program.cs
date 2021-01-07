using System;
using System.Threading.Tasks;

namespace RadCapCurrentSongTracker
{
    internal class Program
    {
        private static async Task Main()
        {
            try
            {
                var options = await Options.InitAsync();
                if (Options.AreInvalid(options, out var message))
                {
                    Console.WriteLine(message);
                    Console.ReadKey();
                    return;
                }
                await new CurrentSongWriter(options!).RunAllAsync();
            }
            catch (Exception a)
            {
                Console.WriteLine(a);
                Console.WriteLine($"{Environment.NewLine}Unexpected error. Application will be closed.");
                Console.ReadKey();
            }
        }
    }
}