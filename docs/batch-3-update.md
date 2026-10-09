> Historical batch document. After applying the profit-accounting patch, follow [the current update](profit-accounting-update.md): percentage shares are retired and new damaged returns are rejected.

# Batch 3 — final API update

Apply `marketapp-batch3-final.patch` to your existing, tested Batch 2 project using **Visual Studio Terminal (PowerShell)**. Keep using `MarketApp_Test` for acceptance testing. Stop the API/debugger, save your files, and make a fresh project/database backup first.

Put the patch beside `MarketApp.sln`. Run each command separately; stop on an error:

```powershell
git apply --check --ignore-space-change .\marketapp-batch3-final.patch
git apply --ignore-space-change .\marketapp-batch3-final.patch
dotnet build MarketApp.sln
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api
dotnet test MarketApp.sln --no-build
```

The patch includes `AddManagerProfitShares`; **do not run Add-Migration**. If the patch check fails, share its output before proceeding. The reverse check below only verifies an already-applied patch; it does not undo it:

```powershell
git apply --reverse --check --ignore-space-change .\marketapp-batch3-final.patch
```

## Included changes

- Stakeholder-configured manager profit-share agreements with percentage, actor, reason, client change ID, and server effective timestamp. No assumed percentage or retroactive share assignment.
- Original manager/percentage snapshots for newly posted sales; offline submissions use the agreement effective at the original sale time. Exact Batch 1/2 sale replays remain compatible.
- Manager earnings added to branch profit reports. Stakeholder sees all managers in that branch; a branch manager sees only their own earnings. Sellers see neither shares nor profits.
- Paged staff, purchase invoice, and transfer lists; staff activation/deactivation and branch reassignment with session revocation.
- Staff creation now matches the existing four-character password minimum, without complexity requirements.
- New uploaded photos are normalized/compressed to at most 150 KiB (153,600 bytes). Existing stored photos are not rewritten.
- SMTP support, production startup checks, persistent Data Protection configuration, explicit web origins, trusted proxy configuration, HTTPS/HSTS, authentication rate limits, health routes, and a stakeholder-protected OpenAPI endpoint.

## Acceptance sequence

1. Run existing `Sales.http` and `Batch2.http` checks again.
2. Open `ControllersTester/Batch3.http`. Fill tokens, IDs, your chosen percentage, and UTC report bounds.
3. Create an agreement as stakeholder. Repeat exactly: same agreement, no duplicate. Seller/manager writes must return 403.
4. Create a **new sale after the agreement**. Its manager earnings should be `(actual sale revenue − baseline snapshots) × percentage / 100`.
5. Change the agreement with a new client change ID. The earlier sale's earnings must stay unchanged.
6. Return one item and verify refund/stock/gross profit/manager share together. See the negative-return examples in the usage guide.
7. Deactivate a test seller account; their existing access token must receive 401. Reactivate/reassign and log in again. This endpoint cannot modify stakeholder access.
8. Upload a new complex photo; download its content and verify it is at most 153,600 bytes.
9. As stakeholder, fetch `/openapi/v1.json`; as seller, expect 403. `/health/live` is anonymous; `/health/ready` requires stakeholder access.
10. Read `docs/api/usage-guide.md`, `docs/api/reference.md`, and `docs/deployment/production.md` before deployment.

## Manager-share interpretation

One agreement is effective per branch at a time. A later record replaces the recipient/percentage for future sale timestamps. Use a new 0% agreement to stop accrual; past records cannot be edited, deleted, or backdated. Disabling an account does not cancel its business agreement: stop/replace the share before removing a manager's access. This API reports signed gross-profit participation, including negative adjustments from returns, and does not issue payroll payments or settle wages. Agree any payroll settlement/rounding policy separately.

Existing sales keep null manager and 0% snapshots. They remain in gross-profit reports but do not receive a new manager share retroactively. Returns of those sales likewise have no manager-share effect. An offline sale first posted after this update uses any agreement that already existed at its sale time.

Batch 3 completes the planned server API implementation. Windows desktop UI/local offline storage, manager web UI, hosting setup, and live SMTP/S3/PostgreSQL acceptance checks remain separate from implementing the API.
