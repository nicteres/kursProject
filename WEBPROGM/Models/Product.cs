public class Product
{
    public int product_id { get; set; }
    public string name { get; set; } = string.Empty;
    public int price { get; set; }
    public string image_url { get; set; } = string.Empty;
    public string[] category { get; set; } = Array.Empty<string>();
}