public class Cart
{
    public int cart_id { get; set; }
    public int user_id { get; set; }
    public int[] product_ids { get; set; } = Array.Empty<int>();
}