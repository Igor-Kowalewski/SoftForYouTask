using CustomerCatalog.App.Forms;
using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CustomerCatalog.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        LogSetup.Configure();
        Log.Information("Starting Customer Catalog application.");

        // Global handling of unhandled exceptions → log to file + message to the user.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => HandleException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleException(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("CustomerCatalog")
                ?? throw new InvalidOperationException("Missing 'CustomerCatalog' connection string in appsettings.json.");

            var connectionFactory = new SqlConnectionFactory(connectionString);

            // Ensure the database/table exist and add test data (Bogus) on first run.
            new DatabaseInitializer(connectionFactory).EnsureCreatedAndSeeded();

            var repository = new CustomerRepository(connectionFactory);
            Application.Run(new MainForm(repository));
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            Log.Information("Shutting down application.");
            Log.CloseAndFlush();
        }
    }

    private static void HandleException(Exception? ex)
    {
        if (ex is null)
            return;

        Log.Error(ex, "Unhandled application error.");
        MessageBox.Show(
            $"Wystąpił błąd:\n\n{ex.Message}\n\nSzczegóły zapisano w pliku logu.",
            "Błąd",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
