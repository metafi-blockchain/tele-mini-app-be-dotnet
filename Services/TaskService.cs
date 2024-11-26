using System.Globalization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OkCoin.API.Models;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ITaskService
{
    ResponseDto<List<MyTaskViewModel>> MyTasks(string userId);
    void CreateTasks();
    Task<ResponseDto<bool>> CreateTaskItem(TaskItem taskItem);
    Task<ResponseDto<bool>> UpdateTaskItem(TaskItem taskItem);
    Task<ResponseDto<bool>> DeleteTaskItem(string id);
    Task<ResponseDto<string>> CompleteTask(string userId, string? taskId, string? code);
    Task CheckUserDiligenceLoginAsync(string userId, int numberConsecutiveLogins);
    Task CheckNumberOfUpdateForApp(string userId, string namePackage, int numberOfUpdate);
}
public class TaskService : ITaskService
{
    private readonly IMongoCollection<TaskItem> _taskCollection;
    private readonly IMongoCollection<MyTask> _myTaskCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly JwtSettings _jwtSettings;
    private readonly IStatisticService _statisticService;

    public TaskService(IOptions<DbSettings> myDatabaseSettings, IOptions<JwtSettings> jwtSettings, IStatisticService statisticService)
    {
        _statisticService = statisticService;
        _jwtSettings = jwtSettings.Value;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _taskCollection = database.GetCollection<TaskItem>(nameof(TaskItem));
        _myTaskCollection = database.GetCollection<MyTask>(nameof(Models.MyTask));
        _userCollection = database.GetCollection<User>(nameof(User));
    }
    public ResponseDto<List<MyTaskViewModel>> MyTasks(string userId)
    {
        var tasks = _taskCollection.Find(x=>x.IsActive == true).ToList();
        var myTasks = _myTaskCollection.Find(x => x.UserId == userId).ToList();
        var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
        var result = new List<MyTaskViewModel>();
        foreach (var task in tasks)
        {
            var myTask = myTasks.Find(x => x.TaskId == task.Id);
            result.Add(new MyTaskViewModel()
            {
                TaskId = task.Id ?? string.Empty,
                Title = task.Title,
                Description = task.Description,
                Reward = task.Reward,
                IsClaimed = myTask != null && myTask.IsClaim,
                IsCompleted = task.Category switch
                {
                    TaskCategory.Farming => task.SubCategory == SubCategory.Farm ? user.GrandBalance >= task.Value : myTask != null,
                    TaskCategory.Referral => user.RefererCount >= task.Value,
                    _ => myTask != null
                },
                CreatedAt = task.CreatedAt,
                CompletedAt = myTask?.CreatedAt,
                ClaimedAt = myTask?.CreatedAt,
                Url = task.Url,
                ImageUrl = task.ImageUrl,
                Category = task.Category,
                SubCategory = task.SubCategory.ToString(),
                TaskValue = task.Value,
                UserValue = task.Category switch
                {
                    TaskCategory.Farming => (long) user.GrandBalance,
                    TaskCategory.Referral => user.RefererCount,
                    _ => 0
                },
                Order = task.Order
            });
        }
        return new ResponseDto<List<MyTaskViewModel>>()
        {
            Success = true,
            Message = string.Empty,
            Data = result.OrderByDescending(x => x.Order).ToList()
        };
    }

