using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Grocery.DMO;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grocery.Context
{
    public class GroceryRepository
    {
        private readonly IMongoCollection<GroceryProduct>? _GroceryProducts;

        private static MongoClient CreateClient(string connStr)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connStr);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
                settings.ConnectTimeout = TimeSpan.FromSeconds(2);
                settings.SocketTimeout = TimeSpan.FromSeconds(2);
                settings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };
                return new MongoClient(settings);
            }
            catch
            {
                return new MongoClient(connStr);
            }
        }

        public GroceryRepository(string connectionString, string databaseName)
        {
            try
            {
                var client = CreateClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _GroceryProducts = database.GetCollection<GroceryProduct>("GroceryProducts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GROCERY REPO INIT WARNING] {ex.Message}");
            }
        }

        public async Task<List<GroceryProduct>> GetAllProductsAsync()
        {
            if (_GroceryProducts == null) return new List<GroceryProduct>();
            try
            {
                using var cts = new CancellationTokenSource(1500);
                return await _GroceryProducts.Find(_ => true).ToListAsync(cts.Token);
            }
            catch
            {
                return new List<GroceryProduct>();
            }
        }

        public async Task<GroceryProduct?> GetProductAsync(string id)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                if (_GroceryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        return await _GroceryProducts.Find(x => x.Id == oid).FirstOrDefaultAsync(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GROCERY REPO GET ERR] {ex.Message}");
                    }
                }
            }
            return null;
        }

        public async Task AddProductAsync(GroceryProduct product)
        {
            if (_GroceryProducts != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(1500);
                    await _GroceryProducts.InsertOneAsync(product, cancellationToken: cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GROCERY REPO ADD ERR] {ex.Message}");
                }
            }
        }

        public async Task UpdateProductAsync(string id, GroceryProduct product)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                product.Id = oid;
                if (_GroceryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        await _GroceryProducts.ReplaceOneAsync(p => p.Id == oid, product, cancellationToken: cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GROCERY REPO UPDATE ERR] {ex.Message}");
                    }
                }
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            if (ObjectId.TryParse(id, out var oid))
            {
                if (_GroceryProducts != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(1500);
                        await _GroceryProducts.DeleteOneAsync(p => p.Id == oid, cancellationToken: cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GROCERY REPO DELETE ERR] {ex.Message}");
                    }
                }
            }
        }
    }
}
