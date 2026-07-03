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
        Log.Information("Uruchamianie aplikacji Katalog klientów.");

        // Globalne przechwytywanie nieobsłużonych wyjątków → log do pliku + komunikat dla użytkownika.
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
                ?? throw new InvalidOperationException("Brak connection stringa 'CustomerCatalog' w appsettings.json.");

            var connectionFactory = new SqlConnectionFactory(connectionString);

            // Zapewnienie istnienia bazy/tabeli + dane testowe (Bogus) przy pierwszym uruchomieniu.
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
            Log.Information("Zamykanie aplikacji.");
            Log.CloseAndFlush();
        }
    }

    private static void HandleException(Exception? ex)
    {
        if (ex is null)
            return;

        Log.Error(ex, "Nieobsłużony błąd aplikacji.");
        MessageBox.Show(
            $"Wystąpił błąd:\n\n{ex.Message}\n\nSzczegóły zapisano w pliku logu.",
            "Błąd",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
