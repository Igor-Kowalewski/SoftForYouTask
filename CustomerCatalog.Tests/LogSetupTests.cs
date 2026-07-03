using CustomerCatalog.Core.Logging;
using FluentAssertions;
using Serilog;
using Xunit;

namespace CustomerCatalog.Tests;

public class LogSetupTests
{
    [Fact]
    public void Configure_WithExplicitDirectory_CreatesDirectoryAndLogFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "CustomerCatalogTests_" + Guid.NewGuid());
        try
        {
            LogSetup.Configure(directory);
            Log.Information("test entry");
            Log.CloseAndFlush();

            Directory.Exists(directory).Should().BeTrue();
            Directory.GetFiles(directory, "log-*.txt").Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Configure_WithNullDirectory_UsesDefaultLogsFolder()
    {
        var defaultDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        try
        {
            LogSetup.Configure();
            Log.Information("test entry");
            Log.CloseAndFlush();

            Directory.Exists(defaultDirectory).Should().BeTrue();
            Directory.GetFiles(defaultDirectory, "log-*.txt").Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(defaultDirectory))
                Directory.Delete(defaultDirectory, recursive: true);
        }
    }
}
