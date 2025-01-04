using Microsoft.AspNetCore.Mvc;

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
    public IActionResult Register(User user)
    {

        _userService.RegisterUser(user);
        return Ok("User registration initiated.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User user)
    {

        var authenticatedUser = await _userService.Authenticate(user.login, user.password);
        if (authenticatedUser == null)
            return Unauthorized("Invalid login or password.");

        return Ok(authenticatedUser);
    }
}