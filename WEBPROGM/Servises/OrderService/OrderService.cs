using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class OrderService : IOrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMessageBroker messageBroker, ILogger<OrderService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task CreateOrder(int userId, int[] productIds)
    {
        _logger.LogInformation("Initiating CreateOrder for UserId: {UserId}, ProductIds: {ProductIds}", userId, productIds);

        var tcs = new TaskCompletionSource<bool>();

        var orderRequest = new { UserId = userId, ProductIds = productIds };
        _messageBroker.Subscribe($"CreateOrderResponse_{userId}", message =>
        {
            if (message == "Success")
            {
                _logger.LogInformation("CreateOrder succeeded for UserId: {UserId}", userId);
                tcs.SetResult(true);
            }
            else
            {
                _logger.LogError("CreateOrder failed for UserId: {UserId}. Message: {Message}", userId, message);
                tcs.SetResult(false);
            }
        });

        try
        {
            _messageBroker.Publish("CreateOrder", JsonConvert.SerializeObject(orderRequest));
            _logger.LogDebug("Published CreateOrder for UserId: {UserId}", userId);

            var timeout = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeout);

            if (completedTask == timeout)
            {
                _logger.LogWarning("CreateOrder request timed out for UserId: {UserId}", userId);
                throw new TimeoutException("CreateOrder request timed out.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while processing CreateOrder for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserId(int userId)
    {
        _logger.LogInformation("Fetching orders for UserId: {UserId}", userId);

        var tcs = new TaskCompletionSource<IEnumerable<Order>>();

        _messageBroker.Subscribe($"GetOrdersByUserIdResponse_{userId}", message =>
        {
            try
            {
                var orders = JsonConvert.DeserializeObject<IEnumerable<Order>>(message);
                if (orders != null)
                {
                    _logger.LogInformation("Successfully fetched orders for UserId: {UserId}", userId);
                    tcs.SetResult(orders);
                }
                else
                {
                    _logger.LogError("Received null orders for UserId: {UserId}", userId);
                    _messageBroker.Publish("GetOrdersByUserId", "Orders cannot be null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process orders response for UserId: {UserId}", userId);
            }
        });

        _messageBroker.Publish("GetOrdersByUserId", JsonConvert.SerializeObject(userId));
        _logger.LogDebug("Published GetOrdersByUserId for UserId: {UserId}", userId);

        var timeout = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(tcs.Task, timeout);

        if (completedTask == timeout)
        {
            _logger.LogWarning("GetOrdersByUserId request timed out for UserId: {UserId}", userId);
            throw new TimeoutException("GetOrdersByUserId request timed out.");
        }

        return await tcs.Task;
    }

    public async Task UpdateOrderStatus(int orderId, order_status status)
    {
        _logger.LogInformation("Updating order status for OrderId: {OrderId}, Status: {Status}", orderId, status);

        var updateRequest = new { OrderId = orderId, Status = status };

        try
        {
            await Task.Run(() =>
                _messageBroker.Publish("UpdateOrderStatus", JsonConvert.SerializeObject(updateRequest))
            );

            _logger.LogInformation("Order status updated successfully for OrderId: {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for OrderId: {OrderId}", orderId);
            throw;
        }
    }
}
