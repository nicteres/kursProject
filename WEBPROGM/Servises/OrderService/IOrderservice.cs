using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public interface IOrderService
{
    Task CreateOrder(int userId, int[] productIds);
    Task<IEnumerable<Order>> GetOrdersByUserId(int userId);
    Task UpdateOrderStatus(int orderId, order_status status);
}