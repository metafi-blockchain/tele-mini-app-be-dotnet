using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OkCoin.API.Models;
[BsonIgnoreExtraElements]
public class SettingItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal? Price { get; set; } = 0.0m;
    public string? Currency { get; set; } = "OKCOIN";
    public decimal? Value { get; set; } = 0.0m;
    public int? Level { get; set; } = 1;
    public decimal? Reward { get; set; } = 0.0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public SettingItemCategory Category { get; set; } = SettingItemCategory.General;
}

public enum SettingItemCategory
{
    General,
    BoostEnergy,
    BoostTap,
    BoostSpeed,
    Referral,
    Level
}