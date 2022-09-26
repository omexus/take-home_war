using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace war.models;

public class Player
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public int Wins { get; set; }
}