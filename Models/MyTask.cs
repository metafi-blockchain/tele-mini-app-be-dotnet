using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;

[BsonIgnoreExtraElements]
public class MyTask
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}