using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;
[BsonIgnoreExtraElements]
public class WithdrawRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("user_id")]
    public string UserId { get; set; } = null!;
    [BsonElement("amount")]
    public decimal Amount { get; set; }
    [BsonElement("currency")]
    public string Currency { get; set; } = "TON";
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [BsonElement("processed_at")]
    public DateTime? ProcessedAt { get; set; }
    [BsonElement("tx_hash")]
    public string? TxHash { get; set; }
    [BsonElement("status")]
    public string Status { get; set; } = WithdrawRequestStatus.Pending.ToString();
    [BsonElement("note")]
    public string? Note { get; set; }
}