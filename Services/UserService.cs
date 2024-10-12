using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OkCoin.API.Models;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface IUserService
{
    Task<List<User>> GetAsync();
    Task<ResponseDto<UserViewModel>> GetAsync(string id);
    Task<User?> GetByIdAsync(string id);
    Task CreateAsync(User newUser);
    Task UpdateAsync(string id, User updatedUser);
    Task RemoveAsync(string id);
    Task<ResponseDto<TokenViewModel>> AuthenticateAsync(LoginViewModel loginViewModel);
    Task<ResponseDto<MeResponse>> MeAsync(string id);
    Task<ResponseDto<List<RefererViewModel>>> ListReferrals(string id);
    long CountTotalUsers();
    Task<ResponseDto<int>> GetPointRewardAsync(string userId);
}

public class UserService : IUserService
{
    private readonly IMongoCollection<User> _userCollection;
    private readonly ITappingService _tappingService;
    private readonly ITaskService _taskService;
    private readonly JwtSettings _jwtSettings;
    private readonly ICacheService _redisCacheService;
    private readonly IStatisticService _statisticService;
    private readonly List<PointRewardDocuments> _pointRewardDocuments;

    public UserService(IOptions<DbSettings> myDatabaseSettings, IOptions<JwtSettings> jwtSettings, ITaskService taskService, ITappingService tappingService, ICacheService redisCacheService, IStatisticService statisticService, IOptions<List<PointRewardDocuments>> pointRewardDocumentOption)
        {
            _taskService = taskService;
            _tappingService = tappingService;
            _redisCacheService = redisCacheService;
            _statisticService = statisticService;
            _jwtSettings = jwtSettings.Value;
            _pointRewardDocuments = pointRewardDocumentOption.Value;
            var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
            var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(nameof(User));
        }
    
