using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;

[BsonIgnoreExtraElements]
public class InGameTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("user_id")]
    public string? UserId { get; set; }
    [BsonElement("amount")]
    public long Amount { get; set; }
    [BsonElement("fee")]
    public long Fee { get; set; }
    [BsonElement("currency")] public string Currency { get; set; } = "POINT";
    [BsonElement("status")] public string Status { get; set; } = "Success";
    [BsonElement("tran_type")]
    public string? TransactionType { get; set; }
    [BsonElement("description")]
    public string? Description { get; set; }
    [BsonElement("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}