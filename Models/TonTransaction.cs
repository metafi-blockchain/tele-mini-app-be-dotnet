using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;

[BsonIgnoreExtraElements]
public class TonTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("tran_hash")]
    public string TransactionHash { get; set; }
    [BsonElement("from_address")]
    public string? FromAddress { get; set; }
    [BsonElement("to_address")]
    public string? ToAddress { get; set; }
    [BsonElement("amount")]
    public long Amount { get; set; }
    [BsonElement("fee")]
    public long Fee { get; set; }
    [BsonElement("currency")]
    public string Currency { get; set; }
    [BsonElement("status")]
    public string Status { get; set; }
    [BsonElement("tran_type")]
    public string TransactionType { get; set; }
    [BsonElement("text")]
    public string BodyText { get; set; }

    [BsonElement("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("is_processed")]
    public bool IsProcessed { get; set; } = true;
}