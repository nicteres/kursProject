using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public interface IUserService
{

    void RegisterUser(User user);
    Task<User> Authenticate(string login, string password);


    Task<int[]> GetCartIds(int userId);
    Task<Product[]> GetCart(int userId);
    void AddToCart(int userId, int productId);
    void ClearCart(int userId);
}
