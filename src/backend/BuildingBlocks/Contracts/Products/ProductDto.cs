namespace Contracts.Products;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockQuantity,
    bool IsActive);

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockQuantity);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockQuantity,
    bool IsActive);
