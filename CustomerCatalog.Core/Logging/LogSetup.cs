using Serilog;

namespace CustomerCatalog.Core.Logging;

/// <summary>
/// Konfiguracja logowania (Serilog → plik). Błędy działania aplikacji trafiają do
/// <c>logs/log-YYYYMMDD.txt</c> (rolowane dziennie).
/// </summary>
public static class LogSetup
{
    /// <summary>
    /// Konfiguruje globalny logger Serilog. <paramref name="logDirectory"/> domyślnie
    /// wskazuje podkatalog "logs" obok pliku wykonywalnego.
    /// </summary>
    public static void Configure(string? logDirectory = null)
    {
        logDirectory ??= Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
