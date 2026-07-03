# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

WinForms desktop customer catalog (CRUD) for the Polish market. Recruitment project
(see README.md, in Polish). UI text, validation messages, and seed data locale are all
Polish by deliberate design decision — see ADR-001 in README.md before adding
multi-country/i18n support.

## Commands

```powershell
# Run the app (auto-creates & seeds LocalDB database on first run)
dotnet run --project CustomerCatalog.App

# Run all tests (includes integration tests against a real LocalDB instance)
dotnet test

# Run a single test class / method
dotnet test --filter "FullyQualifiedName~CustomerValidatorTests"
dotnet test --filter "FullyQualifiedName~NipTests.Parse_ValidNip_Succeeds"

# Build
dotnet build
```

Tests require SQL Server LocalDB (`MSSQLLocalDB` instance) to be available —
`CustomerRepositoryTests`/`DatabaseInitializerTests` create and drop a real
`CustomerCatalog_Test` database via `DatabaseFixture`. Check availability with
`sqllocaldb info MSSQLLocalDB`.

## Architecture

Three projects, split so all business logic is UI-free and independently testable:

- **CustomerCatalog.Core** — domain model, validation, data access, logging config. No
  WinForms dependency.
- **CustomerCatalog.App** — WinForms UI only (`Forms/MainForm.cs`, `Forms/CustomerEditForm.cs`,
  `Program.cs`). Thin: it calls into Core and does not contain business rules.
- **CustomerCatalog.Tests** — xUnit + FluentAssertions, covering both unit tests
  (value objects, validator, `CustomerQuery`) and integration tests (repository against
  real LocalDB).

### Value objects for domain correctness

`Nip`, `Email`, and `Address` (`CustomerCatalog.Core/Models/`) are immutable `sealed record`
types with `Parse`/`TryParse` construction (mirroring `Guid.Parse`/`TryParse`) rather than
plain strings. A `Customer` cannot be constructed with an invalid NIP, e-mail, or postal
code — correctness is enforced by the type itself, not re-checked at each use site.
`Address` is a value object (Street/PostalCode/City), not a separate entity/table — it has
no independent identity and is never shared between customers.

### Dapper mapping boundary

Dapper only ever sees the flat, private `CustomerRow` record inside `CustomerRepository`
(`CustomerCatalog.Core/Data/CustomerRepository.cs`). The repository explicitly converts
`CustomerRow` ↔ `Customer` (with its Nip/Address/Email value objects) rather than
registering global `SqlMapper.TypeHandler`s. When changing the `Customers` schema, update
`CustomerSchema.cs`, `CustomerRow`, and the mapping methods (`ToCustomer`/`ToParameters`)
together.

### Query logic extracted for testability

Filtering, sorting, and pagination (`CustomerCatalog.Core/Services/CustomerQuery.cs`) are
static, UI-independent methods so they can be unit tested without WinForms. `MainForm` calls
these rather than implementing filter/sort logic itself.
`CustomerQuery.CreatedAtDisplayFormat` is shared between what's rendered in the grid and what
the filter matches against, so display and search never drift apart — reuse it if you touch
either side.

### Startup flow (`CustomerCatalog.App/Program.cs`)

1. `LogSetup.Configure()` — Serilog to a rolling daily file.
2. Global exception handlers registered (WinForms `ThreadException` +
   `AppDomain.UnhandledException`) before `ApplicationConfiguration.Initialize()`, logging
   and showing a MessageBox rather than crashing silently.
3. Connection string read from `appsettings.json` (`ConnectionStrings:CustomerCatalog`).
4. `DatabaseInitializer.EnsureCreatedAndSeeded()` creates the LocalDB database/table and
   seeds 10,000 Bogus-generated (`pl` locale) customers if empty, in one transaction.
5. `MainForm` constructed with a `CustomerRepository`.

### Tests

`DatabaseFixture` (`CustomerCatalog.Tests/DatabaseFixture.cs`) creates a dedicated
`CustomerCatalog_Test` LocalDB database per test class run and drops it on dispose — tests
that use it are true integration tests against real SQL, not mocks. `ClearCustomers()`
resets identity for a clean state between tests within a class.
