# MarketApp API — Batch 1 sales

This project contains the existing API and the completed **Batch 1 sales implementation**, based on GitHub commit `3e74404` (`product photo`). Returns, offline synchronization, profit reports, and manager profit shares remain in Batches 2 and 3.

Apply `marketapp-batch1-sales.patch` to your existing solution using the accompanying `Apply-Batch1.bat`. The script must be beside `MarketApp.sln`. It checks the patch before applying it and stops if your local files conflict. It does not migrate the database.

Start with [the Batch 1 guide](docs/batch-1-sales.md). It contains the upgrade instructions, endpoint reference, request example, and joint testing checklist. See [verification](docs/batch-1-verification.md) for completed checks and PostgreSQL checks still to run.

Requirements: .NET 10 SDK, PostgreSQL, and your existing database/JWT configuration. Open `MarketApp.sln` in an IDE that supports .NET 10, or use the terminal. The API project is under `src/MarketApp.Api`.

From the folder containing `MarketApp.sln`:

```powershell
dotnet restore MarketApp.sln
dotnet build MarketApp.sln
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
dotnet run --project src/MarketApp.Api --launch-profile https
```

The migration is already included. **Do not run Add-Migration for this batch.** Apply the update to a backup/test copy of your existing database first.

Keep your current JWT signing key and database connection in user secrets. The project keeps its existing `UserSecretsId`, so those settings continue to work on the same Windows account. This patch preserves your existing appsettings and HTTP files. Only the new Sales.http file contains placeholders. Keep using your working configuration.

```powershell
dotnet user-secrets set "ConnectionStrings:MarketDatabase" "Host=localhost;Port=5432;Database=YOUR_TEST_DATABASE;Username=postgres;Password=YOUR_PASSWORD" --project src/MarketApp.Api
```

Do not reset the JWT key or rerun identity initialization for an existing working installation. Use your existing users and branch assignments.

Run tests:

```powershell
dotnet test MarketApp.sln
```

The default API tests use SQLite. To exercise PostgreSQL transactions and token rotation, see the dedicated test database command in the verification guide.