    public async Task<List<User>> GetAsync() =>
        await _userCollection.Find(_ => true).ToListAsync();
    public async Task<ResponseDto<UserViewModel>> GetAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return new ResponseDto<UserViewModel>()
            {
                Data = new UserViewModel(),
                Message = "Invalid Id",
                Success = false
            };
        var user = await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (user == null)
            return new ResponseDto<UserViewModel>()
            {
                Data = new UserViewModel(),
                Message = "User is not found",
                Success = false
            };
        return new ResponseDto<UserViewModel>()
        {
            Data = GetAdditionalUserInformation(user.ToUserViewModel()),
            Message = "User is found",
            Success = true
        };
    }
    public async Task<ResponseDto<MeResponse>> MeAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            return new ResponseDto<MeResponse>()
            {
                Message = "Invalid Id",
                Success = false
            };
        var user = await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (user == null)
            return new ResponseDto<MeResponse>()
            {
                Message = "User is not found",
                Success = false
            };
        var res = new ResponseDto<MeResponse>()
        {
            Data = new MeResponse()
            {
                User = GetAdditionalUserInformation(user.ToUserViewModel()),
                MyTasks = _taskService.MyTasks(id).Data.OrderByDescending(x=>x.Order).ToList(),
                BoostItems = _tappingService.GetUpgradeItems().Data,
            },
            Message = "User is found",
            Success = true
        };
        var levels = res.Data.MyTasks.Where(x=>x.Category == TaskCategory.Ranking).ToList();
        var rank = levels.OrderByDescending(x=>x.TaskValue).FirstOrDefault(x=>x.IsCompleted)?.Title ?? "Bronze";
        res.Data.User.UserRank = rank;
        return res;
    }
    public Task<ResponseDto<List<RefererViewModel>>> ListReferrals(string id)
    {
        var user = _userCollection.Find(x => x.Id == id).FirstOrDefault();
        if (user == null)
            return Task.FromResult(new ResponseDto<List<RefererViewModel>>()
            {
                Success = false,
                Message = "User not found"
            });
        var referees = _userCollection.Find(x => x.RefererId == user.TelegramId && x.Id != id).ToList()
            .Select(x => new RefererViewModel()
            {
                Id = x.Id ?? string.Empty,
                TelegramUsername = x.TelegramUsername,
                TelegramId = x.TelegramId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                IsTelegramPremium = x.IsTelegramPremium
            }).ToList();
        return Task.FromResult(new ResponseDto<List<RefererViewModel>>()
        {
            Success = true,
            Data = referees
        });
    }

    public long CountTotalUsers()
    {
        return _userCollection.EstimatedDocumentCount();
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
    public async Task CreateAsync(User newUser) =>
        await _userCollection.InsertOneAsync(newUser);
    public async Task UpdateAsync(string id, User updatedUser) =>
        await _userCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);
    public async Task RemoveAsync(string id) =>
        await _userCollection.DeleteOneAsync(x => x.Id == id);
    public async Task<ResponseDto<TokenViewModel>> AuthenticateAsync(LoginViewModel loginViewModel)
    {
        var queryParams = HttpUtility.ParseQueryString(loginViewModel.TelegramData);
        var hash = queryParams["hash"];
        var dataCheckString = string.Join("\n", queryParams.AllKeys
            .Where(key => key != "hash")
            .OrderBy(key => key)
            .Select(key => $"{key}={queryParams[key]}"));

        var secretKey = ComputeHmacSha256Hash(_jwtSettings.BotToken, "WebAppData");
        var computedHash = ComputeHmacSha256Hash(dataCheckString, secretKey);
        if (computedHash == hash)
        {
            var paramUser = queryParams["user"];
            var userObject = JObject.Parse(paramUser ?? "{}");
            var currentUserId = userObject["id"]?.ToString() ?? string.Empty;
            var user = await _userCollection.Find(x => x.TelegramId == currentUserId).FirstOrDefaultAsync();
            var isFirstLogin = false;
            if (user == null)
            {
                user = new User()
                {
                    TelegramUsername = userObject["username"]?.ToString() ?? string.Empty,
                    TelegramId = userObject["id"]?.ToString() ?? string.Empty,
                    FirstName = userObject["first_name"]?.ToString() ?? string.Empty,
                    LastName = userObject["last_name"]?.ToString() ?? string.Empty,
                    IsTelegramPremium = bool.Parse(userObject["is_premium"]?.ToString() ?? "false") ,
                    DefaultLanguage = userObject["language"]?.ToString() ?? "en",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastTapped = DateTime.UtcNow,
                    RefererCount = 0,
                    Balance = 0m,
                    BalanceUpdatedAt = DateTime.UtcNow,
                };
                bool isReferral = false;
                if (!string.IsNullOrEmpty(loginViewModel.RefId) && loginViewModel.RefId != user.TelegramId)
                {
                    user.RefererId = loginViewModel.RefId;
                    isReferral = true;
                }
                await CreateAsync(user);
                await _statisticService.UpdateTotalUsers();
                isFirstLogin = true;
                if (isReferral)
                {
                    var refererUser = await _userCollection.Find(x => x.TelegramId == loginViewModel.RefId).FirstOrDefaultAsync();
                    if(refererUser != null)
                    {
                        refererUser.RefererCount++;
                        await UpdateAsync(refererUser.Id ?? string.Empty, refererUser);
                    }
                }
            }
            
            var token = GenerateToken(user);
            return new ResponseDto<TokenViewModel>()
            {
                Data = new TokenViewModel()
                {
                    Token = token,
                    RefreshToken = string.Empty,
                    Expires = DateTime.Now.AddDays(1),
                    IsFirstLogin = isFirstLogin
                },
                Message = "Authenticated",
                Success = true
            };
        }

        return new ResponseDto<TokenViewModel>()
        {
            Data = new TokenViewModel(),
            Message = "Unauthorized",
            Success = false
        };
    }
    private string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(Constants.CustomClaimTypes.UserId, user.Id??string.Empty),
            new Claim(Constants.CustomClaimTypes.TelegramUsername, user.TelegramUsername),
            new Claim(Constants.CustomClaimTypes.TelegramLastName, user.LastName),
            new Claim(Constants.CustomClaimTypes.TelegramFirstName, user.FirstName),
            new Claim(Constants.CustomClaimTypes.TelegramId, user.TelegramId),
            new Claim(Constants.CustomClaimTypes.TelegramPremium, user.IsTelegramPremium.ToString()),
            new Claim(Constants.CustomClaimTypes.TelegramLanguage, user.DefaultLanguage),
            new Claim(Constants.CustomClaimTypes.IsAdmin, user.IsAdmin.ToString())
        };
        var token = new JwtSecurityToken(_jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private static string ComputeHmacSha256Hash(string message, byte[] secret)
    {
        using (var hmacsha256 = new HMACSHA256(secret))
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hashMessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
    }
    private static byte[] ComputeHmacSha256Hash(string message, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using (var hmacsha256 = new HMACSHA256(keyBytes))
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            return hmacsha256.ComputeHash(messageBytes);
        }
    }
    private UserViewModel GetAdditionalUserInformation(UserViewModel userViewModel)
    {
        userViewModel.InfinityTapUsed = (int)_redisCacheService.GetIncrement(string.Format(Constants.GameSettings.AllowedInfinityTapRedisKey, userViewModel.TelegramId));
        userViewModel.FullEnergyRefillUsed = (int)_redisCacheService.GetIncrement(string.Format(Constants.GameSettings.AllowedFullEnergyRefillRedisKey, userViewModel.TelegramId));
        return userViewModel;
    }

    public async Task<ResponseDto<int>> GetPointRewardAsync(string userId)
    {
        if (!ObjectId.TryParse(userId, out _))
            return new ResponseDto<int>()
            {
                Message = "Invalid Id",
                Success = false
            };

        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

        if (user == null)
        {
            return new ResponseDto<int>()
            {
                Message = "User is not found",
                Success = false
            };
        }

        if (user.IsReceivePointReward) 
        {
            return new ResponseDto<int>()
            {
                Message = "This user has already received the point reward.",
                Success = false
            };
        }

        var point = 0;
        var telegramId = Convert.ToDouble(user.TelegramId) / 1000000;
        foreach (var item in _pointRewardDocuments.OrderByDescending(c => c.Year))
        {
            if (telegramId > item.Ids)
            {
                point = item.Point;
                break;
            }
        }

        user.IsReceivePointReward = true;
        _ = UpdateAsync(userId, user);

        return new ResponseDto<int>
        {
            Success = true,
            Message = "Congratulations on receiving your bonus points!",
            Data = point
        };
    }
}