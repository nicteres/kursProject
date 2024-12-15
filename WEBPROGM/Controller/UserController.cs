using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(user user)
    {
        await _userService.RegisterUser(user);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] user user)
    {
        var authenticatedUser = await _userService.Authenticate(user.login, user.password);
        if (authenticatedUser == null) return Unauthorized();
        return Ok(authenticatedUser);
    }

}