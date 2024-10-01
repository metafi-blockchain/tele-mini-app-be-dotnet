using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using OkCoin.API.Models;
using StackExchange.Redis;
using static System.Console;

namespace OkCoin.API.Services;

public class BackgroundCronJobService : BackgroundService
{
    private readonly ITonChainService _tonChainService;
    private readonly ITappingService _tappingService;
    private readonly IDatabase _db;
    private readonly IMongoCollection<InGameTransaction> _inGameTransactionCollection;
    private readonly CronJobSettings _cronJobSettings;
    
    
    public BackgroundCronJobService(ITonChainService tonChainService, ITappingService tappingService, IOptions<DbSettings> myDatabaseSettings, IOptions<CronJobSettings> cronJobSettings)
    {
        _tonChainService = tonChainService;
        _tappingService = tappingService;
        var redis = ConnectionMultiplexer.Connect(myDatabaseSettings.Value.RedisConnectionString);
        _db = redis.GetDatabase();
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _inGameTransactionCollection = database.GetCollection<InGameTransaction>(nameof(InGameTransaction));
        _cronJobSettings = cronJobSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        WriteLine($"GetTransactionsAsync: {_cronJobSettings.SyncTonTransaction} - DistributeReward: {_cronJobSettings.AIBotRewardDistribution} - ProcessInGameTransactionAsync: {_cronJobSettings.InGameTransaction}");
        var job1 = GetTransactionsAsync(stoppingToken);
        var job2 = DistributeReward(stoppingToken);
        var job3 = ProcessInGameTransactionAsync(stoppingToken);
        await Task.WhenAll(job1, job2, job3);
    }
    
    private async Task GetTransactionsAsync(CancellationToken stoppingToken)
    {
        if(!_cronJobSettings.SyncTonTransaction) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            await _tonChainService.GetTransactionsAsync();
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    private async Task DistributeReward(CancellationToken stoppingToken)
    {
        if(!_cronJobSettings.AIBotRewardDistribution) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRunTime = now.Date.AddDays(1); // Calculate next 12:00 AM
            var delay = nextRunTime - now;
            await Task.Delay(delay, stoppingToken);
            await _tappingService.DistributeDailyRewardsForPremiumBot();
            await Task.Delay(TimeSpan.FromHours(23), stoppingToken);
        }
    }
    
    private async Task ProcessInGameTransactionAsync(CancellationToken stoppingToken)
    {
        if(!_cronJobSettings.InGameTransaction) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessInGameTransactionAsync();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
    
    private async Task ProcessInGameTransactionAsync()
    {
        const long startIndex = 0;
        const long batchSize = 100;
        const string sortedSetKey = "transactions";

        while (true)
        {
            // Fetch a batch of records (100 at a time)
            var records = await _db.SortedSetRangeByRankWithScoresAsync(
                sortedSetKey,
                start: startIndex,
                stop: startIndex + batchSize - 1
            );

            // If no more records, break the loop
            if (records.Length == 0)
            {
                break;
            }

            // Process each record
            foreach (var record in records)
            {
                var member = record.Element.HasValue ? record.Element.ToString() : string.Empty;
                var score = record.Score;

                var transaction = JsonConvert.DeserializeObject<InGameTransaction>(member);
                if(transaction != null)
                {
                    await _inGameTransactionCollection.InsertOneAsync(transaction);
                }
                await _db.SortedSetRemoveAsync(sortedSetKey, record.Element);
            }
        }

        Console.WriteLine("All records processed.");
    }
}