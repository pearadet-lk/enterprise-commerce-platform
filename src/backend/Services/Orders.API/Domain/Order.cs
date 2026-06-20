using SharedKernel.Entities;

namespace Orders.API.Domain;

public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public ICollection<OrderLine> Lines { get; set; } = [];
}

public class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