    public void CreateTasks()
    {
        var indexKeys = Builders<User>.IndexKeys.Ascending(m => m.TelegramId);
        var indexModel = new CreateIndexModel<User>(indexKeys);
        _userCollection.Indexes.CreateOne(indexModel);
        
        var userMyTasksIndexKeys = Builders<MyTask>.IndexKeys.Ascending(m => m.UserId);
        var userMyTasksIndexModel = new CreateIndexModel<MyTask>(userMyTasksIndexKeys);
        _myTaskCollection.Indexes.CreateOne(userMyTasksIndexModel);
        
        var lst = new List<TaskItem>()
        {
            new TaskItem()
            { 
                Category = TaskCategory.Social, 
                Title = "Join Verse_Eternal_Kingdoms in Discord",
                Description = "Join Verse_Eternal_Kingdoms in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/6JGpeHut",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post Verse_Eternal_Kingdoms in Discord",
                Description = "React Latest Post Verse_Eternal_Kingdoms in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/6JGpeHut",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post Verse_Eternal_Kingdoms in Discord",
                Description = "Comment Latest Post Verse_Eternal_Kingdoms in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/6JGpeHut",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join metafi_network_general in Discord",
                Description = "Join metafi_network_general in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/kyeQYgXk",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post metafi_network_general in Discord",
                Description = "React Latest Post metafi_network_general in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/kyeQYgXk",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post metafi_network_general in Discord",
                Description = "Comment Latest Post metafi_network_general in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/kyeQYgXk",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join metafi_network_notify in Discord",
                Description = "Join metafi_network_notify in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/qvE7p4GN",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post metafi_network_notify in Discord",
                Description = "React Latest Post metafi_network_notify in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/qvE7p4GN",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post metafi_network_notify in Discord",
                Description = "Comment Latest Post metafi_network_notify in Discord",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://discord.gg/qvE7p4GN",
                ImageUrl = "https://minigame.metafi.gg/images/game/discord.webp",
                SubCategory = SubCategory.D, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join MetaFi_Network_Notify in Telegrams",
                Description = "Join MetaFi_Network_Notify in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Notify",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post MetaFi_Network_Notify in Telegrams",
                Description = "React Latest Post MetaFi_Network_Notify in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Notify",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post MetaFi_Network_Notify in Telegrams",
                Description = "Comment Latest Post MetaFi_Network_Notify in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Notify",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join MetaFi_Network_Official in Telegrams",
                Description = "Join MetaFi_Network_Official in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Official",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post MetaFi_Network_Official in Telegrams",
                Description = "React Latest Post MetaFi_Network_Official in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Official",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post MetaFi_Network_Official in Telegrams",
                Description = "Comment Latest Post MetaFi_Network_Official in Telegrams",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://t.me/MetaFi_Network_Official",
                ImageUrl = "https://minigame.metafi.gg/images/game/meta.webp",
                SubCategory = SubCategory.T, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join Fanpage Eternal Kingdoms",
                Description = "Join Fanpage Eternal Kingdoms",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.facebook.com/profile.php?id=61566437251830",
                ImageUrl = "https://scontent.fhan5-3.fna.fbcdn.net/v/t39.30808-1/461402274_1534157917985516_4478946268824527127_n.jpg?stp=c119.0.904.904a_dst-jpg_s480x480&_nc_cat=105&ccb=1-7&_nc_sid=f4b9fd&_nc_ohc=0foH3n7cYWkQ7kNvgEJsvkA&_nc_zt=24&_nc_ht=scontent.fhan5-3.fna&_nc_gid=Ad2GHIktFqgrowBR7iSi1LO&oh=00_AYD3_lVrfGkHhrLxvLfxbnMlO9lSIH6A2X3ZYY2JxCQOXQ&oe=67360A95",
                SubCategory = SubCategory.F, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post Fanpage Eternal Kingdoms",
                Description = "React Latest Post Fanpage Eternal Kingdoms",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.facebook.com/profile.php?id=61566437251830",
                ImageUrl = "https://scontent.fhan5-3.fna.fbcdn.net/v/t39.30808-1/461402274_1534157917985516_4478946268824527127_n.jpg?stp=c119.0.904.904a_dst-jpg_s480x480&_nc_cat=105&ccb=1-7&_nc_sid=f4b9fd&_nc_ohc=0foH3n7cYWkQ7kNvgEJsvkA&_nc_zt=24&_nc_ht=scontent.fhan5-3.fna&_nc_gid=Ad2GHIktFqgrowBR7iSi1LO&oh=00_AYD3_lVrfGkHhrLxvLfxbnMlO9lSIH6A2X3ZYY2JxCQOXQ&oe=67360A95",
                SubCategory = SubCategory.F, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post Fanpage Eternal Kingdoms",
                Description = "Comment Latest Post Fanpage Eternal Kingdoms",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.facebook.com/profile.php?id=61566437251830",
                ImageUrl = "https://scontent.fhan5-3.fna.fbcdn.net/v/t39.30808-1/461402274_1534157917985516_4478946268824527127_n.jpg?stp=c119.0.904.904a_dst-jpg_s480x480&_nc_cat=105&ccb=1-7&_nc_sid=f4b9fd&_nc_ohc=0foH3n7cYWkQ7kNvgEJsvkA&_nc_zt=24&_nc_ht=scontent.fhan5-3.fna&_nc_gid=Ad2GHIktFqgrowBR7iSi1LO&oh=00_AYD3_lVrfGkHhrLxvLfxbnMlO9lSIH6A2X3ZYY2JxCQOXQ&oe=67360A95",
                SubCategory = SubCategory.F, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Subcribe Fanpage Eternal Kingdoms",
                Description = "Subcribe Fanpage Eternal Kingdoms",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.facebook.com/profile.php?id=61566437251830",
                ImageUrl = "https://scontent.fhan5-3.fna.fbcdn.net/v/t39.30808-1/461402274_1534157917985516_4478946268824527127_n.jpg?stp=c119.0.904.904a_dst-jpg_s480x480&_nc_cat=105&ccb=1-7&_nc_sid=f4b9fd&_nc_ohc=0foH3n7cYWkQ7kNvgEJsvkA&_nc_zt=24&_nc_ht=scontent.fhan5-3.fna&_nc_gid=Ad2GHIktFqgrowBR7iSi1LO&oh=00_AYD3_lVrfGkHhrLxvLfxbnMlO9lSIH6A2X3ZYY2JxCQOXQ&oe=67360A95",
                SubCategory = SubCategory.F, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Xem Video giới thiệu sản phẩm",
                Description = "Xem Video giới thiệu sản phẩm",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=5zqTpcIePoc",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Giới thiệu Eternal Kingdoms Game",
                Description = "Giới thiệu Eternal Kingdoms Game",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=5zqTpcIePoc",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu Multiverse hợp tác Gameloft triển khai ở Viettel, MobiFone",
                Description = "Video giới thiệu Multiverse hợp tác Gameloft triển khai ở Viettel, MobiFone",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=FtixEhICV34",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu Open Multiverse Hub tác với Gameloft và các nhà mạng Viettel, MobiFone",
                Description = "Video giới thiệu Open Multiverse Hub tác với Gameloft và các nhà mạng Viettel, MobiFone",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=FtixEhICV34",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu MetaFi - Go - Park",
                Description = "Video giới thiệu MetaFi - Go - Park",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=gUIEDkxC424",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Giới thiệu Infinity War Game",
                Description = "Giới thiệu Infinity War Game",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=kNIDk5dQsJU&t=2s",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu MetaFi",
                Description = "Video giới thiệu MetaFi",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=lo0CRy9gmTo",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu Web3 Server Platform for building social and real-time multiplayer games and apps.",
                Description = "Video giới thiệu Web3 Server Platform for building social and real-time multiplayer games and apps.",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=lo0CRy9gmTo",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu ứng dụng Asset Tokenization Platform vào Số hóa Sâm Ngọc Linh",
                Description = "Video giới thiệu ứng dụng Asset Tokenization Platform vào Số hóa Sâm Ngọc Linh",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=LpM55Ky_ebc",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu MetaFi - Go - City",
                Description = "Video giới thiệu MetaFi - Go - City",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=rYS6YJdIQfA",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video hướng dẫn Mua bán NFT trên Marketplace với mạng AVAX",
                Description = "Video hướng dẫn Mua bán NFT trên Marketplace với mạng AVAX",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=xpabDO6bOT0",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu MetaFi - Go - Concert",
                Description = "Video giới thiệu MetaFi - Go - Concert",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=XZF1AfNHt4o",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu MetaFi - Go - Ginseng Farm",
                Description = "Video giới thiệu MetaFi - Go - Ginseng Farm",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=yvhCzkCOWeY",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Video giới thiệu một ứng dụng nhỏ sử dụng AI Tools, AI-Driven Digital Content Generation",
                Description = "Video giới thiệu một ứng dụng nhỏ sử dụng AI Tools, AI-Driven Digital Content Generation",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://www.youtube.com/watch?v=YZ84LAKWG0g",
                ImageUrl = "https://minigame.metafi.gg/images/game/youtube.webp",
                SubCategory = SubCategory.Y, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Join MetaFi_Network_Official in X",
                Description = "Join MetaFi_Network_Official in X",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://x.com/MetaFi_Network",
                ImageUrl = "https://minigame.metafi.gg/images/game/x.webp",
                SubCategory = SubCategory.X, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "React Latest Post MetaFi_Network_Official in X",
                Description = "React Latest Post MetaFi_Network_Official in X",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://x.com/MetaFi_Network",
                ImageUrl = "https://minigame.metafi.gg/images/game/x.webp",
                SubCategory = SubCategory.X, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Comment Latest Post MetaFi_Network_Official in X",
                Description = "Comment Latest Post MetaFi_Network_Official in X",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://x.com/MetaFi_Network",
                ImageUrl = "https://minigame.metafi.gg/images/game/x.webp",
                SubCategory = SubCategory.X, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Follow MetaFi_Network_Official in X",
                Description = "Follow MetaFi_Network_Official in X",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://x.com/MetaFi_Network",
                ImageUrl = "https://minigame.metafi.gg/images/game/x.webp",
                SubCategory = SubCategory.X, 
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social, 
                Title = "Subcribe MetaFi_Network_Official in X",
                Description = "Subcribe MetaFi_Network_Official in X",
                Reward = 1000m, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Url = "https://x.com/MetaFi_Network",
                ImageUrl = "https://minigame.metafi.gg/images/game/x.webp",
                SubCategory = SubCategory.X, 
                Order = 1
            },
        };
        
        // task friends
        lst.AddRange(
            new List<TaskItem>(){
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 1 friend",
                    Description = "Invite 1 friend and earn rewards",
                    Reward = 10000m,
                    IsActive = true,
                    Value = 1,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 5 friends",
                    Description = "Invite 5 friends and earn rewards",
                    Reward = 50000m,
                    IsActive = true,
                    Value = 5,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 10 friends",
                    Description = "Invite 10 friends and earn rewards",
                    Reward = 100000m,
                    IsActive = true,
                    Value = 10,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 20 friends",
                    Description = "Invite 20 friends and earn rewards",
                    Reward = 200000m,
                    IsActive = true,
                    Value = 20,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 50 friends",
                    Description = "Invite 50 friends and earn rewards",
                    Reward = 500000m,
                    IsActive = true,
                    Value = 50,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 100 friends",
                    Description = "Invite 100 friends and earn rewards",
                    Reward = 1000000m,
                    IsActive = true,
                    Value = 100,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 200 friends",
                    Description = "Invite 200 friends and earn rewards",
                    Reward = 2000000m,
                    IsActive = true,
                    Value = 200,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 500 friends",
                    Description = "Invite 500 friends and earn rewards",
                    Reward = 5000000m,
                    IsActive = true,
                    Value = 500,
                },
                new TaskItem()
                {
                    Category = TaskCategory.Referral,
                    Title = "Invite 1000 friends",
                    Description = "Invite 1000 friends and earn rewards",
                    Reward = 10000000m,
                    IsActive = true,
                    Value = 1000,
                },
            }
        );

