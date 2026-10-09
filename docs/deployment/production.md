# Deployment and operations

This is an operator checklist for the implemented API, not confirmation that a public deployment already exists. Finish the local acceptance tests and PostgreSQL-specific tests before using real branch data. The app does not automatically migrate a database on startup.

## Runtime and configuration

Use .NET 10, PostgreSQL, and the same reviewed package versions as the solution. Publish from the existing project:

```powershell
dotnet publish src/MarketApp.Api -c Release -o .\publish
```

`production.example.json` is a review template and is not loaded automatically. Replace its placeholders and configure the target host using environment variables/its secret store or an uncommitted `appsettings.Production.json`. Do not put actual passwords, tokens, or signing keys in Git. Preserve the working Development settings for your local tests.

ASP.NET configuration uses double underscores in environment names, for example `ConnectionStrings__MarketDatabase`, `Jwt__SigningKeyBase64`, `Email__Password`, and `Cors__AllowedOrigins__0`. Set `ASPNETCORE_ENVIRONMENT=Production` on the deployed host.

| Setting | Purpose |
|---|---|
| `ConnectionStrings:MarketDatabase` | Target PostgreSQL connection string. Use a restricted application account and protect transport as required by your database host. Apply migrations using an account permitted to change schema. |
| `Jwt:Issuer`, `Jwt:Audience` | Matching token issuer/audience values for this deployment. |
| `Jwt:SigningKeyBase64` | At least 32 random bytes encoded as Base64, kept stable across API instances. Rotating it invalidates existing access tokens; plan a fresh-login window. |
| `Jwt:AccessTokenMinutes` | 1–60 minutes; default 30. Refresh sessions last seven days. |
| `AllowedHosts` | Explicit API host names separated by semicolons; no `*` outside Development. |
| `Email:Provider`, `Host`, `Port`, `From` | `Smtp`, provider hostname, usually 587, verified sender. Non-development startup validates these values. |
| `Email:UserName`, `Password` | SMTP credentials where required. Adapter always requires STARTTLS. Providers requiring OAuth or implicit TLS need a separate adapter; do not assume port 465 works. |
| `DataProtection:KeysPath` | Absolute persistent private directory for email/reset token protection keys. Required outside Development. Back it up; share it between instances of the same app. |
| `DataProtection:CertificatePath`, `CertificatePassword` | Optional PFX certificate for encrypting persisted protection keys. If used, retain its private key for recovery. Without it, use strict filesystem ACLs and encrypted storage. |
| `PhotoStorage:Provider` | `Local` or `S3`. |
| `PhotoStorage:LocalRoot` | Absolute persistent private photo directory, outside the public web root. Required for Local outside Development. Windows example: `C:\MarketAppData\photos`; Linux example: `/var/lib/marketapp/photos`. |
| `PhotoStorage:BucketName`, `Region` | Private S3 bucket and region. Prefer host identity/default AWS credentials; explicit AccessKeyId and SecretAccessKey must be supplied together through secrets if needed. |
| `Cors:AllowedOrigins` | Exact manager-web origins, e.g. `https://manager.example.com`; no paths/trailing slash/wildcards. Empty denies browser cross-origin access. Desktop clients do not use CORS. |
| `ReverseProxy:KnownProxies` | Actual trusted proxy IPs only. Forwarded headers from other hosts are not trusted. Default loopback trust remains. |

Generate a signing key locally with a cryptographic generator; do not reuse keys from examples. Never print a production key into a shared transcript. This PowerShell command displays a new key for copying into your host secret configuration:

```powershell
$keyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
[Convert]::ToBase64String($keyBytes)
$rng.Dispose()
```

## Host setup and release

1. Provision the database, persistent photo/key storage, service account permissions, SMTP sender, and TLS certificate.
2. Back up the target database and existing photos/keys. Stop sale writes for the migration window.
3. Configure target secrets; check that the connection string identifies the intended database, not your local test database.
4. Run `dotnet ef database update --project src/MarketApp.Infrastructure --startup-project src/MarketApp.Api` from the source release with target configuration, or have the database operator apply the reviewed generated SQL for the exact migration range. Do not apply both blindly.
5. Deploy the published directory. Start it under a service manager/IIS with Production environment. Use `dotnet MarketApp.Api.dll` from the publish directory if hosting as a service.
6. Expose HTTPS through IIS/a reverse proxy or configured Kestrel HTTPS endpoints. Restrict any internal HTTP listener to the proxy/private interface. Forward the original HTTPS scheme and client IP through a configured trusted proxy. Do not trust arbitrary forwarded headers.
7. Check anonymous `/health/live`, stakeholder `/health/ready`, login, a branch catalog request, SMTP delivery, a small photo upload/download, and a reversible test sale/return on approved test data.
8. Remove bootstrap secrets after one-time account initialization and allow branch activity only after acceptance.

The initializer remains Development-only and will not overwrite an existing stakeholder. For a brand-new production database, run the initialization once from an isolated administrative environment pointing to that database, with Development mode and the required BootstrapStakeholder secrets; do not expose that temporary setup process publicly. Then remove BootstrapStakeholder secrets and start the deployed API in Production. Existing databases already have their roles/stakeholder and do not need this step again.

Production adds HSTS and HTTPS redirection. CORS uses bearer Authorization headers, not cookies or AllowCredentials. Keep refresh tokens protected in the future clients (Windows OS-protected local storage; deliberate browser token storage design). Password complexity remains relaxed as requested; failed logins are still subject to account lockout and IP rate limits.

## Mail, sessions, and maintenance

In Development, mail is written under the current OS user's LocalApplicationData/MarketApp/DevelopmentMail unless `Email:Provider=Smtp` is explicitly selected. In Production, requests use real SMTP. The reset endpoint intentionally returns a generic response even when delivery fails; monitor server logs for delivery failures, without logging email bodies or tokens. Mail currently contains user ID and token; the desktop/web UI must provide the verification/reset forms.

The consistent Data Protection application name introduced here can invalidate reset/verification tokens generated before the update; request a new email if an old token fails. Password reset updates the security stamp, invalidating old authenticated sessions. Disabling/reassigning staff revokes their sessions; reactivation requires a fresh login.

Authentication routes allow 30 requests per minute per client IP; password recovery/verification requests allow 10 per 10 minutes per IP. Rate counters are in-memory per instance. Multi-instance deployments need suitable gateway/distributed limits and the same persistent Data Protection keys/JWT configuration. These limits do not replace account lockout.

`/health/live` only proves the process can respond. `/health/ready` checks database connectivity, not schema currency, mail delivery, S3 permissions, or disk capacity. It is stakeholder-protected; use a protected monitoring probe with appropriate credentials or monitor connectivity from the deployment system. OpenAPI is likewise stakeholder-protected.

## Backup and recovery

Back up PostgreSQL, the local photo directory or private S3 objects, and the Data Protection key directory/certificate. Test restoring them together to a separate environment. Keep backups access-controlled. No automatic backup job is created by this patch.

Monitor failed logins, mail delivery failures, 409 conflicts, 500 responses, storage capacity, photo cleanup failures, and database availability. Store logs with access controls and retention; never enable request-body logging for authentication endpoints. Photo deletion is staged so the cleanup worker can retry storage failures. Retain an older release and a tested backup; rolling a migration down after new records exist can remove business data.

## External checks not performed by automated SQLite tests

Real PostgreSQL migration/concurrency/session behavior, SMTP delivery, private S3 access, real reverse-proxy/TLS configuration, backup restoration, and deployment load limits must be checked in your environment. The future Windows app still needs its local offline queue/cache and the manager web app still needs its user interface.
