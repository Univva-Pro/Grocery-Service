using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Grocery.DMO
{
    [BsonIgnoreExtraElements]
    public class GroceryProduct
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public double Price { get; set; }
        public DateTime PasteurizationDate { get; set; }
    }
}
