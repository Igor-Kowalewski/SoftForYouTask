using Serilog;

namespace CustomerCatalog.Core.Logging;

/// <summary>
/// Logging configuration (Serilog → file). Application errors are written to
/// <c>logs/log-YYYYMMDD.txt</c> (rolled daily).
/// </summary>
public static class LogSetup
{
    /// <summary>
    /// Configures the global Serilog logger. <paramref name="logDirectory"/> defaults
    /// to a "logs" subdirectory next to the executable.
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
