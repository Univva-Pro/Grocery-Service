using Grocery.Context;
using Grocery.DTO;
using Grocery.DMO;
using Common.Library.Models;
using Common.Library.DTOs;
using Common.Library.Data;
using Common.Library.Extensions;
using Common.Library.Services;
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

builder.Services.AddSingleton<GroceryRepository>(sp => new GroceryRepository(connectionString, databaseName));
builder.Services.AddSingleton<UserRepository>(sp => new UserRepository(connectionString, databaseName));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAVerySecretKeyForJwtAuthenticationWhichNeedsToBeLongEnough";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddCommonJwtAuthentication(builder.Configuration);

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

// Get Products (Accessible by both Admin and User, returning role-based fields)
app.MapGet("/api/Grocery/products", async (GroceryRepository repository, HttpContext httpContext) =>
{
    var products = await repository.GetAllProductsAsync();
    bool isAdmin = httpContext.User.IsInRole("Admin") ||
                   httpContext.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type.ToLower() == "role") &&
                                                      c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

    if (!isAdmin)
    {
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role"));
                if (roleClaim != null && string.Equals(roleClaim.Value, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAdmin = true;
                }
            }
            catch { }
        }
    }

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
}).AllowAnonymous();

// Add Product (Admin Only)
app.MapPost("/api/Grocery/products", async (GroceryProductRequest request, GroceryRepository repository) =>
{
    var existingProducts = await repository.GetAllProductsAsync();
    var existing = existingProducts.FirstOrDefault(p => !string.IsNullOrEmpty(p.Name) && p.Name.Trim().Equals(request.Name?.Trim(), StringComparison.OrdinalIgnoreCase));

    if (existing != null)
    {
        existing.StockQuantity = request.StockQuantity;
        existing.Price = request.Price;
        await repository.UpdateProductAsync(existing.Id.ToString(), existing);

        var commonUrl = builder.Configuration["ServiceUrls:CommonService"];
        _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
        {
            OriginalId = existing.Id.ToString(),
            Name = existing.Name,
            Category = "Grocery",
            Price = (decimal)existing.Price,
            StockQuantity = existing.StockQuantity,
            SourceService = "Grocery",
            ActionType = "Update"
        }, commonUrl);

        var updateResponse = new GroceryProductAdminResponse
        {
            ProductId = existing.Id.ToString(),
            Name = existing.Name,
            StockQuantity = existing.StockQuantity,
            Price = existing.Price,
            IsFresh = true
        };
        return Results.Ok(updateResponse);
    }

    var product = new GroceryProduct
    {
        Name = request.Name?.Trim() ?? "",
        StockQuantity = request.StockQuantity,
        Price = request.Price,
        PasteurizationDate = DateTime.UtcNow
    };
    await repository.AddProductAsync(product);

    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];

    // Live Sync to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = product.Id.ToString(),
        Name = product.Name,
        Category = "Grocery",
        Price = (decimal)product.Price,
        StockQuantity = product.StockQuantity,
        SourceService = "Grocery",
        ActionType = "Add"
    }, commonServiceUrl);

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
    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];
    var existing = await repository.GetProductAsync(id);
    if (existing == null)
    {
        existing = new GroceryProduct
        {
            Name = request.Name,
            StockQuantity = request.StockQuantity,
            Price = request.Price,
            PasteurizationDate = DateTime.UtcNow
        };
        await repository.AddProductAsync(existing);
    }
    else
    {
        existing.Name = request.Name;
        existing.StockQuantity = request.StockQuantity;
        existing.Price = request.Price;
        await repository.UpdateProductAsync(id, existing);
    }

    // Live Sync Update to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = id,
        Name = existing.Name,
        Category = "Grocery",
        Price = (decimal)existing.Price,
        StockQuantity = existing.StockQuantity,
        SourceService = "Grocery",
        ActionType = "Update"
    }, commonServiceUrl);

    return Results.Ok(new { message = "Product updated successfully", product = existing });
}).RequireAuthorization("AdminOnly");

// Delete Product (Admin Only)
app.MapDelete("/api/Grocery/products/{id}", async (string id, GroceryRepository repository) =>
{
    var commonServiceUrl = builder.Configuration["ServiceUrls:CommonService"];
    var existing = await repository.GetProductAsync(id);
    if (existing == null) return Results.NotFound();

    await repository.DeleteProductAsync(id);

    // Live Sync Delete to Common-Service Master Inventory
    _ = ProductSyncClient.SyncProductToCommonAsync(new ProductSyncPayload
    {
        OriginalId = id,
        Name = existing.Name,
        Category = "Grocery",
        SourceService = "Grocery",
        ActionType = "Delete"
    }, commonServiceUrl);

    return Results.Ok(new { message = "Product deleted successfully" });
}).RequireAuthorization("AdminOnly");

app.MapFallbackToFile("index.html");

app.Run();
