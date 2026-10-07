# Product photos, step 1: metadata

This patch follows `transfer-pricing.patch`. It prepares the database and product
responses for multiple photos. Uploading, downloading, removing photos, cloud
storage configuration, and desktop caching will be implemented in subsequent
steps. This patch does not yet accept or serve image files.

## Apply

Put `product-photo-metadata.patch` beside `MarketApp.sln`. Use the Visual Studio
terminal from that folder:

```powershell
git apply --check product-photo-metadata.patch
git apply product-photo-metadata.patch
dotnet build MarketApp.sln
```

If the check fails, stop and share the error instead of overwriting local edits.

Stop the API. The migration is already included, so run **only Update-Database**
in Visual Studio's Package Manager Console:

```powershell
Update-Database -Project MarketApp.Infrastructure -StartupProject MarketApp.Api -Context AppDbContext -Args '--environment Development'
```

Restart the API. No new package, storage account, or application setting is
required for this metadata step.

## What changed

`Product` now has a `Photos` collection. A new `ProductPhotos` table stores these
fields:

| Field | Purpose |
| --- | --- |
| `Id` | Stable photo identity. |
| `ProductId` | Product owning the photo. |
| `StorageKey` | Stable key for the future storage provider, such as `products/{productId}/{photoId}.jpg`. |
| `ContentType` | JPEG, PNG, or WebP MIME type. |
| `FileSizeBytes` | Size of the stored file. |
| `ContentHash` | Uppercase SHA-256 of the stored bytes; the desktop can later use it to check cached content. |
| `SortOrder` | Display order; the first ordered photo is the main photo. |
| `CreatedAtUtc` | Creation timestamp. |

The table contains metadata only, with no image-byte column. A storage key is
not a download URL or a local Windows path. The future upload endpoint will
generate the key and compute the hash and size from the actual stored file.
Database type constraints alone do not validate an image's contents; the upload
implementation must do that.

Product detail, list, create, and update responses include a `photos` array.
Entries are ordered by `SortOrder`, then `Id`. Updating a product's name, SKU,
category, or active status continues to preserve its photos. `SaveProductDto`
has not gained any photo write fields.

Each photo belongs to one product. Product deletion is restricted while photo
records exist, so a future deletion workflow must handle external image files
explicitly. Existing authorization remains in place.

## Test this step

1. Run the API and log in as the stakeholder.
2. Open `src/MarketApp.Api/ControllersTester/ProductPhotoMetadata.http`.
3. Set the API host, token, and an existing product ID.
4. Send the product detail request. Expect HTTP 200 and `"photos": []`.
5. Send the product list request. Each product should contain a `photos` array.
6. Your existing product create and update requests should also return the array.

Empty arrays are correct: no upload endpoint exists yet. Do not insert fake
photo paths or hashes to make the list nonempty.

## Verification and next step

The API builds with zero warnings or errors. The migration was scaffolded using
EF Core against the preceding transfer-pricing model. The migration must still
be applied and checked against your local PostgreSQL database.

Next, add photo upload and download through a storage interface. The production
storage implementation will hold the original image, and the Windows desktop
will later cache a local copy. This metadata step does not itself make photos
available offline or implement catalog synchronization.
