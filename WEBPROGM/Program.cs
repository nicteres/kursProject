using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddHostedService<DatabaseHandler>();
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddSingleton<IMessageBroker, SimpleMessageBroker>();
builder.Services.AddTransient<IRecommendationService, RecommendationService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VR Shop API", Version = "v1" });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
      builder =>
      {
          builder.WithOrigins("http://localhost:5173")
                 .AllowAnyHeader()
                 .AllowAnyMethod();
      });
});
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VR Shop API V1");
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();
app.UseCors("AllowSpecificOrigin");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();