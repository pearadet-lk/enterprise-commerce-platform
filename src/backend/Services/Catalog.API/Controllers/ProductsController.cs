using Catalog.API.Domain;
using Catalog.API.Infrastructure;
using Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(CatalogDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.UnitPrice,
                p.StockQuantity,
                p.IsActive))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(Map(product));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request)
    {
        if (await dbContext.Products.AnyAsync(p => p.Sku == request.Sku))
        {
            return Conflict($"SKU '{request.Sku}' already exists.");
        }

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, Map(product));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, UpdateProductRequest request)
    {
        var product = await dbContext.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.UnitPrice = request.UnitPrice;
        product.StockQuantity = request.StockQuantity;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = User.Identity?.Name;

        await dbContext.SaveChangesAsync();
        return Ok(Map(product));
    }

    private static ProductDto Map(Product product) =>
        new(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.StockQuantity,
            product.IsActive);
}
