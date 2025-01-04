using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> CreateOrder(int userId, [FromBody] int[] productIds)
    {
        if (productIds == null || productIds.Length == 0)
        {
            return BadRequest("Product IDs cannot be empty.");
        }


        await _orderService.CreateOrder(userId, productIds);
        return Ok(new { message = "Order created successfully." });

    }


    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetOrdersByUserId(int userId)
    {
        var orders = await _orderService.GetOrdersByUserId(userId);
        return Ok(orders);
    }

    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] order_status status)
    {
        await _orderService.UpdateOrderStatus(orderId, status);
        return NoContent();
    }
}