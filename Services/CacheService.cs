using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OkCoin.API.Models;
using StackExchange.Redis;

namespace OkCoin.API.Services;

public interface ICacheService
{
    Task<bool> Set(string key, string? value);
    Task<bool> Set(string key, string value, TimeSpan expiry);
    Task<bool> Set<T>(string key, T value);
    Task<bool> Set<T>(string key, T value, TimeSpan expiry);
    Task<string?> Get(string key);
    Task<Dictionary<string, string>> Get(IEnumerable<string> keys);
    Task<T?> GetAsync<T>(string key);
    long GetIncrement(string key);
    Task SetIncrement(string key);
    Task Delete(string key);
    Task IncreasePrefix(string key, long value);
    IDatabase GetDatabase();
}

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    public RedisCacheService(IOptions<DbSettings> myDatabaseSettings)
    {
        var redis = ConnectionMultiplexer.Connect(myDatabaseSettings.Value.RedisConnectionString);
        _db = redis.GetDatabase();
    }

    public async Task<bool> Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) return false;
        await _db.StringSetAsync(key, value);
        return true;
    }

    public Task<bool> Set(string key, string value, TimeSpan expiry)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) return Task.FromResult(false);
        return _db.StringSetAsync(key, value, expiry);
    }

    public async Task<bool> Set<T>(string key, T value)
    {
        if (value == null || string.IsNullOrEmpty(key)) return false;
        return await _db.StringSetAsync(key, JsonConvert.SerializeObject(value));
    }

    public async Task<bool> Set<T>(string key, T value, TimeSpan expiry)
    {
        if (value == null || string.IsNullOrEmpty(key)) return false;
        return await _db.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry);
    }

    public async Task<string?> Get(string key)
    {
        return (await _db.StringGetAsync(key)).ToString();
    }

    public async Task<Dictionary<string, string>> Get(IEnumerable<string> keys)
    {
        var enumerable = keys as string[] ?? keys.ToArray();
        var tasks = await _db.StringGetAsync(enumerable.Select(x=>(RedisKey)x).ToArray());
        return enumerable.Zip(tasks.Select(x=>x.ToString()), (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNull ? default(T) : JsonConvert.DeserializeObject<T>(value.ToString());
    }

    public long GetIncrement(string key)
    {
        var value = _db.StringGet(key);
        return value.IsNull ? 0 : long.Parse(value.ToString());
    }

    public Task SetIncrement(string key)
    {
        return _db.StringIncrementAsync(key);
    }

    public Task Delete(string key)
    {
        return _db.KeyDeleteAsync(key);
    }

    public Task IncreasePrefix(string key, long value = 1)
    {
        var remainingSeconds =
            (DateTime.Today.ToUniversalTime().AddDays(1).AddSeconds(-1) - DateTime.UtcNow).TotalSeconds;
        return _db.StringSetAsync(key, value, TimeSpan.FromSeconds(remainingSeconds));
    }

    public IDatabase GetDatabase()
    {
        return _db;
    }
}