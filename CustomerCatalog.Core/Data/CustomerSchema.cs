namespace CustomerCatalog.Core.Data;

/// <summary>
/// Central definition of the Customers table schema, shared by the runtime
/// initializer and the test fixtures so the schema is defined in a single place.
/// </summary>
public static class CustomerSchema
{
    public const string TableName = "Customers";

    /// <summary>CREATE TABLE statement for dbo.Customers (without any existence guard).</summary>
    public const string CreateTableSql = """
        CREATE TABLE dbo.Customers
        (
            Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            Name       NVARCHAR(200)     NOT NULL,
            Nip        NVARCHAR(10)      NOT NULL CONSTRAINT UQ_Customers_Nip UNIQUE,
            Street     NVARCHAR(200)     NOT NULL,
            PostalCode NVARCHAR(10)      NOT NULL,
            City       NVARCHAR(100)     NOT NULL,
            Phone      NVARCHAR(30)      NULL,
            Email      NVARCHAR(200)     NOT NULL,
            CreatedAt  DATETIME2         NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
        );
        """;

    /// <summary>Creates the table only when it does not already exist.</summary>
    public const string CreateTableIfNotExistsSql = $"""
        IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
        BEGIN
            {CreateTableSql}
        END
        """;
}
