using Microsoft.Extensions.Options;
using MongoDB.Driver;
using war.models;

namespace war.Store;

public interface IMongoContext
{
    IMongoCollection<Player> Players { get; }
    IMongoCollection<Match> Matches { get; }
}

public class MongoContext: IMongoContext
{
    public IMongoCollection<Player> Players { get; }
    public IMongoCollection<Match> Matches { get; }

    public MongoContext(IOptions<DbSettings> dbSettings)
    {
        var client = new MongoClient(dbSettings.Value.ConnectionString);
        var db = client.GetDatabase(dbSettings.Value.Db);
        Matches = db.GetCollection<Match>("match");
        Players = db.GetCollection<Player>("player");
    }
}