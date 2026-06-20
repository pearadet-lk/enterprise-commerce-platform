IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'IdentityDb')
    CREATE DATABASE IdentityDb;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CatalogDb')
    CREATE DATABASE CatalogDb;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'OrdersDb')
    CREATE DATABASE OrdersDb;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'UsersDb')
    CREATE DATABASE UsersDb;
GO
