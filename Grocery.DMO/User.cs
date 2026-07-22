using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Grocery.DMO
{
    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // "Admin" or "User"
    }
}
