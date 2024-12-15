using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IUserService _userService;

    public CartController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("{userId}/add/{productId}")]
    public async Task<IActionResult> AddToCart(int userId, int productId)
    {
        await _userService.AddToCart(userId, productId);
        return Ok("Product added to cart.");
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        var cart = await _userService.GetCart(userId);
        return Ok(cart);
    }
    [HttpGet("{userId}/ids")]
    public async Task<IActionResult> GetCartIds(int userId)
    {
        var cart = await _userService.GetCartIds(userId);
        return Ok(cart);
    }

    [HttpDelete("{userId}/clear")]
    public async Task<IActionResult> ClearCart(int userId)
    {
        await _userService.ClearCart(userId);
        return Ok("Cart cleared.");
    }
}
