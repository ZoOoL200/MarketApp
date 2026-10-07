# Product photo upload and storage

This completes the photo metadata step. Product images live in local file storage or a private Amazon S3 bucket. PostgreSQL stores their identifiers, storage keys, content hashes and display order. The API normalizes uploads to WebP and serves them through authenticated endpoints.

This patch is based on the previously applied transfer pricing and product photo metadata changes. It does not include those earlier patches again.

## Apply and run

From the repository root containing MarketApp.sln, save your current work and put product-photos-complete.patch in that folder. Run in Terminal or PowerShell:

```powershell
git apply --check product-photos-complete.patch
git apply product-photos-complete.patch
dotnet restore MarketApp.sln
dotnet build MarketApp.sln
dotnet test MarketApp.sln
```

If the check reports a conflict, do not force the patch; compare the named files with your current changes. This patch expects the ProductPhoto entity and AddProductPhotoMetadata migration to exist already.

The new CompleteProductPhotoStorage migration is included. Do not run Add-Migration again. In Visual Studio Package Manager Console, run:

```powershell
Update-Database -Project MarketApp.Infrastructure -StartupProject MarketApp.Api
```

Or from a terminal at the repository root, use:

```powershell
dotnet ef database update `
  --project src/MarketApp.Infrastructure `
  --startup-project src/MarketApp.Api
dotnet run --project src/MarketApp.Api -- --environment Development
```

Use your existing PostgreSQL connection string and JWT signing key. Run the database update before starting the API. Open MarketApp.sln explicitly; the pre-existing MarketApp.slnx is empty.

## Storage configuration

Local storage is the default. On Windows its default location is `%LOCALAPPDATA%\MarketApp\ProductPhotos`, relative to the account running the API. For a predictable persistent location, merge this section into appsettings.Development.json:

```json
{
  "PhotoStorage": {
    "Provider": "Local",
    "LocalRoot": "C:\\MarketAppData\\ProductPhotos"
  }
}
```

The API account must have read/write/delete access. Keep this folder outside wwwroot and back it up with the database. A container needs a persistent mounted folder; multiple API instances need shared storage or S3.

For a private Amazon S3 bucket, set:

```json
{
  "PhotoStorage": {
    "Provider": "S3",
    "BucketName": "YOUR_PRIVATE_BUCKET",
    "Region": "YOUR_AWS_REGION"
  }
}
```

The code uses the AWS SDK credential chain when explicit keys are absent. Give the API identity GetObject, PutObject and DeleteObject permissions for the bucket's products/* prefix. Missing-object detection may also require scoped ListBucket permission; S3 can otherwise return 403 for a missing key. Keep public access blocked. For development only, PhotoStorage:AccessKeyId and PhotoStorage:SecretAccessKey can be supplied together through User Secrets. Never commit credentials.

The bucket and its IAM permissions must be provisioned separately; this patch does not create them. Changing the provider does not copy existing images. Copy existing objects with the same keys before switching, or use separate development and production databases. With S3 versioning, deletion may leave prior object versions, subject to the bucket lifecycle policy.

## API contract

Base route: `/api/products/{productId}/photos`.

| Method | Suffix | Role | Result |
| --- | --- | --- | --- |
| GET | none | Stakeholder, BranchManager, Seller | Ordered ready-photo array, or 404 for missing product |
| POST | none | Stakeholder | Multipart File and optional SortOrder; 201 with photo metadata |
| GET | /{photoId}/content | All three roles | WebP bytes; 304 for matching conditional request; 404 if unavailable |
| PUT | /{photoId}/order | Stakeholder | JSON { "sortOrder": 0 }; 204 or 404 |
| DELETE | /{photoId} | Stakeholder | 204; repeating is safe |

Every route requires the existing bearer JWT and active session. Photos are shared product catalog data. Existing product-management endpoint permissions are unchanged. An unauthenticated request returns 401; a seller or manager attempting a photo write returns 403.

Uploads accept JPEG, PNG or WebP content, between 1 byte and 5 MiB, up to 20 million decoded pixels. The multipart request limit is 6 MiB. Invalid image data is rejected even when its filename says .jpg. Original filenames never become storage paths. The API applies orientation, resizes to at most 1600 pixels on the longest edge without enlarging small images, retains only the first frame, strips source metadata, and writes WebP quality 82. This is a display image, not an archival original. Inactive products reject new uploads with 409; missing products return 404.

Metadata includes id, storageKey, contentType, fileSizeBytes, contentHash, sortOrder, createdAtUtc and downloadPath. contentHash is SHA-256 of the normalized bytes, not the original upload. Lower sortOrder appears first; ties use photo id. There is no separate primary-photo flag.

Use downloadPath through the API with an Authorization header. The storage key is not a public URL. For a web frontend, fetch the image with the bearer header and display a blob URL; a plain img src does not automatically attach a bearer token. Do not put access tokens in image URLs.

## Manual test in PowerShell

Start the API, log in as a stakeholder using the existing authentication flow, and choose an existing active product. Adjust the URL to match the address printed by your API.

```powershell
$api = "https://localhost:7126"
$token = "PASTE_ACCESS_TOKEN"
$productId = "PASTE_PRODUCT_GUID"
$photoFile = "C:\Users\YourName\Pictures\product.jpg"
$photoUrl = "$api/api/products/$productId/photos"
curl.exe -i -X POST $photoUrl `
  -H "Authorization: Bearer $token" `
  -F "File=@$photoFile" -F "SortOrder=0"
```

