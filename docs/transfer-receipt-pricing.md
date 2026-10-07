# Transfer receipt pricing

This change is based on GitHub commit `21e6e50` and implements the agreed rule:
if a transfer carries baseline 100 and the branch baseline changes to 150 after
the transfer is created, receipt adds the stock and keeps 150. The transfer line
continues to show its original 100.

## Apply the change

Download `transfer-pricing.patch` into your local repository root (beside
`MarketApp.sln`). In the Visual Studio terminal, run:

```powershell
git apply --check transfer-pricing.patch
git apply transfer-pricing.patch
dotnet build MarketApp.sln
dotnet test tests/MarketApp.Application.Tests/MarketApp.Application.Tests.csproj
```

If the patch check reports a conflict, stop and retain your local edits; the
patch was prepared against the commit above. No remote branch was changed.

The migration is already included. **Do not run Add-Migration for this change.**
Stop the running API, then run this in Visual Studio's Package Manager Console:

```powershell
Update-Database -Project MarketApp.Infrastructure -StartupProject MarketApp.Api -Context AppDbContext -Args '--environment Development'
```

Restart the API using your existing connection string and JWT user secrets.
Open `src/MarketApp.Api/ControllersTester/TransferReceiptPricing.http`, fill in
the token and IDs, and follow the numbered requests. Keep credentials local.

## Behavior

| Situation | Result on receipt |
| --- | --- |
| Baseline has not changed since creation | Apply the transfer baseline to current branch pricing. |
| Baseline changed after creation | Keep the current baseline, even if someone later changed it back. |
| Only minimum selling price changed | Apply the transfer baseline; preserve the current minimum. |
| No branch price exists | Create baseline pricing; leave minimum selling price unset. |
| Minimum exists but baseline is unset | Set baseline; preserve minimum. |
| Transfer baseline equals current baseline | Receive stock without an extra price revision or history entry. |
| Transfer was already received | Return success without applying stock or pricing again. |
| Transfer was created before this migration | Receive stock normally; do not automatically change pricing. |
| Repeated product lines use one baseline | Sum their stock quantities; create at most one price history entry for the product. |
| Repeated product lines use different baselines | Reject creation with HTTP 400. |

All new transfers capture the destination product's baseline revision at creation.
`BaselineRevision` stores the overall revision of the latest actual baseline
change, so minimum-only changes leave it unchanged. It is zero until a baseline
has been configured. The snapshot is internal and cannot be supplied by clients.

The migration backfills existing price rows from their price history, falling
back to the current revision where no baseline history exists. Old transfers
retain a null snapshot: their original baseline revision cannot be reliably
reconstructed. If an old transfer arrives and the branch has no baseline yet,
the stakeholder must set it through the existing baseline endpoint.

Every actual receipt price change records `TransferReceipt`, the original and
new baseline, the unchanged minimum, the transfer line, and the authenticated
receiving user's ID. For repeated product lines, the first line identifies this
single product price change. Preserving a newer price creates no artificial
price-change event; the transfer itself retains its original line prices.

The manager's minimum is never calculated automatically. Existing seller DTOs
remain unchanged and do not expose the baseline or its internal revision.

If multiple transfers captured the same baseline revision, the first receipt
that changes the baseline makes the others stale. A later transfer created
after that update can change it again. This follows the agreed rule to preserve
any baseline change made after a transfer was created.

## Persistence and concurrency

Receipt prepares all balance and pricing changes before mutating tracked
entities. It saves transfer status, movements, balances, prices, and history
through one UnitOfWork save. Existing PostgreSQL `xmin` concurrency checks and
unique indexes remain in place. A concurrent conflicting write returns HTTP 409;
refresh and retry the receipt so it can evaluate the newest baseline revision.

Shipping and physical returns continue to affect stock only.

## Verification

The API build and 13 application tests passed in the development workspace.
The full solution also builds with zero warnings and errors. EF reports no
pending model changes, and the migration SQL was generated and inspected.
The tests cover the 100-to-150 rule, minimum-only changes, changing a baseline
and changing it back, missing/zero baselines, repeated receipt, repeated product
lines, conflicting prices, legacy transfers, unchanged prices, missing actor,
and validation failures before mutation.

These are application tests using repository fakes. They do not claim to verify
PostgreSQL transaction rollback or concurrent database requests. The migration
has not been applied to your database.

After the numbered HTTP scenario, also check:

1. Create another transfer with baseline 120, do not change the baseline, receive
   it, and verify baseline 120 plus one `TransferReceipt` history entry.
2. Create a transfer with a different baseline, change only the minimum, receive,
   and verify the transfer baseline and the manager's new minimum both remain.
3. Repeat either successful receipt and verify balances and price history do not
   grow again.

Product photos are the next feature after you verify this step. The full API
reference and user documentation remain planned for API completion.
