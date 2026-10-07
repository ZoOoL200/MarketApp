using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using MarketApp.Api.Controllers;
using MarketApp.Api.ExceptionHandling;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.Interfaces.Services;
using MarketApp.Domain.Entity.Main;
using MarketApp.Infrastructure.Persistence;
using MarketApp.Infrastructure.Services;
using MarketApp.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SkiaSharp;

namespace MarketApp.Photos.Tests;

// Real controller, service, image processor and local file storage. SQLite is a
// test-only relational substitute; production PostgreSQL and JWT need smoke tests.
public sealed class PhotoWorkflowTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _root = Path.Combine(Path.GetTempPath(), "market-photo-tests-" + Guid.NewGuid());
    private readonly IHost _host;
    private readonly TestServer _server;
    private readonly HttpClient _client;
    private readonly FaultStorage _storage;
    private readonly Guid _productId = Guid.NewGuid();
    private string Route => $"/api/products/{_productId}/photos";

    public PhotoWorkflowTests()
    {
        _connection.Open();
        _storage = new(new LocalProductPhotoStorage(new() { LocalRoot = _root }));
        _host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(ProductPhotosController).Assembly);
            services.AddProblemDetails();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
            services.AddAuthorization(options => options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser()));
            services.AddScoped<AppDbContext>(_ => NewDb());
            services.AddSingleton<IProductPhotoStorage>(_storage);
            services.AddScoped<ProductPhotoService>();
            services.AddScoped<IProductPhotoService>(p => p.GetRequiredService<ProductPhotoService>());
        }).Configure(app =>
        {
            app.UseExceptionHandler();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        })).Start();
        _server = _host.GetTestServer();
        using var db = NewDb();
        db.Database.EnsureCreated();
        var category = new Category { Name = "Photos" };
        db.Products.Add(new() { Id = _productId, Category = category, Name = "Test", Sku = "PHOTO-1" });
        db.SaveChanges();
        _client = _server.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Stakeholder");
    }

    private PhotoDb NewDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
    private async Task<HttpResponseMessage> Upload(byte[] bytes, int order = 0)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new ByteArrayContent(bytes), "File", "untrusted-name.jpg");
        body.Add(new StringContent(order.ToString()), "SortOrder");
        return await _client.PostAsync(Route, body);
    }
    private async Task<ProductPhotoDto> UploadOk(byte[]? bytes = null, int order = 0)
    {
        using var response = await Upload(bytes ?? ImageBytes(), order);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductPhotoDto>())!;
    }
    private async Task<List<ProductPhotoDto>> List() => (await _client.GetFromJsonAsync<List<ProductPhotoDto>>(Route))!;
    private async Task Cleanup()
    {
        using var scope = _server.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ProductPhotoService>().CleanupAsync(default);
    }
    private async Task AgeDeletion(Guid id)
    {
        using var db = NewDb();
        await db.ProductPhotos.Where(p => p.Id == id).ExecuteUpdateAsync(s =>
            s.SetProperty(p => p.DeletedAtUtc, DateTime.UtcNow.AddMinutes(-6)));
    }
    private static byte[] ImageBytes(SKEncodedImageFormat format = SKEncodedImageFormat.Png, int width = 40, int height = 20)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Upload_normalizes_downloads_and_supports_conditional_get(SKEncodedImageFormat format)
    {
        var photo = await UploadOk(ImageBytes(format, 2000, 1000));
        Assert.Equal("image/webp", photo.ContentType);
        Assert.Equal($"products/{_productId:N}/{photo.Id:N}.webp", photo.StorageKey);
        Assert.Single(await List());
        using var response = await _client.GetAsync(photo.DownloadPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(photo.ContentHash, Convert.ToHexString(SHA256.HashData(bytes)));
        Assert.Equal(photo.FileSizeBytes, bytes.LongLength);
        using var image = SKBitmap.Decode(bytes);
        Assert.Equal(1600, image.Width);
        Assert.Equal(800, image.Height);
        Assert.True(response.Headers.CacheControl!.Private);
        using var request = new HttpRequestMessage(HttpMethod.Get, photo.DownloadPath);
        request.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        Assert.Equal(HttpStatusCode.NotModified, (await _client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(photo.DownloadPath.Replace(_productId.ToString(), Guid.NewGuid().ToString()))).StatusCode);
    }

    [Theory]
    [InlineData("Seller")]
    [InlineData("BranchManager")]
    public async Task Staff_can_read_but_cannot_change_photos(string role)
    {
        var photo = await UploadOk();
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Add("X-Test-Role", role);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(photo.DownloadPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Upload(ImageBytes())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync($"{Route}/{photo.Id}/order", new { sortOrder = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync($"{Route}/{photo.Id}")).StatusCode);
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(photo.DownloadPath)).StatusCode);
    }

    [Fact]
    public async Task Rejects_invalid_empty_oversized_and_negative_order_without_storage_writes()
    {
        foreach (var bytes in new[] { Array.Empty<byte>(), "not an image"u8.ToArray(), new byte[5 * 1024 * 1024 + 1] })
            Assert.Equal(HttpStatusCode.BadRequest, (await Upload(bytes)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(ImageBytes(), -1)).StatusCode);
        Assert.Empty(await List());
        Assert.Equal(0, _storage.Writes);
    }

    [Fact]
    public async Task Inactive_and_missing_products_reject_uploads()
    {
        using var db = NewDb();
        await db.Products.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        Assert.Equal(HttpStatusCode.Conflict, (await Upload(ImageBytes())).StatusCode);
        await db.Products.ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await Upload(ImageBytes())).StatusCode);
        Assert.Equal(0, _storage.Writes);
    }

    [Fact]
    public async Task Ordering_and_deletion_hide_photo_immediately_and_cleanup_retries()
    {
        var first = await UploadOk(order: 1);
        var second = await UploadOk(order: 2);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"{Route}/{second.Id}/order", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PutAsJsonAsync($"{Route}/{second.Id}/order", new { sortOrder = 0 })).StatusCode);
        Assert.Equal(second.Id, (await List())[0].Id);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{Route}/{second.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{Route}/{second.Id}")).StatusCode);
        Assert.Equal(first.Id, Assert.Single(await List()).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(second.DownloadPath)).StatusCode);
        await AgeDeletion(second.Id);
        _storage.FailDelete = true;
        await Cleanup();
        using var db = NewDb();
        Assert.True(await db.ProductPhotos.AnyAsync(p => p.Id == second.Id));
        _storage.FailDelete = false;
        await Cleanup();
        Assert.False(await db.ProductPhotos.AnyAsync(p => p.Id == second.Id));
        Assert.Null(await _storage.ReadAsync(second.StorageKey));
        Assert.NotNull(await _storage.ReadAsync(first.StorageKey));
    }

    [Fact]
    public async Task Partial_storage_failure_is_hidden_and_recoverable()
    {
        _storage.FailPut = true;
        Assert.Equal(HttpStatusCode.InternalServerError, (await Upload(ImageBytes())).StatusCode);
        Assert.Empty(await List());
        using var db = NewDb();
        var pending = await db.ProductPhotos.SingleAsync();
        Assert.False(pending.IsReady);
        Assert.NotNull(pending.DeletedAtUtc);
        await AgeDeletion(pending.Id);
        await Cleanup();
        Assert.Empty(await db.ProductPhotos.ToListAsync());
        Assert.Null(await _storage.ReadAsync(pending.StorageKey));
    }

    [Fact]
    public async Task Delete_during_upload_cannot_publish_the_photo()
    {
        _storage.AfterPut = async () =>
        {
            using var db = NewDb();
            await db.ProductPhotos.ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAtUtc, DateTime.UtcNow));
        };
        Assert.Equal(HttpStatusCode.Conflict, (await Upload(ImageBytes())).StatusCode);
        Assert.Empty(await List());
    }

    [Fact]
    public async Task Storage_rejects_traversal_overwrite_and_corrupted_content()
    {
        var photo = await UploadOk();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storage.ReadAsync("../secret"));
        await Assert.ThrowsAsync<IOException>(() => _storage.PutAsync(photo.StorageKey, [1], "image/webp"));
        File.WriteAllBytes(Path.Combine(_root, photo.StorageKey), [1, 2]);
        Assert.Equal(HttpStatusCode.InternalServerError, (await _client.GetAsync(photo.DownloadPath)).StatusCode);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public async Task Jpeg_orientation_is_applied_before_metadata_is_removed(int orientation)
    {
        using var bitmap = new SKBitmap(80, 40);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Blue);
            using var paint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, 40, 40, paint);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        var jpeg = encoded.ToArray();
        // APP1 EXIF with one little-endian TIFF orientation entry.
        byte[] exif = [0xFF, 0xE1, 0, 34, 69, 120, 105, 102, 0, 0,
            73, 73, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 1, 3, 0,
            1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        byte[] oriented = [.. jpeg[..2], .. exif, .. jpeg[2..]];
        var photo = await UploadOk(oriented);
        var bytes = await _client.GetByteArrayAsync(photo.DownloadPath);
        using var output = SKBitmap.Decode(bytes);
        Assert.Equal(40, output.Width);
        Assert.Equal(80, output.Height);
        var top = output.GetPixel(20, 10);
        Assert.True(orientation == 6 ? top.Red > top.Blue : top.Blue > top.Red);
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    [Fact]
    public async Task Expired_pending_upload_is_cleaned_but_fresh_pending_is_kept()
    {
        var expired = await UploadOk();
        var fresh = await UploadOk();
        using var db = NewDb();
        await db.ProductPhotos.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsReady, false));
        await db.ProductPhotos.Where(p => p.Id == expired.Id).ExecuteUpdateAsync(s =>
            s.SetProperty(p => p.CreatedAtUtc, DateTime.UtcNow.AddHours(-2)));
        await Cleanup();
        Assert.NotNull((await db.ProductPhotos.AsNoTracking().SingleAsync(p => p.Id == expired.Id)).DeletedAtUtc);
        Assert.Null((await db.ProductPhotos.AsNoTracking().SingleAsync(p => p.Id == fresh.Id)).DeletedAtUtc);
        Assert.Empty(await List());
        await AgeDeletion(expired.Id);
        await Cleanup();
        Assert.Null(await _storage.ReadAsync(expired.StorageKey));
        Assert.NotNull(await _storage.ReadAsync(fresh.StorageKey));
    }

    public void Dispose()
    {
        _client.Dispose(); _host.Dispose(); _connection.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private sealed class PhotoDb(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var keep = new[] { typeof(Product), typeof(Category), typeof(ProductPhoto) };
            foreach (var entity in modelBuilder.Model.GetEntityTypes().ToArray())
                if (!keep.Contains(entity.ClrType)) modelBuilder.Ignore(entity.ClrType);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToArray())
                    entity.RemoveCheckConstraint(constraint.Name!);
        }
    }

    private sealed class FaultStorage(IProductPhotoStorage inner) : IProductPhotoStorage
    {
        public int Writes { get; private set; }
        public bool FailPut { get; set; }
        public bool FailDelete { get; set; }
        public Func<Task>? AfterPut { get; set; }
        public async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken ct = default)
        {
            Writes++;
            await inner.PutAsync(key, bytes, contentType, ct);
            if (FailPut) throw new IOException("Simulated lost storage response");
            if (AfterPut is not null) await AfterPut();
        }
        public Task<byte[]?> ReadAsync(string key, CancellationToken ct = default) => inner.ReadAsync(key, ct);
        public Task DeleteAsync(string key, CancellationToken ct = default) => FailDelete
            ? Task.FromException(new IOException("Simulated delete failure")) : inner.DeleteAsync(key, ct);
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test"), new Claim(ClaimTypes.Role, role) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
