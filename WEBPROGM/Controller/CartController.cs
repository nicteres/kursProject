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
    public IActionResult AddToCart(int userId, int productId)
    {
        _userService.AddToCart(userId, productId);
        return Ok("Product added to cart.");
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        var cart = await _userService.GetCart(userId);
        if (cart == null || !cart.Any())
            return NotFound("Cart is empty or user does not exist.");

        return Ok(cart);
    }

    [HttpGet("{userId}/ids")]
    public async Task<IActionResult> GetCartIds(int userId)
    {
        var cartIds = await _userService.GetCartIds(userId);
        if (cartIds == null || !cartIds.Any())
            return NotFound("Cart IDs are empty or user does not exist.");

        return Ok(cartIds);
    }

    [HttpDelete("{userId}/clear")]
    public IActionResult ClearCart(int userId)
    {
        _userService.ClearCart(userId);
        return Ok("Cart cleared.");
    }
}