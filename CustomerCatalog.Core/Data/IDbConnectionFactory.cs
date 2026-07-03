using System.Data;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Tworzy połączenia do bazy danych. Abstrakcja ułatwia testowanie
/// (można podstawić inny connection string, np. testową bazę).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Connection string wskazujący na docelową bazę katalogu.</summary>
    string ConnectionString { get; }

    /// <summary>Zwraca nowe, otwarte połączenie.</summary>
    IDbConnection CreateOpenConnection();
}
