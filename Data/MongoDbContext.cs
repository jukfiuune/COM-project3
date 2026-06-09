using MongoDB.Driver;
using Data.Entities;

namespace Data;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        EnsureIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Data.Entities.RefreshToken> RefreshTokens => _database.GetCollection<Data.Entities.RefreshToken>("refreshTokens");

    private void EnsureIndexes()
    {
        Users.Indexes.CreateOne(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));

        Users.Indexes.CreateOne(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true }));

        RefreshTokens.Indexes.CreateOne(new CreateIndexModel<Data.Entities.RefreshToken>(Builders<Data.Entities.RefreshToken>.IndexKeys.Ascending(rt => rt.TokenHash),
            new CreateIndexOptions { Unique = true }));
    }
}
