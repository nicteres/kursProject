using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

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

    void RegisterUser(user user);
    Task<user> Authenticate(string login, string password);

  
    Task<int[]> GetCartIds(int userId); 
    Task<product[]> GetCart(int userId);
    void AddToCart(int userId, int productId); 
    void ClearCart(int userId); 
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
    private readonly IMessageBroker _messageBroker;

    public RecommendationService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task<IEnumerable<product>> GetRecommendationsAsync(int userId)
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"GetRecommendationsResponse_{userId}";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("GetRecommendations", JsonConvert.SerializeObject(userId));
        var response = await tcs.Task;

        return JsonConvert.DeserializeObject<IEnumerable<product>>(response);
    }
}



public class ProductService : IProductService
{
    private readonly IMessageBroker _messageBroker;

    public ProductService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task<IEnumerable<product>> GetAllAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "GetAllProductsResponse";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("GetAllProducts", null);
        var response = await tcs.Task;

        return JsonConvert.DeserializeObject<List<product>>(response);
    }

    public async Task<product> GetByIdAsync(int id)
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"GetProductByIdResponse_{id}";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("GetProductById", JsonConvert.SerializeObject(id));
        var response = await tcs.Task;

        return JsonConvert.DeserializeObject<product>(response);
    }

    public async Task AddAsync(product product)
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "AddProductResponse";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("AddProduct", JsonConvert.SerializeObject(product));
        await tcs.Task;
    }

    public async Task UpdateAsync(product product)
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "UpdateProductResponse";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("UpdateProduct", JsonConvert.SerializeObject(product));
        await tcs.Task;
    }

    public async Task DeleteAsync(int id)
    {
        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"DeleteProductResponse_{id}";

        _messageBroker.Subscribe(responseTopic, message =>
        {
            tcs.SetResult(message);
        });

        _messageBroker.Publish("DeleteProduct", JsonConvert.SerializeObject(id));
        await tcs.Task;
    }
}

public class UserService : IUserService
{
    private readonly IMessageBroker _messageBroker;

    public UserService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public void RegisterUser(user user)
    {
        user.password = HashPassword(user.password);
        _messageBroker.Publish("RegisterUser", JsonConvert.SerializeObject(user));
    }

    public async Task<user> Authenticate(string login, string password)
    {
        var tcs = new TaskCompletionSource<user>();

        _messageBroker.Subscribe($"AuthenticateUserResponse_{login}", message =>
        {
            if (message != null)
            {
                var response = JsonConvert.DeserializeObject<dynamic>(message);
                var user = JsonConvert.DeserializeObject<user>(response.User.ToString());
                tcs.SetResult(user);
            }
            else
            {
                tcs.SetResult(null);
            }
        });

        _messageBroker.Publish("AuthenticateUser", JsonConvert.SerializeObject((login, password)));



        return await tcs.Task;
    }

    public async Task<int[]> GetCartIds(int userId)
    {
        var tcs = new TaskCompletionSource<int[]>();
        _messageBroker.Subscribe($"GetCartIdsResponse_{userId}", message =>
        {
            var productIds = JsonConvert.DeserializeObject<int[]>(message);
            tcs.SetResult(productIds);
        });

        _messageBroker.Publish("GetCartIds", JsonConvert.SerializeObject(userId));


        return await tcs.Task;
    }

    public async Task<product[]> GetCart(int userId)
    {
        var tcs = new TaskCompletionSource<product[]>();
        _messageBroker.Subscribe($"GetCartResponse_{userId}", message =>
        {
            var products = JsonConvert.DeserializeObject<product[]>(message);
            tcs.SetResult(products);
        });

        _messageBroker.Publish("GetCart", JsonConvert.SerializeObject(userId));


        return await tcs.Task;
    }

    public void AddToCart(int userId, int productId)
    {
        _messageBroker.Publish("AddToCart", JsonConvert.SerializeObject((userId, productId)));
    }

    public void ClearCart(int userId)
    {
        _messageBroker.Publish("ClearCart", JsonConvert.SerializeObject(userId));
    }

    public string GenerateJwtToken(user user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("pneumonoultramicroscopicsilicovolcanoconiosis");
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

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }
}

public class OrderService : IOrderService
{
    private readonly IMessageBroker _messageBroker;

    public OrderService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task CreateOrder(int userId, int[] productIds)
    {
        var tcs = new TaskCompletionSource<bool>();


        var orderRequest = new { UserId = userId, ProductIds = productIds };
        _messageBroker.Subscribe($"CreateOrderResponse_{userId}", message =>
        {
            if (message == "Success")
            {
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetResult(false);
            }
        });

        _messageBroker.Publish("CreateOrder", JsonConvert.SerializeObject(orderRequest));

        
        var timeout = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(tcs.Task, timeout);

        if (completedTask == timeout)
        {
            throw new TimeoutException("CreateOrder request timed out.");
        }
    }


    public async Task<IEnumerable<order>> GetOrdersByUserId(int userId)
    {
        var tcs = new TaskCompletionSource<IEnumerable<order>>();


        _messageBroker.Subscribe($"GetOrdersByUserIdResponse_{userId}", message =>
        {
            var orders = JsonConvert.DeserializeObject<IEnumerable<order>>(message);
            tcs.SetResult(orders);
        });



        _messageBroker.Publish("GetOrdersByUserId", JsonConvert.SerializeObject(userId));
        var timeout = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(tcs.Task, timeout);

        if (completedTask == timeout)
        {
            throw new TimeoutException("GetOrdersByUserId request timed out.");
        }



        return await tcs.Task;
    }

    public async Task UpdateOrderStatus(int orderId, string status)
    {
        var tcs = new TaskCompletionSource<bool>();


        var updateRequest = new { OrderId = orderId, Status = status };


        _messageBroker.Publish("UpdateOrderStatus", JsonConvert.SerializeObject(updateRequest));

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