Expect 201 and copy id from the response. Use a current token; refresh or log in again if it expires.

```powershell
$photoId = "PASTE_PHOTO_GUID"
curl.exe -i $photoUrl -H "Authorization: Bearer $token"
curl.exe "$photoUrl/$photoId/content" `
  -H "Authorization: Bearer $token" --output downloaded.webp
```

Open downloaded.webp. Use ControllersTester/ProductPhotos.http to change order or delete. The delete should immediately hide the photo and make its content route return 404. Physical cleanup runs every minute and waits at least five minutes after deletion. Check existing product GET responses too: their photos collection now contains only ready, non-deleted photos.

Repeat with a seller or manager token: list/download should work, and upload/order/delete should return 403. Try a text file renamed .jpg (400), an image over 5 MiB (400 within the multipart request limit, or 413 from the host for a larger request), a missing product (404), and an inactive product (409). Host/proxy body limits can reject a request before the controller.

## Files and implementation

- Domain/Entity/Main/ProductPhoto.cs: IsReady and DeletedAtUtc track publication and deletion.
- Application/Interfaces/Services/IProductPhotoService.cs and IProductPhotoStorage.cs: separate workflow and storage contracts.
- Application/DTOs/Products: shared photo metadata mapping, upload result and download content contracts.
- Application/Common/ProductPhotoLimits.cs: request, image size and pixel limits.
- Application/Services/ProductService.cs: exposes only ready, non-deleted photos.
- Infrastructure/Photos/ProductPhotoProcessor.cs: image validation, orientation, resizing and WebP encoding with SkiaSharp.
- Infrastructure/Storage: Local and S3 adapters, options, generated-key validation and bounded reads.
- Infrastructure/Services/ProductPhotoService.cs: upload publication, metadata reads, integrity checking, ordering and deletion.
- Infrastructure/Services/ProductPhotoCleanupWorker.cs: retryable background cleanup.
- Infrastructure/Extension/ProductPhotoRegistration.cs: provider selection and dependency injection; called by AddMarketInfrastructure.
- Infrastructure/Persistence/Configurations/ProductPhotoConfiguration.cs and Migrations: two columns and cleanup index, plus updated model snapshot.
- Api/Models and Controllers/ProductPhotosController.cs: request validation, role permissions and HTTP responses.
- Api/ControllersTester/ProductPhotos.http: manual requests; existing metadata tester updated.
- tests/MarketApp.Photos.Tests and MarketApp.sln: photo integration test project and solution registration.

Project prefixes above mean src/MarketApp.Domain, src/MarketApp.Application, src/MarketApp.Infrastructure and src/MarketApp.Api respectively. The migration is 20261004094238_CompleteProductPhotoStorage. Infrastructure adds SkiaSharp 4.153.1, its Linux native assets package, and AWSSDK.S3 4.0.104.1. Final code uses SkiaSharp, not the earlier ImageSharp draft.

## Failure handling

Upload first saves a pending database row, then stores normalized bytes, then conditionally marks the row ready. A storage failure leaves a recoverable record but no visible photo. A delete arriving during upload prevents publication. Download compares byte length and SHA-256 against metadata; corrupt content produces a safe server error and a diagnostic log.

Deletion marks DeletedAtUtc immediately. The worker processes up to 25 eligible records per minute after a five-minute grace period. It deletes the file/object before deleting its metadata. A failure leaves the record for retry. Pending uploads older than one hour are marked for the same cleanup. The worker requires the API process to be running, so physical cleanup may take longer under backlog or outages. Do not automatically retry an ambiguous successful upload without first refreshing the photo list, because a second upload can create a duplicate.

The migration defaults IsReady to false. Existing metadata-only rows are therefore hidden and eventually cleaned if still pending. If you manually created real photo records outside the earlier metadata step, reconcile their files and state before deploying this migration.

## Verification and remaining deployment checks

Verified on 4 October 2026 with .NET SDK 10.0.401:

- Full solution build: 0 warnings, 0 errors.
- Existing transfer pricing tests: 13 passed.
- New photo workflow tests: 14 passed, covering input formats, resizing, JPEG rotation, conditional GET, roles, invalid files, inactive/missing products, ordering, deletion retries, interrupted uploads, traversal/overwrite rejection and corrupt content.
- EF model check: no pending model changes; PostgreSQL migration SQL generated successfully.

Photo tests run real controllers, photo services, SkiaSharp and local file storage through ASP.NET TestServer, with a SQLite database and test authentication. They do not validate production PostgreSQL constraints, real JWT/session integration, a live S3 bucket, or Windows native-library loading. Run the manual checks against your configured API after applying the migration. No database update or cloud deployment was performed here.

## Offline client and next work

The future desktop application can cache downloaded WebP files locally, keyed by photo id and contentHash, and reconcile its cache with the current photo list when connected. It can show cached images without internet; an uncached image needs a placeholder until the next download. This API step does not implement that desktop cache. There is one authoritative image in server storage, with a local cache on each device when the desktop app is built.

This autonomous implementation ends with the photo feature. Subsequent API work returns to showing you code and steps for you to apply. Full API and usage documentation remains planned after the complete API project is finished.
