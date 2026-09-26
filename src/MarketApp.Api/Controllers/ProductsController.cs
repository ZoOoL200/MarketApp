using MarketApp.Application.Common;
using MarketApp.Application.Common.Result;
using MarketApp.Application.DTOs.Products;
using MarketApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPage(
        [FromQuery] ProductQueryDto query,
        CancellationToken cancellationToken)
    {
        var page = await _productService.GetPageAsync(
            query,
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] SaveProductDto request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(
            request,
            cancellationToken);

        if (result.Status != ProductSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        var product = result.Product!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] SaveProductDto request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result.Status != ProductSaveStatus.Success)
        {
            return SaveFailure(result.Status);
        }

        return Ok(result.Product);
    }

    private ObjectResult SaveFailure(ProductSaveStatus status)
    {
        return status switch
        {
            ProductSaveStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Product not found."),

            ProductSaveStatus.CategoryNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid category.",
                detail: "Choose an existing category."),

            ProductSaveStatus.DuplicateSku => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "SKU already exists.",
                detail: "Another product uses this SKU."),

            _ => throw new InvalidOperationException(
                "Unexpected product save result.")
        };
    }
}