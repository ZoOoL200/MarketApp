using MarketApp.Api.Models;
using MarketApp.Application.Common;
using MarketApp.Application.Common.Security;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MarketApp.Api.Controllers;

// Photos are shared catalog data, not branch pricing or stock data.
[ApiController]
[Route("api/products/{productId:guid}/photos")]
[Authorize(Policy = "AuthenticatedUser", Roles = AppRoles.Stakeholder + "," + AppRoles.BranchManager + "," + AppRoles.Seller)]
public class ProductPhotosController(IProductPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MarketApp.Application.DTOs.Products.ProductPhotoDto>), 200)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetList(Guid productId, CancellationToken cancellationToken)
    {
        var photos = await photoService.GetListAsync(productId, cancellationToken);
        return photos is null ? NotFound() : Ok(photos);
    }

    [HttpGet("{photoId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid productId, Guid photoId, CancellationToken cancellationToken)
    {
        var content = await photoService.GetContentAsync(productId, photoId, cancellationToken);
        if (content is null) return NotFound();
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return new FileContentResult(content.Bytes, content.ContentType)
        {
            EntityTag = new EntityTagHeaderValue($"\"{content.ContentHash}\""),
            LastModified = new DateTimeOffset(content.CreatedAtUtc)
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(MarketApp.Application.DTOs.Products.ProductPhotoDto), 201)]
    [Authorize(Roles = AppRoles.Stakeholder)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductPhotoLimits.MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductPhotoLimits.MaximumRequestBytes)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Upload(Guid productId, [FromForm] UploadProductPhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0 || request.File.Length > ProductPhotoLimits.MaximumFileBytes)
            return Problem(statusCode: 400, title: "Invalid photo.", detail: "Choose an image between 1 byte and 5 MB.");

        await using var input = request.File.OpenReadStream();
        var result = await photoService.UploadAsync(productId, input, request.SortOrder, cancellationToken);
        if (result.Status == PhotoUploadStatus.Success)
            return CreatedAtAction(nameof(GetContent), new { productId, photoId = result.Photo!.Id }, result.Photo);

        var statusCode = result.Status switch
        {
            PhotoUploadStatus.NotFound => 404,
            PhotoUploadStatus.Conflict => 409,
            _ => 400
        };
        return Problem(statusCode: statusCode, title: "Photo upload could not be completed.", detail: result.Error);
    }

    [HttpPut("{photoId:guid}/order")]
    [ProducesResponseType(204)]
    [Authorize(Roles = AppRoles.Stakeholder)]
    public async Task<IActionResult> SetOrder(Guid productId, Guid photoId,
        [FromBody] UpdateProductPhotoOrderRequest request, CancellationToken cancellationToken)
    {
        var updated = await photoService.SetSortOrderAsync(productId, photoId, request.SortOrder!.Value, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{photoId:guid}")]
    [ProducesResponseType(204)]
    [Authorize(Roles = AppRoles.Stakeholder)]
    public async Task<IActionResult> Delete(Guid productId, Guid photoId, CancellationToken cancellationToken)
    {
        await photoService.DeleteAsync(productId, photoId, cancellationToken);
        return NoContent();
    }
}
