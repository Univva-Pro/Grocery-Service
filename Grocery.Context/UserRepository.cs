using System.Threading.Tasks;
using Grocery.DMO;
using MongoDB.Driver;

namespace Grocery.Context
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _users = database.GetCollection<User>("users");
            SeedUsersIfEmpty();
        }

        private void SeedUsersIfEmpty()
        {
            if (_users.CountDocuments(_ => true) == 0)
            {
                // Simple plain-text hashing for demonstration 
                _users.InsertMany(new[]
                {
                    new User { Username = "admin", PasswordHash = "admin@05", Role = "Admin" },
                    new User { Username = "user", PasswordHash = "user123", Role = "User" }
                });
            }
        }

        public async Task<User?> GetUserAsync(string username, string passwordHash)
        {
            return await _users.Find(u => u.Username == username && u.PasswordHash == passwordHash).FirstOrDefaultAsync();
        }
    }
}
