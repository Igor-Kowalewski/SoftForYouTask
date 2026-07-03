# Katalog klientów

Aplikacja desktopowa (WinForms) będąca katalogiem klientów z pełną obsługą CRUD:
przegląd, filtrowanie, sortowanie, dodawanie, edycja i usuwanie danych klienta
(nazwa, NIP, adres, nr telefonu, e-mail).

> Projekt realizowany wyłącznie w celach rekrutacyjnych.

## Stos technologiczny

| Obszar | Technologia |
|--------|-------------|
| Runtime | .NET 10 (WinForms, `net10.0-windows`) |
| Język | C# |
| Dostęp do danych | [Dapper](https://github.com/DapperLib/Dapper) |
| Baza danych | Microsoft SQL Server **LocalDB** (`MSSQLLocalDB`) |
| Dane testowe | [Bogus](https://github.com/bchavez/Bogus) |
| Logowanie | [Serilog](https://serilog.net/) (rolowany plik) |
| Testy | xUnit + FluentAssertions |

### Uwaga o DevExpress

Wytyczne dopuszczały DevExpress, jednak jest to biblioteka komercyjna wymagająca licencji
i prywatnego kanału NuGet. W tym projekcie użyto wbudowanego, darmowego `DataGridView`
(sortowanie po nagłówkach, filtrowanie przez pole wyszukiwania). Warstwa UI jest cienka –
ewentualna wymiana gridu na DevExpress `GridControl` sprowadza się do podmiany kontrolki
w `MainForm`, bez zmian w logice (`CustomerCatalog.Core`).

## Struktura rozwiązania

```
CustomerCatalog.slnx
├─ CustomerCatalog.Core     # Logika i dostęp do danych (testowalne, bez zależności od UI)
│  ├─ Models/Customer.cs
│  ├─ Validation/           # NipValidator, CustomerValidator
│  ├─ Data/                 # IDbConnectionFactory, CustomerRepository (Dapper), DatabaseInitializer
│  └─ Logging/LogSetup.cs   # Konfiguracja Serilog
├─ CustomerCatalog.App      # Aplikacja WinForms
│  ├─ Program.cs            # Start, globalne łapanie wyjątków, inicjalizacja bazy
│  ├─ Forms/MainForm.cs     # Lista, sortowanie, filtr, dodaj/edytuj/usuń, dwuklik → edycja
│  ├─ Forms/CustomerEditForm.cs
│  └─ appsettings.json      # Connection string
└─ CustomerCatalog.Tests    # Testy xUnit (integracyjne repozytorium + jednostkowe walidacji)
```

## Wymagania

- Windows 10 lub nowszy
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server **LocalDB** (instancja `MSSQLLocalDB`) – zwykle instalowana z Visual Studio
  lub pakietem *SQL Server Express LocalDB*

Sprawdzenie dostępności LocalDB:

```powershell
sqllocaldb info MSSQLLocalDB
```

## Uruchomienie

```powershell
dotnet run --project CustomerCatalog.App
```

Przy pierwszym uruchomieniu aplikacja automatycznie:
1. tworzy bazę `CustomerCatalog` w LocalDB (jeśli nie istnieje),
2. tworzy tabelę `Customers`,
3. wypełnia ją ~50 losowymi klientami wygenerowanymi przez Bogus.

Connection string można zmienić w `CustomerCatalog.App/appsettings.json`.

## Obsługa aplikacji

- **Przegląd** – lista klientów ładowana z bazy w oknie głównym.
- **Sortowanie** – kliknięcie nagłówka kolumny (ponowne kliknięcie odwraca kierunek).
- **Filtrowanie** – pole „Filtruj” zawęża listę po nazwie, NIP, e-mailu, telefonie lub adresie.
- **Dodawanie** – przycisk *Dodaj* otwiera formularz nowego klienta.
- **Edycja** – **dwuklik** w wiersz (lub przycisk *Edytuj*) otwiera widok szczegółowy;
  po zapisaniu następuje powrót do widoku głównego.
- **Usuwanie** – przycisk *Usuń* (z potwierdzeniem).

## Logowanie błędów

Błędy działania aplikacji są zapisywane do pliku
`CustomerCatalog.App/bin/.../logs/log-YYYYMMDD.txt` (Serilog, rolowanie dzienne).
Nieobsłużone wyjątki są przechwytywane globalnie i logowane, a użytkownik otrzymuje komunikat.

## Testy

```powershell
dotnet test
```

Testy obejmują:
- **integracyjne** repozytorium (Dapper) na osobnej bazie `CustomerCatalog_Test` w LocalDB
  (pełny cykl Insert → GetById → Update → GetAll → Delete; baza jest tworzona i usuwana automatycznie),
- **jednostkowe** walidacji NIP (suma kontrolna) oraz modelu klienta,
- weryfikację, że dane generowane przez Bogus są poprawne.

## Publikacja na GitHub

Repozytorium lokalne jest już zainicjowane. Aby wypchnąć je na GitHub:

```powershell
# 1. Utwórz puste repozytorium na https://github.com/new (bez README/.gitignore)
# 2. Podłącz zdalne repozytorium i wypchnij:
git remote add origin https://github.com/<uzytkownik>/<repo>.git
git branch -M main
git push -u origin main
```
