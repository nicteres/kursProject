using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;

public interface IProductService
{
    Task<IEnumerable<product>> GetAllAsync();
    Task<product> GetByIdAsync(int id);
    Task AddAsync(product product);
    Task UpdateAsync(product product);
    Task DeleteAsync(int id);
}

public interface IMessageBroker
{
    void Publish(string topic, string message);
    void Subscribe(string topic, Action<string> handler);
}


public interface IUserService
{
    Task<int[]> GetCartIds(int userId);
    Task RegisterUser(user user);
    Task<user> Authenticate(string login, string password);
    Task AddToCart(int userId, int productId);
    Task<product[]> GetCart(int userId);
    Task ClearCart(int userId);
}



public interface IOrderService
{
    Task CreateOrder(int userId, int[] productIds);
    Task<IEnumerable<order>> GetOrdersByUserId(int userId);
    Task UpdateOrderStatus(int orderId, string status);
}
public interface IRecommendationService
{
    Task<IEnumerable<product>> GetRecommendationsAsync(int userId);
}
public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;

    public RecommendationService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<product>> GetRecommendationsAsync(int userId)
    {

        var purchasedProducts = await _context.orders
            .Where(o => o.user_id == userId)
            .SelectMany(o => o.product_ids)
            .Distinct()
            .ToListAsync();

        
        var purchasedProductsDetails = await _context.products
            .Where(p => purchasedProducts.Contains(p.product_id))
            .ToListAsync();

        
        var purchasedCategories = purchasedProductsDetails
            .SelectMany(p => p.category)
            .Distinct()
            .ToList();

       
        var allProducts = await _context.products.ToListAsync();

        var recommendations = allProducts
            .Where(p => p.category.Any(c => purchasedCategories.Contains(c))
                        && !purchasedProducts.Contains(p.product_id)) 
            .OrderBy(p => p.price) 
            .ToList();

        return recommendations;
    }
}


public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<product>> GetAllAsync()
    {
        return await _context.products.ToListAsync();
    }

    public async Task<product> GetByIdAsync(int id)
    {
        return await _context.products.FindAsync(id);
    }

    public async Task AddAsync(product product)
    {
        _context.products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(product product)
    {
        _context.products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.products.FindAsync(id);
        if (product != null)
        {
            _context.products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private static readonly string JwtSecretKey = "pneumonoultramicroscopicsilicovolcanoconiosis";


    public UserService(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task RegisterUser(user user)
    {
        user.password = HashPassword(user.password);
        _dbContext.users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<user> Authenticate(string login, string password)
    {
        var user = await _dbContext.users.FirstOrDefaultAsync(u => u.login == login);
        if (user != null && VerifyPassword(password, user.password))
        {
            user.password = GenerateJwtToken(user);
            return user;
        }
        return null;
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }

    public string GenerateJwtToken(user user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecretKey); 
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, user.login),
            new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString())
        }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }


    public async Task AddToCart(int userId, int productId)
    {
        var curentcart = _dbContext.carts.FirstOrDefault(i => i.user_id == userId);
        if (curentcart != null) {
            List<int> products = curentcart.product_ids.ToList();
            products.Add(productId);
            curentcart.product_ids = products.ToArray();

        }
        else
        {
            int[] products = new int[1];
            products[0] = productId;
            cart newcart = new cart
            {
                user_id = userId,
                product_ids = products
            };
            _dbContext.carts.Add(newcart);
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<product[]> GetCart(int userId)
    {

        var cartItems = _dbContext.carts.ToList<cart>().FirstOrDefault(i => i.user_id == userId);
        List<product> products = new List<product>();
        if (cartItems != null)
        {

            foreach (int id in cartItems.product_ids)
            {
                products.Add(_dbContext.products.ToList<product>().Find(i => i.product_id == id));
            }
            return products.ToArray();
        }
        else { return null; }
    }
    public async Task<int[]> GetCartIds(int userId)
    {

    
        var cartItems = _dbContext.carts.ToList<cart>().FirstOrDefault(i => i.user_id == userId);
        if (cartItems != null)
        {
            return cartItems.product_ids;

        }
        else {return null; }
    }
    public async Task ClearCart(int userId)
    {
        var curentcart = _dbContext.carts.FirstOrDefault(i => i.user_id == userId);
        if (curentcart != null)
        {
            List<int> products = curentcart.product_ids.ToList();
            products.Clear();
            curentcart.product_ids = products.ToArray();

        }

        await _dbContext.SaveChangesAsync();
    }
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IMessageBroker _messageBroker;

    public OrderService(ApplicationDbContext context, IMessageBroker messageBroker)
    {
        _context = context;
        _messageBroker = messageBroker;
    }

    public async Task CreateOrder(int userId, int[] productIds)
    {
        if (productIds == null || productIds.Length == 0)
        {
            throw new ArgumentException("Product IDs cannot be empty.");
        }

        var order = new order
        {
            user_id = userId,
            product_ids = productIds,
            status = "Pending"
        };

        _context.orders.Add(order);
        await _context.SaveChangesAsync();

        _messageBroker.Publish("OrderCreated", $"Order {order.order_id} created for User {userId}");
    }

    public async Task<IEnumerable<order>> GetOrdersByUserId(int userId)
    {
        return await _context.orders.Where(o => o.user_id == userId).ToListAsync();
    }

    public async Task UpdateOrderStatus(int orderId, string status)
    {
        var order = await _context.orders.FindAsync(orderId);
        if (order != null)
        {
            order.status = status;
            _context.orders.Update(order);
            await _context.SaveChangesAsync();

            _messageBroker.Publish("OrderStatusUpdated", $"Order {orderId} status updated to {status}");
        }
    }
}
public class SimpleMessageBroker : IMessageBroker
{
    private readonly Dictionary<string, List<Action<string>>> _subscribers = new();

    public void Publish(string topic, string message)
    {
        if (_subscribers.ContainsKey(topic))
        {
            foreach (var handler in _subscribers[topic])
            {
                handler(message);
            }
        }
    }

    public void Subscribe(string topic, Action<string> handler)
    {
        if (!_subscribers.ContainsKey(topic))
        {
            _subscribers[topic] = new List<Action<string>>();
        }
        _subscribers[topic].Add(handler);
    }
}