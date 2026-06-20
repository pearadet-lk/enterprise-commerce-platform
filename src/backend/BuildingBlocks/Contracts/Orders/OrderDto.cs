namespace Contracts.Orders;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineDto> Lines);

public record OrderLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record CreateOrderRequest(
    string CustomerName,
    IReadOnlyList<CreateOrderLineRequest> Lines);

public record CreateOrderLineRequest(
    Guid ProductId,
    int Quantity);
