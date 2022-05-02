using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace RadCapCurrentSongTracker.Logging;

internal sealed class PlainConsoleFormatter : ConsoleFormatter
{
    public PlainConsoleFormatter() : base(nameof(PlainConsoleFormatter))
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        var exception = logEntry.Exception;
        if (message is null && exception is null)
        {
            return;
        }
        var exceptionText = exception?.ToString();
        if (exceptionText is not null)
        {
            exceptionText = $"{Environment.NewLine}{exceptionText}";
        }
        var timestamp = $"{DateTime.Now:HH:mm:ss}";
        var line = $"[{timestamp}] {message}{exceptionText}";
        textWriter.WriteLine(line);
    }
}

internal static partial class Extensions
{
    public static ILoggingBuilder AddPlainConsole(this ILoggingBuilder builder) =>
        builder.AddConsole(options => options.FormatterName = nameof(PlainConsoleFormatter))
           .AddConsoleFormatter<PlainConsoleFormatter, SimpleConsoleFormatterOptions>();

    public static ILoggingBuilder LogMyCodeOnly(this ILoggingBuilder builder, bool enabled = true)
    {
        if (enabled)
        {
            builder.AddFilter("*", LogLevel.None).AddFilter(nameof(RadCapCurrentSongTracker), LogLevel.Trace);
        }
        return builder;
    }
}