        // farming
        lst.AddRange(new List<TaskItem>{
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Farm: 1,000 Points",
                Description = "Farm: 1,000 Points and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Value = 1000,
                SubCategory = SubCategory.Farm
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Farm: 5,000 Points",
                Description = "Farm: 5,000 Points and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Value = 5000,
                SubCategory = SubCategory.Farm
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Farm: 10,000 Points",
                Description = "Farm: 10,000 Points and earn rewards",
                Reward = 2000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Value = 10000,
                SubCategory = SubCategory.Farm
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Farm: 20,000 Points",
                Description = "Farm: 20,000 Points and earn rewards",
                Reward = 2000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                Value = 20000,
                SubCategory = SubCategory.Farm
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Farm: 50,000 Points",
                Description = "Farm: 50,000 Points and earn rewards",
                Reward = 5000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 50000,
                SubCategory = SubCategory.Farm 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Login continuously for 3 days",
                Description = "Login continuously for 3 days and earn rewards",
                Reward = 500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 3,
                SubCategory = SubCategory.Diligence 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Login continuously for 5 days",
                Description = "Login continuously for 5 days and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 5,
                SubCategory = SubCategory.Diligence 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Login continuously for 7 days",
                Description = "Login continuously for 7 days and earn rewards",
                Reward = 3000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 7,
                SubCategory = SubCategory.Diligence 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Login continuously for 14 days",
                Description = "Login continuously for 14 days and earn rewards",
                Reward = 5000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 14,
                SubCategory = SubCategory.Diligence
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Play game MetaFi: Eternal Kingdom",
                Description = "Play game MetaFi: Eternal Kingdom and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Play 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Play game MetaFi: Infinity War",
                Description = "Play game MetaFi: Infinity War and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Play 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Play game MetaFi: Angle Land",
                Description = "Play game MetaFi: Angle Land and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Play 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Play game MetaFi: Space Alpha",
                Description = "Play game MetaFi: Space Alpha and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow, 
                SubCategory = SubCategory.Play
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Play game MetaFi: My Master War",
                Description = "Play game MetaFi: My Master War and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Play 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 2 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 2 times and earn rewards",
                Reward = 2000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 2,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 4 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 4 times and earn rewards",
                Reward = 3000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 4,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 6 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.RECHARGING_SPEED}: 6 times and earn rewards",
                Reward = 4000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 6,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Buy AI Bot: x1",
                Description = "Buy AI Bot: x1 and earn rewards",
                Reward = 5000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 1,
                SubCategory = SubCategory.Purchase 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 3 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 3 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 3,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 5 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 5 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 5,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 7 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.MULTI_TAP}: 7 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 7,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 3 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 3 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 3,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 5 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 5 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 5,
                SubCategory = SubCategory.Upgrade 
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 7 times",
                Description = $"Upgrade {Constants.NamePackageUpgrade.ENERGY_LIMIT}: 7 times and earn rewards",
                Reward = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                Value = 7,
                SubCategory = SubCategory.Upgrade  
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Vote 5 stars Store: Eternal Kingdom",
                Description = "Vote 5 stars Store: Eternal Kingdom and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Vote  
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Vote 5 stars Store: Infinity War",
                Description = "Vote 5 stars Store: Infinity War and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Vote  
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Vote 5 stars Store: Angle Land",
                Description = "Vote 5 stars Store: Angle Land and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Vote  
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Vote 5 stars Store: Space Alpha",
                Description = "Vote 5 stars Store: Space Alpha and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Vote  
            },
            new TaskItem()
            {
                Category = TaskCategory.Farming,
                Title = "Vote 5 stars Store: My Master War",
                Description = "Vote 5 stars Store: My Master War and earn rewards",
                Reward = 1500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                SubCategory = SubCategory.Vote  
            },
        });

        var existingTasks = _taskCollection.Find(x => true).Any();
        if (!existingTasks)
        {
            _taskCollection.InsertMany(lst);
        }
        else
        {
            foreach (var taskItem in lst)
            {
                var existingTask = _taskCollection.Find(x => x.Title == taskItem.Title && x.Category == taskItem.Category).FirstOrDefault();
                if (existingTask == null)
                {
                    _taskCollection.InsertOne(taskItem);
                }
                else
                {
                    existingTask.Description = taskItem.Description;
                    existingTask.Reward = taskItem.Reward;
                    existingTask.IsActive = taskItem.IsActive;
                    existingTask.UpdatedAt = DateTime.UtcNow;
                    existingTask.Url = taskItem.Url;
                    existingTask.Code = taskItem.Code;
                    existingTask.ImageUrl = taskItem.ImageUrl;
                    existingTask.SubCategory = taskItem.SubCategory;
                    existingTask.Category = taskItem.Category;
                    existingTask.Value = taskItem.Value;
                    existingTask.Order = taskItem.Order;
                    _taskCollection.ReplaceOne(x => x.Id == existingTask.Id, existingTask);
                }
            }
        }
    }

    public async Task<ResponseDto<bool>> CreateTaskItem(TaskItem taskItem)
    {
        try
        {
            if (string.IsNullOrEmpty(taskItem.Title))
            {
                return new ResponseDto<bool>
                {
                    Message = "Please input the title",
                };
            }    

            if (taskItem.Category == TaskCategory.Social && taskItem.SubCategory == null)
            {
                return new ResponseDto<bool>
                {
                    Message = "Please input the sub-category",
                };
            }

            await _taskCollection.InsertOneAsync(taskItem);

            return new ResponseDto<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"{nameof(CreateTaskItem)} Error: {ex.Message}");
            return new ResponseDto<bool> 
            {
            };
        }
    }

    public async Task<ResponseDto<bool>> UpdateTaskItem(TaskItem taskItem)
    {
        try
        {
            if (string.IsNullOrEmpty(taskItem.Title))
            {
                return new ResponseDto<bool>
                {
                    Message = "Please input the title",
                };
            }    

            if (taskItem.Category == TaskCategory.Social && taskItem.SubCategory == null)
            {
                return new ResponseDto<bool>
                {
                    Message = "Please input the sub-category",
                };
            }

            var task = await _taskCollection.Find(c => c.Id == taskItem.Id).FirstOrDefaultAsync();
            if (task is null)
            {
                return new ResponseDto<bool>
                {
                    Message = "The task is not found"
                };
            }

            task.Title = taskItem.Title;
            task.Category = taskItem.Category;
            task.Description = taskItem.Description;
            task.Reward = taskItem.Reward;
            task.IsActive = taskItem.IsActive;
            task.UpdatedAt = DateTime.UtcNow;
            task.Url = taskItem.Url;
            task.Code = taskItem.Code;
            task.ImageUrl = taskItem.ImageUrl;
            task.SubCategory = taskItem.SubCategory;
            task.Category = taskItem.Category;
            task.Value = taskItem.Value;
            task.Order = taskItem.Order;

            await _taskCollection.ReplaceOneAsync(c => c.Id == task.Id, task);

            return new ResponseDto<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (System.Exception ex)
        {
            
            Console.WriteLine($"{nameof(UpdateTaskItem)} Error: {ex.Message}");
            return new ResponseDto<bool> 
            {
            };
        }
    }

    public async Task<ResponseDto<bool>> DeleteTaskItem(string id)
    {
        try
        {
            var task = await _taskCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (task is null)
            {
                return new ResponseDto<bool>
                {
                    Message = "The task is not found"
                };
            }

            await _taskCollection.DeleteOneAsync(c => c.Id == id);

            return new ResponseDto<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"{nameof(UpdateTaskItem)} Error: {ex.Message}");
            return new ResponseDto<bool> 
            {
            };
        }
    }

    public async Task<ResponseDto<string>> CompleteTask(string userId, string? taskId, string? code)
    {
        if(string.IsNullOrEmpty(taskId))
        {
            return new ResponseDto<string>()
            {
                Success = false,
                Message = "Task not found.",
                Data = string.Empty
            };
        }
        var task = _taskCollection.Find(x => x.Id == taskId).FirstOrDefault();
        if(task == null)
        {
            return new ResponseDto<string>()
            {
                Success = false,
                Message = "Task not found.",
                Data = string.Empty
            };
        }
        var myTask = _myTaskCollection.Find(x => x.UserId == userId && x.TaskId == taskId).FirstOrDefault();
        if(myTask != null && myTask.IsClaim)
        {
            return new ResponseDto<string>()
            {
                Success = false,
                // Message = "Task already completed.",
                 Message = "Task already claimed.",
                Data = string.Empty
            };
        }
        
        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        if (task.Category == TaskCategory.Video && !string.IsNullOrEmpty(task.Code))
        {
            if (task.Code != code)
            {
                return new ResponseDto<string>()
                {
                    Success = false,
                    Message = "Code is incorrect.",
                    Data = string.Empty
                };
            }
        }
        else if (task.Category == TaskCategory.Social)
        {
            // check if user has shared the post
        }
        else if (task.Category == TaskCategory.Farming && task.SubCategory == SubCategory.Farm)
        {
            if (user.TapBalance < task.Value)
            {
                return new ResponseDto<string>()
                {
                    Success = false,
                    Message = "Claim is invalid.",
                    Data = string.Empty
                };
            }
        }
        else if (task.Category == TaskCategory.Referral)
        {
            if (user.RefererCount < task.Value)
            {
                return new ResponseDto<string>()
                {
                    Success = false,
                    Message = "Referrals is not enough.",
                    Data = string.Empty
                };
            }
        }
        
        if (myTask != null)
        {
            myTask.IsClaim = true;
            await _myTaskCollection.ReplaceOneAsync(myTask.Id, myTask);
        }
        else
        {
            myTask = new MyTask()
            {
                TaskId = taskId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _myTaskCollection.InsertOneAsync(myTask);
        }
        
        user.Balance += task.Reward;
        
        user.TournamentBalance += task.Reward;
        user.TournamentBalanceUpdatedAt = DateTime.UtcNow;

        user.GrandBalance += task.Reward;
        user.BalanceUpdatedAt = DateTime.UtcNow;
        await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
        try
        {
            await _statisticService.UpdateTotalSharedBalance((long)task.Reward);
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)task.Reward,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.TaskReward.ToString(),
                Description = "Reward for completing task " + task.Id + " - " + task.Title,
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
        return new ResponseDto<string>()
        {
            Success = true,
            Message = "Task completed.",
            Data = string.Empty
        };
    }

    public async Task CheckUserDiligenceLoginAsync(string userId, int numberConsecutiveLogins)
    {
        var taskItem = await _taskCollection.Find(c => c.Category == TaskCategory.Farming 
                                        && c.SubCategory == SubCategory.Diligence 
                                        && c.Value == numberConsecutiveLogins).FirstOrDefaultAsync();

        if (taskItem is null)
        {
            return;
        }

        await CompleteTask(userId, taskItem.Id, string.Empty);
    }

    public async Task CheckNumberOfUpdateForApp(string userId, string namePackage, int numberOfUpdate)
    {
        var taskItem = await _taskCollection.Find(c => c.Category == TaskCategory.Farming 
                                        && c.SubCategory == SubCategory.Upgrade 
                                        && c.Title.ToLower().Contains(namePackage.ToLower())
                                        && c.Value == numberOfUpdate).FirstOrDefaultAsync();

        if (taskItem is null)
        {
            return;
        }

        await CompleteTask(userId, taskItem.Id, string.Empty);
    }
}