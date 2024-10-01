using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;
[BsonIgnoreExtraElements]
public class TaskItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("category")]
    public TaskCategory Category { get; set; }
    [BsonElement("sub_category")]
    public SubCategory? SubCategory { get; set; }
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("reward")]
    public decimal Reward { get; set; } = 0;
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;
    [BsonElement("img_url")]
    public string? ImageUrl { get; set; }
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;
    [BsonElement("value")]
    public long? Value { get; set; }
    [BsonElement("order")] public int Order { get; set; } = 0;
}

public enum TaskCategory
{
    Video,
    Social,
    Ranking,
    Referral,
}

public enum SubCategory
{
    X,
    Y,
    T
}