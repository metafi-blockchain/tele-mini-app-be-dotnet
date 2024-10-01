using OkCoin.API.Models;

namespace OkCoin.API.ViewModels;

public class MyTaskViewModel
{
    //public string Id { get; set; } = null!;
    public string TaskId { get; set; } = null!;
    //public string UserId { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Reward { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public bool IsClaimed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public long? TaskValue { get; set; }
    public long UserValue { get; set; }
    public TaskCategory Category { get; set; }
    public string? SubCategory { get; set; }
    public int Order { get; set; } = 0;
}