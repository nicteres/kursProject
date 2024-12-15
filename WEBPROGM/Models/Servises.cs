using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

public interface IProductService
{
    Task<IEnumerable<product>> GetAllAsync();
    Task<product> GetByIdAsync(int id);
    Task AddAsync(product product);
    Task UpdateAsync(product product);
    Task DeleteAsync(int id);
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

        var purchasedCategories = await _context.products
            .Where(p => purchasedProducts.Contains(p.product_id))
            .Select(p => p.category)
            .Distinct()
            .ToListAsync();

        var recommendations = await _context.products
            .Where(p => purchasedCategories.Contains(p.category) && !purchasedProducts.Contains(p.product_id))
            .OrderBy(p => p.price)
            .ToListAsync();

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

    public UserService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterUser(user user)
    {
        _dbContext.users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<user> Authenticate(string login, string password)
    {
        return await _dbContext.users.FirstOrDefaultAsync(u => u.login == login && u.password == password);
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

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
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
        }
    }
}