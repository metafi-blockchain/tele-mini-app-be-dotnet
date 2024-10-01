using System.Globalization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OkCoin.API.Models;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ITaskService
{
    ResponseDto<List<MyTaskViewModel>> MyTasks(string userId);
    void CreateTasks();
    void CreateTaskItem(TaskItem taskItem);
    void UpdateTaskItem(string id, TaskItem taskItem);
    void DeleteTaskItem(string id);
    Task<ResponseDto<string>> CompleteTask(string userId, string? taskId, string? code);
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
                IsClaimed = myTask != null,
                IsCompleted = task.Category switch
                {
                    TaskCategory.Ranking => user.TapBalance >= task.Value,
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
                    TaskCategory.Ranking => (long) user.TapBalance,
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
        
        var twitters = new Dictionary<string, string>()
        {
            {"https://x.com/cristiano?s=21","Follow Cristiano Ronaldo on X"},
            {"https://x.com/leomessisite?s=21","Follow Lionel Messi on X"},
            {"https://x.com/kmbappe?s=21","Follow Kylian Mbappé on X"},
            {"https://x.com/bellinghamjude?s=21","Follow Jude Bellingham on X"},
            {"https://x.com/b_fernandes8?s=21","Follow Bruno Fernandes on X"},
            {"https://x.com/realmadrid?s=21","Follow Real Madrid C.F. on X"},
            {"https://x.com/fcbarcelona?s=21","Follow FC Barcelona on X"},
            {"https://x.com/manutd?s=21","Follow Manchester United on X"},
            {"https://x.com/mancity?s=21","Follow Manchester City on X"},
            {"https://x.com/chelseafc?s=21","Follow Chelsea FC on X"},
            {"https://x.com/fcbayern?s=21","Follow FC Bayern München on X"},
            {"https://x.com/bayer04fussball?s=21","Follow Bayer 04 Leverkusen on X"},
            {"https://x.com/juventusfc?s=21","Follow JuventusFC on X"},
            {"https://x.com/ton_blockchain?s=21","Follow TON on X"},
            {"https://x.com/tonkeeper?s=21","Follow Tonkeeper on X"},
            {"https://x.com/realdogshouse?s=21","Follow Dogs Community on X"},
            {"https://x.com/coinbase?s=21","Follow Coinbase on X"},
            {"https://x.com/okx?s=21","Follow OKX on X"},
            {"https://x.com/vinijr?s=21","Follow Vini Jr. on X"},
            {"https://x.com/agarnacho7?s=21","Follow Alejandro Garnacho on X"},
            {"https://x.com/smane_officiel?s=21","Follow Sadio Mané on X"}
        };
        var lst = new List<TaskItem>()
        {
            new TaskItem()
            {
                Category = TaskCategory.Video,
                Title = "Subscribe our Youtube channel",
                Description = "Subscribe our Youtube channel and earn rewards",
                Reward = 50000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "http://www.youtube.com/@THE_SPORTSHERO",
                Code = "",
                Order = 999
            },
            new TaskItem()
            {
                Category = TaskCategory.Video,
                Title = "Subscribe Cristiano Ronaldo channel",
                Description = "Subscribe Cristiano Ronaldo channel and earn rewards",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "https://www.youtube.com/@cristiano",
                ImageUrl = "https://app.sportshero.club/images/game/youtube.webp",
                Order = 1
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Follow us on X",
                Description = "Follow us on X and earn rewards",
                Reward = 50000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "https://x.com/thesportshero",
                ImageUrl = "https://app.sportshero.club/images/game/x.webp",
                SubCategory = SubCategory.X,
                Order = 999
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Join Sports Hero",
                Description = "Join our Telegram channel",
                Reward = 50000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "https://t.me/the_sportshero",
                ImageUrl = "https://app.sportshero.club/images/icons/logo.svg",
                SubCategory = SubCategory.T,
                Order = 999
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Vertus",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@Vertus_App_bot",
                ImageUrl = "https://app.sportshero.club/images/game/vertus.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Hamster Kombat",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@hamster_kombat_bot",
                ImageUrl = "https://app.sportshero.club/images/game/hamster.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Cyber Finance",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@CyberFinanceBot",
                ImageUrl = "https://app.sportshero.club/images/game/cyber.webp",
                SubCategory = SubCategory.T
            },

            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "OKX Racer",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@OKX_official_bot",
                ImageUrl = "https://app.sportshero.club/images/game/okx.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Gemz",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@gemzcoin_bot",
                ImageUrl = "https://app.sportshero.club/images/game/gemz.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Dotcoin",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@dotcoin_bot",
                ImageUrl = "https://app.sportshero.club/images/game/dotcoin.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "TapSwap",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@tapswap_mirror_bot",
                ImageUrl = "https://app.sportshero.club/images/game/tapsw-ap.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Rocky Rabbit",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@rocky_rabbit_bot",
                ImageUrl = "https://app.sportshero.club/images/game/rocky.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Spell Wallet",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@spell_wallet_bot",
                ImageUrl = "https://app.sportshero.club/images/game/spell.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Toncapy",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "@toncapy_bot",
                ImageUrl = "https://app.sportshero.club/images/game/tony.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Dragon Crossing",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "https://t.me/DragonCrossing_bot/DragonCrossing?startapp=Mzc1MjQxNzg5",
                ImageUrl = "https://app.sportshero.club/images/game/dragon.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = "Binance Moonbix bot",
                Description = "Play a game",
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = "https://t.me/Binance_Moonbix_bot/start?startApp=ref_1247878012&startapp=ref_1247878012&utm_medium=web_share_copy",
                ImageUrl = "https://app.sportshero.club/images/game/moonbix.webp",
                SubCategory = SubCategory.T
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Bronze",
                Description = "Reach Bronze level and earn rewards",
                Reward = 500m,
                IsActive = true,
                Value = 0,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Silver",
                Description = "Reach Silver level and earn rewards",
                Reward = 5000m,
                IsActive = true,
                Value = 50000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Gold",
                Description = "Reach Gold level and earn rewards",
                Reward = 50000m,
                IsActive = true,
                Value = 500000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Platinum",
                Description = "Reach Platinum level and earn rewards",
                Reward = 250000m,
                IsActive = true,
                Value = 2500000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Diamond",
                Description = "Reach Diamond level and earn rewards",
                Reward = 500000m,
                IsActive = true,
                Value = 5000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Master",
                Description = "Reach Master level and earn rewards",
                Reward = 1000000m,
                IsActive = true,
                Value = 10000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Coach",
                Description = "Reach Coach level and earn rewards",
                Reward = 5000000m,
                IsActive = true,
                Value = 50000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Chairman",
                Description = "Reach Chairman level and earn rewards",
                Reward = 10000000m,
                IsActive = true,
                Value = 100000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Shark",
                Description = "Reach Shark level and earn rewards",
                Reward = 15000000m,
                IsActive = true,
                Value = 250000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "Lord",
                Description = "Reach Lord level and earn rewards",
                Reward = 25000000m,
                IsActive = true,
                Value = 500000000,
            },
            new TaskItem()
            {
                Category = TaskCategory.Ranking,
                Title = "King",
                Description = "Reach King level and earn rewards",
                Reward = 50000000m,
                IsActive = true,
                Value = 1000000000,
            },
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
        };

        foreach (var tw in twitters)
        {
            lst.Add(new TaskItem()
            {
                Category = TaskCategory.Social,
                Title = tw.Value,
                Description = tw.Value,
                Reward = 10000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Url = tw.Key,
                ImageUrl = "https://app.sportshero.club/images/game/x.webp",
                SubCategory = SubCategory.X
            });
        }
        
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

    public void CreateTaskItem(TaskItem taskItem)
    {
        throw new NotImplementedException();
    }

    public void UpdateTaskItem(string id, TaskItem taskItem)
    {
        throw new NotImplementedException();
    }

    public void DeleteTaskItem(string id)
    {
        throw new NotImplementedException();
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
        if(myTask != null)
        {
            return new ResponseDto<string>()
            {
                Success = false,
                Message = "Task already completed.",
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
        else if (task.Category == TaskCategory.Ranking)
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
        
        
        myTask = new MyTask()
        {
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _myTaskCollection.InsertOneAsync(myTask);
        
        user.Balance += task.Reward;
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
}