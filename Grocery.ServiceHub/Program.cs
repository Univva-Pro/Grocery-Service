using Grocery.Context;
using Grocery.DTO;
using Grocery.DMO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// DB Config
var connectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27018";
var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "GroceryDB";

Console.WriteLine("====================================================");
Console.WriteLine($"[STARTUP] Using MongoDB Connection: {connectionString}");
Console.WriteLine($"[STARTUP] Using Database Name: {databaseName}");
Console.WriteLine("====================================================");

try 
{
    builder.Services.AddSingleton(new GroceryRepository(connectionString, databaseName));
    builder.Services.AddSingleton(new UserRepository(connectionString, databaseName));
    Console.WriteLine("[STARTUP] Successfully connected to MongoDB and seeded collections!");
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL ERROR] Could not connect to MongoDB. Is your IP whitelisted in Atlas? Error: {ex.Message}");
}

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecretKeyForJwtAuthenticationWhichNeedsToBeLongEnough";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Grocery.ServiceHub",
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

// Login Endpoint
app.MapPost("/api/auth/login", async (LoginRequest request, UserRepository userRepo) =>
{
    var user = await userRepo.GetUserAsync(request.Username, request.Password);
    if (user == null) return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"] ?? "Grocery.ServiceHub",
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new AuthResponse { Token = tokenHandler.WriteToken(token), Role = user.Role, Username = user.Username });
});

// Get Products (Accessible by both Admin and User, but returns different fields)
app.MapGet("/api/Grocery/products", async (GroceryRepository repository, ClaimsPrincipal user) =>
{
    var products = await repository.GetAllProductsAsync();
    bool isAdmin = user.IsInRole("Admin");

    if (isAdmin)
    {
        var response = products.Select(p => new GroceryProductAdminResponse
        {
            ProductId = p.Id.ToString(),
            Name = p.Name ?? "Unknown Product",
            StockQuantity = p.StockQuantity,
            Price = p.Price,
            IsFresh = (DateTime.UtcNow - p.PasteurizationDate).TotalDays <= 14
        }).ToList();
        return Results.Ok(response);
    }
    else
    {
        var response = products.Select(p => new GroceryProductResponse
        {
            ProductId = p.Id.ToString(),
            Name = p.Name ?? "Unknown Product",
            Quantity = p.StockQuantity,
            Price = p.Price
        }).ToList();
        return Results.Ok(response);
    }
}).RequireAuthorization();

// Add Product (Admin Only)
app.MapPost("/api/Grocery/products", async (GroceryProductRequest request, GroceryRepository repository) =>
{
    var product = new GroceryProduct
    {
        Name = request.Name,
        StockQuantity = request.StockQuantity,
        Price = request.Price,
        PasteurizationDate = DateTime.UtcNow
    };
    await repository.AddProductAsync(product);
    var response = new GroceryProductAdminResponse
    {
        ProductId = product.Id.ToString(),
        Name = product.Name,
        StockQuantity = product.StockQuantity,
        Price = product.Price,
        IsFresh = true
    };
    return Results.Ok(response);
}).RequireAuthorization("AdminOnly");

// Update Product (Admin Only)
app.MapPut("/api/Grocery/products/{id}", async (string id, GroceryProductRequest request, GroceryRepository repository) =>
{
    var existing = await repository.GetProductAsync(id);
    if (existing == null) return Results.NotFound();

    existing.Name = request.Name;
    existing.StockQuantity = request.StockQuantity;
    existing.Price = request.Price;

    await repository.UpdateProductAsync(id, existing);
    return Results.Ok(new { message = "Product updated successfully" });
}).RequireAuthorization("AdminOnly");

// Delete Product (Admin Only)
app.MapDelete("/api/Grocery/products/{id}", async (string id, GroceryRepository repository) =>
{
    var existing = await repository.GetProductAsync(id);
    if (existing == null) return Results.NotFound();

    await repository.DeleteProductAsync(id);
    return Results.Ok(new { message = "Product deleted successfully" });
}).RequireAuthorization("AdminOnly");

app.Run();
