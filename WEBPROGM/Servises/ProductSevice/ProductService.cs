using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class ProductService : IProductService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IMessageBroker messageBroker, ILogger<ProductService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        _logger.LogInformation("Starting GetAllAsync method.");

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "GetAllProductsResponse";

        try
        {
            _messageBroker.Subscribe(responseTopic, message =>
            {
                _logger.LogDebug("Received message on topic {Topic}: {Message}", responseTopic, message);
                tcs.SetResult(message);
            });

            _logger.LogInformation("Publishing message to topic GetAllProducts.");
            _messageBroker.Publish("GetAllProducts", null!);
            var response = await tcs.Task;

            _logger.LogInformation("Successfully retrieved products.");
            return JsonConvert.DeserializeObject<List<Product>>(response)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting all products.");
            throw;
        }
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        _logger.LogInformation("Starting GetByIdAsync method with ID {Id}.", id);

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"GetProductByIdResponse_{id}";

        try
        {
            _messageBroker.Subscribe(responseTopic, message =>
            {
                _logger.LogDebug("Received message on topic {Topic}: {Message}", responseTopic, message);
                tcs.SetResult(message);
            });

            _logger.LogInformation("Publishing message to topic GetProductById with ID {Id}.", id);
            _messageBroker.Publish("GetProductById", JsonConvert.SerializeObject(id));
            var response = await tcs.Task;

            _logger.LogInformation("Successfully retrieved product with ID {Id}.", id);
            return JsonConvert.DeserializeObject<Product>(response)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting product by ID {Id}.", id);
            throw;
        }
    }

    public async Task AddAsync(Product product)
    {
        _logger.LogInformation("Starting AddAsync method for product {Product}.", product);

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "AddProductResponse";

        try
        {
            _messageBroker.Subscribe(responseTopic, message =>
            {
                _logger.LogDebug("Received message on topic {Topic}: {Message}", responseTopic, message);
                tcs.SetResult(message);
            });

            _logger.LogInformation("Publishing message to topic AddProduct.");
            _messageBroker.Publish("AddProduct", JsonConvert.SerializeObject(product));
            await tcs.Task;

            _logger.LogInformation("Successfully added product.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding a product.");
            throw;
        }
    }

    public async Task UpdateAsync(Product product)
    {
        _logger.LogInformation("Starting UpdateAsync method for product {Product}.", product);

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = "UpdateProductResponse";

        try
        {
            _messageBroker.Subscribe(responseTopic, message =>
            {
                _logger.LogDebug("Received message on topic {Topic}: {Message}", responseTopic, message);
                tcs.SetResult(message);
            });

            _logger.LogInformation("Publishing message to topic UpdateProduct.");
            _messageBroker.Publish("UpdateProduct", JsonConvert.SerializeObject(product));
            await tcs.Task;

            _logger.LogInformation("Successfully updated product.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating a product.");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Starting DeleteAsync method for product ID {Id}.", id);

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"DeleteProductResponse_{id}";

        try
        {
            _messageBroker.Subscribe(responseTopic, message =>
            {
                _logger.LogDebug("Received message on topic {Topic}: {Message}", responseTopic, message);
                tcs.SetResult(message);
            });

            _logger.LogInformation("Publishing message to topic DeleteProduct with ID {Id}.", id);
            _messageBroker.Publish("DeleteProduct", JsonConvert.SerializeObject(id));
            await tcs.Task;

            _logger.LogInformation("Successfully deleted product with ID {Id}.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting product with ID {Id}.", id);
            throw;
        }
    }
}
