using System.Collections.Generic;
using System.Threading.Tasks;
using Grocery.DMO;
using MongoDB.Driver;

namespace Grocery.Context
{
    public class GroceryRepository
    {
        private readonly IMongoCollection<GroceryProduct> _GroceryProducts;

        public GroceryRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _GroceryProducts = database.GetCollection<GroceryProduct>("GroceryProducts");
            SeedProductsIfEmpty();
        }

        private void SeedProductsIfEmpty()
        {
            if (_GroceryProducts.CountDocuments(_ => true) == 0)
            {
                _GroceryProducts.InsertMany(new[]
                {
                    new GroceryProduct { Name = "Whole Milk", Price = 4.99, StockQuantity = 100, PasteurizationDate = System.DateTime.UtcNow.AddDays(-2) },
                    new GroceryProduct { Name = "Low Fat Milk", Price = 4.49, StockQuantity = 50, PasteurizationDate = System.DateTime.UtcNow.AddDays(-5) },
                    new GroceryProduct { Name = "Skim Milk", Price = 3.99, StockQuantity = 30, PasteurizationDate = System.DateTime.UtcNow.AddDays(-20) }
                });
            }
        }

        public async Task<List<GroceryProduct>> GetAllProductsAsync()
        {
            return await _GroceryProducts.Find(_ => true).ToListAsync();
        }

        public async Task<GroceryProduct> GetProductAsync(string id)
        {
            return await _GroceryProducts.Find(p => p.Id == MongoDB.Bson.ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public async Task AddProductAsync(GroceryProduct product)
        {
            await _GroceryProducts.InsertOneAsync(product);
        }

        public async Task UpdateProductAsync(string id, GroceryProduct product)
        {
            product.Id = MongoDB.Bson.ObjectId.Parse(id);
            await _GroceryProducts.ReplaceOneAsync(p => p.Id == product.Id, product);
        }

        public async Task DeleteProductAsync(string id)
        {
            await _GroceryProducts.DeleteOneAsync(p => p.Id == MongoDB.Bson.ObjectId.Parse(id));
        }
    }
}
