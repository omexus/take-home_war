using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace war.models;

public class Match
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime CreatedTime { get; set; }
    public PlayerMatch PlayerOne { get; set; }
    public PlayerMatch? PlayerTwo { get; set; }
    public string Winner { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int CardsToPlay { get; set; } = 1;
    public List<int>? CardsOnPile { get; set; }
}

public class PlayerMatch
{
    public string? PlayerId { get; set; }
    public List<int>? Cards { get; set; }
    public int CurrentCard { get; set; }
}