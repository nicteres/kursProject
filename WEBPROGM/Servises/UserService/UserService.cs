using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class UserService : IUserService
{
    private readonly IMessageBroker _messageBroker;

    public UserService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public void RegisterUser(User user)
    {
        user.password = HashPassword(user.password);
        _messageBroker.Publish("RegisterUser", JsonConvert.SerializeObject(user));
    }

    public async Task<User> Authenticate(string login, string password)
    {
        var tcs = new TaskCompletionSource<User>();

        _messageBroker.Subscribe($"AuthenticateUserResponse_{login}", message =>
        {
            
            {
                var response = JsonConvert.DeserializeObject<dynamic>(message);
                if (response != null)
                {
                    var user = JsonConvert.DeserializeObject<User>(response.User.ToString());
                    tcs.SetResult(user);
                }
                else
                {
                    tcs.SetResult(null!);
                }
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
            var productIds = JsonConvert.DeserializeObject<int[]>(message)!;
            tcs.SetResult(productIds);
        });

        _messageBroker.Publish("GetCartIds", JsonConvert.SerializeObject(userId));


        return await tcs.Task;
    }

    public async Task<Product[]> GetCart(int userId)
    {
        var tcs = new TaskCompletionSource<Product[]>();
        _messageBroker.Subscribe($"GetCartResponse_{userId}", message =>
        {
            
            var products = JsonConvert.DeserializeObject<Product[]>(message)!;
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

    public string GenerateJwtToken(User user)
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