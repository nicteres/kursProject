public class Order
{
    public int order_id { get; set; }
    public int user_id { get; set; }
    public int[] product_ids { get; set; } = Array.Empty<int>();
    public order_status status { get; set; }
    
}
public enum order_status
{
    ordered,
    sent,
    shipped,
    received
}