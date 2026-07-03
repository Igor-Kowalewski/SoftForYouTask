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
│  ├─ Models/Customer.cs    # Encja główna
│  ├─ Models/{Nip,Email,Address}.cs  # Value objects – patrz niżej
│  ├─ Validation/           # CustomerInput (DTO z formularza), CustomerValidator
│  ├─ Services/             # CustomerQuery (filtrowanie i sortowanie – testowalne)
│  ├─ Data/                 # IDbConnectionFactory, CustomerRepository (Dapper), DatabaseInitializer, CustomerSchema
│  └─ Logging/LogSetup.cs   # Konfiguracja Serilog
├─ CustomerCatalog.App      # Aplikacja WinForms
│  ├─ Program.cs            # Start, globalne łapanie wyjątków, inicjalizacja bazy
│  ├─ Forms/MainForm.cs     # Lista, sortowanie, filtr, dodaj/edytuj/usuń, dwuklik → edycja
│  ├─ Forms/CustomerEditForm.cs
│  └─ appsettings.json      # Connection string
└─ CustomerCatalog.Tests    # Testy xUnit (integracyjne repozytorium + jednostkowe walidacji)
```

### Model domenowy: NIP, e-mail i adres jako value objects

`Nip`, `Email` i `Address` (`CustomerCatalog.Core/Models`) nie są zwykłymi `string`, tylko
niemutowalnymi typami (`sealed record`) z walidacją wbudowaną w konstrukcję
(`Parse`/`TryParse`, na wzór `Guid.Parse`/`TryParse`). Dzięki temu nie da się zbudować
`Customer` z niepoprawnym NIP-em, e-mailem czy kodem pocztowym — poprawność jest
gwarantowana przez typ, a nie sprawdzana osobno przy każdym użyciu.

`Address` jest osobnym value objectem (Street/PostalCode/City), a nie jednym polem
tekstowym ani osobną tabelą w bazie: nie ma własnej tożsamości i nigdy nie jest
współdzielony między klientami, więc modelowanie go jako oddzielnej encji z FK byłoby
niepotrzebną złożonością — kanoniczne podejście DDD (Evans) traktuje adres jako przykładowy
value object. W bazie przechowywany jest jako trzy kolumny (`Street`, `PostalCode`, `City`)
w tabeli `Customers`.

Dapper mapuje surowe wiersze SQL na płaski, prywatny rekord `CustomerRow`
(`CustomerRepository`), a dopiero repozytorium jawnie konwertuje go na `Customer` z
value objects — bez rejestrowania globalnych `SqlMapper.TypeHandler`.

## Decyzje architektoniczne (ADR)

### ADR-001: Aplikacja dostosowana wyłącznie do rynku polskiego

**Status:** Zaakceptowane

**Kontekst:**
Wytyczne definiują katalog klientów z polami nazwa/NIP/adres/telefon/e-mail. NIP to
specyficznie polski numer identyfikacji podatkowej, a projekt jest realizowany w
kontekście polskiej rekrutacji, bez wymogu obsługi innych rynków, walut czy formatów
danych.

**Decyzja:**
Aplikacja nie jest projektowana jako wielorynkowa ani wielojęzyczna. W szczególności:

- `Nip` (`CustomerCatalog.Core/Models/Nip.cs`) implementuje wyłącznie polski algorytm
  sumy kontrolnej NIP (wagi 6,5,7,2,3,4,5,6,7, modulo 11) — nie inne krajowe numery
  identyfikacji podatkowej (np. VAT ID innych krajów UE).
- `Address.PostalCode` (`CustomerCatalog.Core/Models/Address.cs`) waliduje wyłącznie
  polski format kodu pocztowego `NN-NNN` (`^\d{2}-\d{3}$`) — nie formaty innych krajów.
  `Address` nie ma pola `Country` (zakładana jest Polska).
- Cały interfejs użytkownika (etykiety, komunikaty walidacyjne, przyciski, okna dialogowe)
  jest zapisany na sztywno w języku polskim — brak mechanizmu i18n/zasobów językowych.
- Dane testowe generowane przez Bogus używają locale `"pl"` (`DatabaseInitializer`).
- `Customer.Phone` nie ma numeru kierunkowego kraju ani walidacji formatu — zakładany
  jest numer krajowy zapisany dowolnie (tylko limit długości).

**Konsekwencje:**
Rozszerzenie na inny kraj wymagałoby m.in.: uogólnienia `Nip` na politykę per-kraj
(np. `TaxId` z wymienną strategią walidacji), sparametryzowania walidacji kodu
pocztowego, dodania pola `Country` do `Address`, oraz wprowadzenia i18n dla UI
(zasoby językowe zamiast stringów na sztywno). W obecnym, jednorynkowym zakresie
rekrutacyjnym jest to świadome uproszczenie — nie wprowadzano spekulacyjnych
abstrakcji wielokrajowych, których nic obecnie nie wymaga.

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
- **jednostkowe** value objects `Nip`, `Email`, `Address` (parsowanie, walidacja, równość),
- **jednostkowe** `CustomerValidator` (agregacja błędów z wielu pól) oraz filtrowania/sortowania (`CustomerQuery`),
- weryfikację, że dane generowane przez Bogus są poprawne (nie rzucają wyjątkiem przy budowie value objects).

## Publikacja na GitHub

Repozytorium lokalne jest już zainicjowane. Aby wypchnąć je na GitHub:

```powershell
# 1. Utwórz puste repozytorium na https://github.com/new (bez README/.gitignore)
# 2. Podłącz zdalne repozytorium i wypchnij:
git remote add origin https://github.com/<uzytkownik>/<repo>.git
git branch -M main
git push -u origin main
```
