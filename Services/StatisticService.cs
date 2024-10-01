using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using OkCoin.API.Models;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;
public interface IStatisticService
{
    Task<GameStatisticViewModel> GetGameStatistic();
    Task UpdateTotalSharedBalance(long balance);
    Task UpdateTotalTap(long balance);
    Task UpdateTotalUsers();
    Task MarkUserAsOnlineAsync(string userId);
    Task LogInGameTransactionAsync(InGameTransaction transaction);
    Task LogExceptionAsync(string message);
    Task Fix();
}
public class StatisticService : IStatisticService
{
    private readonly ICacheService _cacheService;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IMongoCollection<InGameTransaction> _inGameTransactionCollection;
    private readonly int _onlineStatusExpiryInMinutes = 10; // Adjust as needed
    public StatisticService(ICacheService cacheService, IOptions<DbSettings> myDatabaseSettings)
    {
        _cacheService = cacheService;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _userCollection = database.GetCollection<User>(nameof(User));
        _inGameTransactionCollection = database.GetCollection<InGameTransaction>(nameof(InGameTransaction));
    }
    public async Task<GameStatisticViewModel> GetGameStatistic()
    {
        var totalUsers = await _cacheService.GetAsync<long?>("TotalUsers");
        if(totalUsers == null)
        {
            totalUsers = await _userCollection.EstimatedDocumentCountAsync();
            await _cacheService.Set("TotalUsers", totalUsers);
        }
        var totalSharedBalance = await _cacheService.GetAsync<long?>("TotalSharedBalance");
        if(totalSharedBalance == null)
        {
            totalSharedBalance = 0; // sum of all user balance
            await _cacheService.Set("TotalSharedBalance", totalSharedBalance);
        }
        var totalTouch = await _cacheService.GetAsync<long?>("TotalTouch");
        if(totalTouch == null)
        {
            totalTouch = 0; // sum of all user touch
            await _cacheService.Set("TotalTouch", totalTouch);
        }

        return new GameStatisticViewModel()
        {
            TotalUsers = totalUsers.Value,
            TotalSharedBalance = totalSharedBalance.Value,
            TotalTouch = totalTouch.Value,
            DailyUsers = await CountUsersActiveInLast24HoursAsync(),
            OnlineUsers = await CountOnlineUsersAsync()
        };
    }

    public async Task UpdateTotalSharedBalance(long balance)
    {
        var totalSharedBalance = await _cacheService.GetAsync<long?>("TotalSharedBalance");
        await _cacheService.Set("TotalSharedBalance", totalSharedBalance + balance);
    }

    public async Task UpdateTotalTap(long balance)
    {
        var totalTouch = await _cacheService.GetAsync<long?>("TotalTouch");
        await _cacheService.Set("TotalTouch", totalTouch + balance);
    }

    public async Task UpdateTotalUsers()
    {
        await _cacheService.SetIncrement("TotalUsers");
    }
    
    public async Task MarkUserAsOnlineAsync(string userId)
    {
        var db = _cacheService.GetDatabase();
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await db.SortedSetAddAsync("online_users", userId, currentTime);
    }

    public async Task LogInGameTransactionAsync(InGameTransaction transaction)
    {
        var db = _cacheService.GetDatabase();
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await db.SortedSetAddAsync("transactions", JsonConvert.SerializeObject(transaction), currentTime);
    }
    
    public async Task LogExceptionAsync(string message)
    {
        var db = _cacheService.GetDatabase();
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await db.SortedSetAddAsync("exceptions", message, currentTime);
    }
    
    public async Task Fix()
    {
        var trans = _inGameTransactionCollection.Find(x =>
                x.TransactionType == InGameTransactionType.TapBotReward.ToString()
                && x.CreatedAt >= DateTime.UtcNow.AddHours(-13))
            .ToList().GroupBy(x => x.UserId)
            .Select(x=>new {Key = x.Key, Trans = x.ToList()});

        var totalAmountToCollect = 0m;
        foreach (var tran in trans)
        {
            var tranXs = tran.Trans.OrderBy(x => x.CreatedAt).ToList(); 
            DateTime? firstDate = null;
            var amountToColect = 0m;
            foreach (var inGameTran in tranXs)
            {
                if(firstDate == null)
                {
                    firstDate = inGameTran.CreatedAt;
                    continue;
                }
                
                var totalSeconds = (inGameTran.CreatedAt - firstDate.Value).TotalSeconds;
                //var amountShouldCollect = 4 * (inGameTran.CreatedAt.AddMinutes(-10) - firstDate.Value).TotalSeconds;
                if (totalSeconds <= 600)
                {
                    amountToColect += inGameTran.Amount;
                    var inGameRecord = new InGameTransaction()
                    {
                        UserId = inGameTran.UserId,
                        Amount = inGameTran.Amount * -1,
                        TransactionType = InGameTransactionType.VarCheckRevert.ToString(),
                        CreatedAt = inGameTran.CreatedAt
                    };
                    await _inGameTransactionCollection.InsertOneAsync(inGameRecord);
                }
                
                firstDate = inGameTran.CreatedAt;
            }

            if (amountToColect > 0)
            {
                var user = _userCollection.Find(x=>x.Id == tran.Key).FirstOrDefault();
                if(user != null)
                {
                    user.Balance -= amountToColect;
                    await _userCollection.ReplaceOneAsync(x=>x.Id == user.Id, user);
                }
                
                totalAmountToCollect += amountToColect;
            }
        }
        
        await UpdateTotalTap((int)totalAmountToCollect * -1);
        await UpdateTotalSharedBalance((int)totalAmountToCollect * -1);
        
        // var totalPoints = trans.Select(x => new
        // {
        //     UserId = x.Key,
        //     Total = x.Sum(y => y.Amount)
        // }).Where(x=>x.Total > 172800 * 1.5).OrderByDescending(x=>x.Total).ToList();
        //
        // var totalTrans = trans.Select(x => new
        // {
        //     UserId = x.Key,
        //     Total = x.Count()
        // }).ToList();

        var x = 1;
    }

    private async Task<long> CountOnlineUsersAsync()
    {
        var onlineStatusExpiry = TimeSpan.FromMinutes(_onlineStatusExpiryInMinutes);
        var db = _cacheService.GetDatabase();
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var minScore = currentTime - (long)onlineStatusExpiry.TotalSeconds;

        // Count users with scores within the last 5 minutes
        return await db.SortedSetLengthAsync("online_users", minScore, currentTime);
    }

    private async Task<long> CountUsersActiveInLast24HoursAsync()
    {
        var last24Hours = TimeSpan.FromHours(24);
        var db = _cacheService.GetDatabase();
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var minScore = currentTime - (long)last24Hours.TotalSeconds;

        // Count users with scores within the last 24 hours
        return await db.SortedSetLengthAsync("online_users", minScore, currentTime);
    }
}