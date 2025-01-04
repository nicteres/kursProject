public class AddToCartRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
}
public class OrderRequest
{
    public int UserId { get; set; }
    public int[] ProductIds { get; set; } = Array.Empty<int>();
}
public class OrderStatusRequest
{
    public int OrderId { get; set; }
    public order_status Status { get; set; }
}