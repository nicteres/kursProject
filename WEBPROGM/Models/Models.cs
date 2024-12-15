public class product
{
    public int product_id { get; set; }
    public string name { get; set; }
    public int price { get; set; }
    public string image_url { get; set; }
    public string[] category { get; set; }
}
public class user
{
    public int user_id { get; set; }
    public string login { get; set; }
    public string password { get; set; }
}
public class cart
{
    public int cart_id { get; set; }
    public int user_id { get; set; }
    public int[] product_ids { get; set; }
}
public class order
{
    public int order_id { get; set; }
    public int user_id { get; set; }
    public int[] product_ids { get; set; }
    public string status { get; set; }
}
