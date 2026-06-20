using Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.API.Domain;
using Orders.API.Infrastructure;
using System.Net.Http.Json;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(
    OrdersDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(Map(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
    {
        if (request.Lines.Count == 0)
        {
            return BadRequest("At least one order line is required.");
        }

        var catalogBaseUrl = configuration["Services:Catalog"] ?? "http://localhost:5101";
        var client = httpClientFactory.CreateClient("catalog");
        client.BaseAddress = new Uri(catalogBaseUrl);

        var authHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
        }

        var lines = new List<OrderLine>();
        decimal total = 0;

        foreach (var line in request.Lines)
        {
            var productResponse = await client.GetAsync($"/api/products/{line.ProductId}");
            if (!productResponse.IsSuccessStatusCode)
            {
                return BadRequest($"Product '{line.ProductId}' was not found in catalog.");
            }

            var product = await productResponse.Content.ReadFromJsonAsync<CatalogProductSnapshot>();
            if (product is null || !product.IsActive)
            {
                return BadRequest($"Product '{line.ProductId}' is unavailable.");
            }

            var lineTotal = product.UnitPrice * line.Quantity;
            total += lineTotal;

            lines.Add(new OrderLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = line.Quantity,
                UnitPrice = product.UnitPrice
            });
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerName = request.CustomerName,
            Status = "Submitted",
            TotalAmount = total,
            Lines = lines,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, Map(order));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] string status)
    {
        var order = await dbContext.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = User.Identity?.Name;
        await dbContext.SaveChangesAsync();

        return Ok(Map(order));
    }

    private static OrderDto Map(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CustomerName,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.Lines.Select(l => new OrderLineDto(
                l.ProductId,
                l.ProductName,
                l.Quantity,
                l.UnitPrice)).ToList());

    private sealed record CatalogProductSnapshot(
        Guid Id,
        string Name,
        decimal UnitPrice,
        bool IsActive);
}
