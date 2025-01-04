using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class DatabaseHandler : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMessageBroker _messageBroker;

    public DatabaseHandler(IServiceProvider serviceProvider, IMessageBroker messageBroker)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _messageBroker = messageBroker;
      
    }

    private void SubscribeToCommands()
    {
        _messageBroker.Subscribe("GetUser", HandleGetUser);
        _messageBroker.Subscribe("RegisterUser", HandleAddUser);
        _messageBroker.Subscribe("AuthenticateUser", HandleAuthenticateUser);
        _messageBroker.Subscribe("GetCart", HandleGetCart);
        _messageBroker.Subscribe("GetCartIds", HandleGetCartIds);
        _messageBroker.Subscribe("AddToCart", HandleAddToCart);
        _messageBroker.Subscribe("ClearCart", HandleClearCart);


        _messageBroker.Subscribe("CreateOrder", HandleCreateOrder);
        _messageBroker.Subscribe("GetOrdersByUserId", HandleGetOrdersByUserId);
        _messageBroker.Subscribe("UpdateOrderStatus", HandleUpdateOrderStatus);

        _messageBroker.Subscribe("GetAllProducts", HandleGetAllProducts);
        _messageBroker.Subscribe("GetProductById", HandleGetProductById);
        _messageBroker.Subscribe("AddProduct", HandleAddProduct);
        _messageBroker.Subscribe("UpdateProduct", HandleUpdateProduct);
        _messageBroker.Subscribe("DeleteProduct", HandleDeleteProduct);

        _messageBroker.Subscribe("GetRecommendations", HandleGetRecommendations);
    }

    public void HandleGetUser(string message)
    {
        var userId = JsonConvert.DeserializeObject<int>(message);
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = dbContext.users.FirstOrDefault(u => u.user_id == userId);
        _messageBroker.Publish($"GetUserResponse_{userId}", JsonConvert.SerializeObject(user));
    }

    public void HandleAddUser(string message)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = JsonConvert.DeserializeObject<User>(message);
        if (user != null)
        {
            dbContext.users.Add(user);
            dbContext.SaveChanges();
            _messageBroker.Publish("AddUserResponse", $"User {user.login} added successfully.");
        }
        else
        {
            _messageBroker.Publish("AddUserResponse", $"User cannot be null");
        }
    }

    public void HandleAuthenticateUser(string message)
    {
        var credentials = JsonConvert.DeserializeObject<(string Login, string Password)>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


        var user = dbContext.users.FirstOrDefault(u => u.login == credentials.Login);

        if (user != null && VerifyPassword(credentials.Password, user.password))
        {

            var response = new
            {
                User = user,
                Token = GenerateJwtToken(user)
            };
            _messageBroker.Publish($"AuthenticateUserResponse_{credentials.Login}", JsonConvert.SerializeObject(response));
        }
        else
        {

            _messageBroker.Publish($"AuthenticateUserResponse_{credentials.Login}", "unable to authenticate");
        }
    }

    public void HandleGetCart(string message)
    {
        var userId = JsonConvert.DeserializeObject<int>(message);
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cart = dbContext.carts.FirstOrDefault(c => c.user_id == userId);
        var products = cart?.product_ids.Select(id => dbContext.products.FirstOrDefault(p => p.product_id == id)).ToArray();

        _messageBroker.Publish($"GetCartResponse_{userId}", JsonConvert.SerializeObject(products));
    }

    public void HandleGetCartIds(string message)
    {
        var userId = JsonConvert.DeserializeObject<int>(message);
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cart = dbContext.carts.FirstOrDefault(c => c.user_id == userId);
        _messageBroker.Publish($"GetCartIdsResponse_{userId}", JsonConvert.SerializeObject(cart?.product_ids ?? Array.Empty<int>()));
    }
    public void HandleAddToCart(string message)
    {
        var request = JsonConvert.DeserializeObject<(int UserId, int ProductId)>(message);
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cart = dbContext.carts.FirstOrDefault(c => c.user_id == request.UserId);
        if (cart == null)
        {
            cart = new Cart { user_id = request.UserId, product_ids = new int[] { request.ProductId } };
            dbContext.carts.Add(cart);
        }
        else
        {
            cart.product_ids = cart.product_ids.Append(request.ProductId).ToArray();
        }

        dbContext.SaveChanges();
        _messageBroker.Publish($"AddToCartResponse_{request.UserId}", $"Product {request.ProductId} added to cart.");
    }

    public void HandleClearCart(string message)
    {
        var userId = JsonConvert.DeserializeObject<int>(message);
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cart = dbContext.carts.FirstOrDefault(c => c.user_id == userId);
        if (cart != null)
        {
            cart.product_ids = Array.Empty<int>();
            dbContext.SaveChanges();
        }

        _messageBroker.Publish($"ClearCartResponse_{userId}", "Cart cleared.");
    }


    private string GenerateJwtToken(User user)
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
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes) == hashedPassword;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeToCommands();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    public async void HandleCreateOrder(string message)
    {
        OrderRequest orderRequest = JsonConvert.DeserializeObject<OrderRequest>(message)!;
        Console.WriteLine(orderRequest);
        
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (orderRequest != null)
        {
            {

                if (orderRequest.ProductIds == null || orderRequest.ProductIds.Length == 0)
                {
                    throw new ArgumentException("Product IDs cannot be empty.");
                }

                var order = new Order
                {
                    user_id = orderRequest.UserId,
                    product_ids = orderRequest.ProductIds,
                    status = order_status.ordered
                };

                dbContext.orders.Add(order);
                await dbContext.SaveChangesAsync();

                _messageBroker.Publish($"CreateOrderResponse_{orderRequest.UserId}", "Success");
            }
        }
        else
        {
            _messageBroker.Publish($"CreateOrderResponse_", "Deny");
        }
    }

    public void HandleGetOrdersByUserId(string message)
{
    var userId = JsonConvert.DeserializeObject<int>(message);

    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var orders = dbContext.orders.Where(o => o.user_id == userId).ToList();
    _messageBroker.Publish($"GetOrdersByUserIdResponse_{userId}", JsonConvert.SerializeObject(orders));
}

    public void HandleUpdateOrderStatus(string message)
{
    var updateRequest = JsonConvert.DeserializeObject<OrderStatusRequest>(message);

    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (updateRequest != null)
        {

            var order = dbContext.orders.FirstOrDefault(o => o.order_id == updateRequest.OrderId);
            if (order != null)
            {
                if(updateRequest.Status>order.status)
                order.status = updateRequest.Status;
                dbContext.orders.Update(order);
                dbContext.SaveChanges();

                _messageBroker.Publish($"UpdateOrderStatusResponse_{updateRequest.OrderId}", "Success");
            }
            else
            {
                _messageBroker.Publish($"UpdateOrderStatusResponse_{updateRequest.OrderId}", "Failure");
            }
        }
        else
        {
            _messageBroker.Publish($"UpdateOrderStatusResponse_", "deny");
        }
}
    private void HandleGetAllProducts(string message)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var products = dbContext.products.ToList();
        _messageBroker.Publish("GetAllProductsResponse", JsonConvert.SerializeObject(products));
    }

    private void HandleGetProductById(string message)
    {
        var productId = JsonConvert.DeserializeObject<int>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = dbContext.products.FirstOrDefault(p => p.product_id == productId);
        _messageBroker.Publish($"GetProductByIdResponse_{productId}", JsonConvert.SerializeObject(product));
    }

    private void HandleAddProduct(string message)
    {
        var product = JsonConvert.DeserializeObject<Product>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (product != null)
        {
            dbContext.products.Add(product);
            dbContext.SaveChanges();

            _messageBroker.Publish("AddProductResponse", "Success");
        }
        else
        {
            _messageBroker.Publish("AddProductResponse", "Product cant be null");
        }
    }

    private void HandleUpdateProduct(string message)
    {
        var product = JsonConvert.DeserializeObject<Product>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (product != null)
        {

            dbContext.products.Update(product);
            dbContext.SaveChanges();

            _messageBroker.Publish("UpdateProductResponse", "Success");
        }
        else
        {
            _messageBroker.Publish("AddProductResponse", "Product cannot be null");
        }
    }

    private void HandleDeleteProduct(string message)
    {
        var productId = JsonConvert.DeserializeObject<int>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = dbContext.products.FirstOrDefault(p => p.product_id == productId);
        if (product != null)
        {
            dbContext.products.Remove(product);
            dbContext.SaveChanges();
        }

        _messageBroker.Publish($"DeleteProductResponse_{productId}", "Success");
    }

    private void HandleGetRecommendations(string message)
    {
        var userId = JsonConvert.DeserializeObject<int>(message);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
             
        var purchasedProducts = dbContext.orders
            .Where(o => o.user_id == userId)
            .SelectMany(o => o.product_ids)
            .Distinct()
            .ToList();

        var purchasedProductsDetails = dbContext.products
            .Where(p => purchasedProducts.Contains(p.product_id))
            .ToList();

        var purchasedCategories = purchasedProductsDetails
            .SelectMany(p => p.category)
            .Distinct()
            .ToList();

        var allProducts = dbContext.products.ToList();

        var recommendations = allProducts
            .Where(p => p.category.Any(c => purchasedCategories.Contains(c))
                        && !purchasedProducts.Contains(p.product_id))
            .OrderBy(p => p.price)
            .ToList();

        _messageBroker.Publish($"GetRecommendationsResponse_{userId}", JsonConvert.SerializeObject(recommendations));
    }

}
