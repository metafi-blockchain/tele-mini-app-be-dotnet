using System.Globalization;
using OkCoin.API.Models;
using OkCoin.API.ViewModels;
using System.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OkCoin.API.Utils;

namespace OkCoin.API.Services;

public interface ITappingService
{
    ResponseDto<List<BoostItemViewModel>> GetUpgradeItems();
    Task<ResponseDto<UserViewModel>> DoUpgradeAsync(string userId, BoostType type);
    Task<ResponseDto<UserViewModel>> TapAsync(string userId, int tapped, long startTime);
    Task<ResponseDto<UserViewModel>> TapAsync(string userId, int tapped);
    Task<ResponseDto<UserViewModel>> RefillFullEnergy(string userId);
    Task<ResponseDto<UserViewModel>> InfinityTapAsync(string userId, int tapped);
    Task<ResponseDto<long>> ClaimBotTap(string userId);
    Task DistributeDailyRewardsForPremiumBot();
}

public class TappingService : ITappingService
{
    
    private readonly IMongoCollection<User> _userCollection;
    private readonly ICacheService _redisCacheService;
    private readonly IStatisticService _statisticService;
    private const string MultiTapRequiredValue = "5|10|20|30|40|50|60|70|80|90|180|360|720|1440|2880|5760|11520|23040|46080|92160";    
    private const string EnergyLimitRequiredValue = "5|10|20|30|40|50|60|70|80|90|180|360|720|1440|2880|5760|11520|23040|46080|92160";
    private const string RechargeSpeedRequiredValue = "5|10|20|50|100";

    public TappingService(IOptions<DbSettings> myDatabaseSettings, ICacheService redisCacheService, IStatisticService statisticService)
    {
        _redisCacheService = redisCacheService;
        _statisticService = statisticService;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _userCollection = database.GetCollection<User>(nameof(User));
    }

    public List<BoostItemViewModel> UpgradeItems()
    {
        var upgradeItems = new List<BoostItemViewModel>();
        var multiTapRequiredValues = MultiTapRequiredValue.Split('|').Select(int.Parse).ToList();
        var energyLimitRequiredValues = EnergyLimitRequiredValue.Split('|').Select(int.Parse).ToList();
        var rechargeSpeedRequiredValues = RechargeSpeedRequiredValue.Split('|').Select(int.Parse).ToList();
        for (var i = 1; i <= multiTapRequiredValues.Count; i++)
        {
            upgradeItems.Add(new BoostItemViewModel()
            {
                Id = $"{BoostType.MultiTap}-{i}",
                Name = $"Level {i}",
                Price = multiTapRequiredValues[i-1] * 1000,
                Value = i,
                Type = BoostType.MultiTap,
                Level = i,
            });
        }
        for (var i = 1; i <= energyLimitRequiredValues.Count; i++)
        {
            upgradeItems.Add(new BoostItemViewModel()
            {
                Id = $"{BoostType.EnergyLimit}-{i}",
                Name = $"Level {i}",
                Price = energyLimitRequiredValues[i-1] * 1000,
                Value = i * 500,
                Type = BoostType.EnergyLimit,
                Level = i,
            });
        }
        
        for (var i = 1; i <= rechargeSpeedRequiredValues.Count; i++)
        {
            upgradeItems.Add(new BoostItemViewModel()
            {
                Id = $"{BoostType.RechargeSpeed}-{i}",
                Name = $"Level {i}",
                Price = rechargeSpeedRequiredValues[i-1] * 1000,
                Value = i,
                Type = BoostType.RechargeSpeed,
                Level = i,
            });
        }
        
        upgradeItems.Add(
            new BoostItemViewModel()
            {
                Id = $"{BoostType.TapBot}-1",
                Name = "Level 1",
                Description = "Tap bot will tap for you. Get +4 points for every second until you reach 172800 points",
                Price = 200000,
                Value = 4,
                Type = BoostType.TapBot,
                Level = 1,
            }
        );
        
        upgradeItems.Add(
            new BoostItemViewModel()
            {
                Id = $"{BoostType.PremiumBot}-1",
                Name = "Level 1",
                Description = "Tap bot will tap for you. Get 1M points for every day.",
                Price = 1,
                Value = 1000000000m,
                Type = BoostType.PremiumBot,
                Level = 1,
            }
        );
        
        return upgradeItems;
    }
    
    public ResponseDto<List<BoostItemViewModel>> GetUpgradeItems()
    {
        return new ResponseDto<List<BoostItemViewModel>>()
        {
            Data = UpgradeItems(),
            Success = true
        };
    }

    public async Task<ResponseDto<UserViewModel>> DoUpgradeAsync(string userId, BoostType type)
    {
        var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
        if (user == null)
            return new ResponseDto<UserViewModel>()
            {
                Message = "User is not found",
                Success = false
            };
        if (type == BoostType.EnergyLimit)
        {
            var upgradeItem = UpgradeItems().FirstOrDefault(x => x.Type == type & x.Level == (user.EnergyLimitLevel + 1));
            if (upgradeItem == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Upgrade item is not found",
                    Success = false
                };

            if (user.Balance < upgradeItem.Price)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Balance is not enough",
                    Success = false
                };
            
            user.Balance -= upgradeItem.Price;
            user.EnergyLimitLevel += 1;
            user.EnergyLimitValue = int.Parse(upgradeItem.Value.ToString(CultureInfo.CurrentCulture));
            
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            
            
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)upgradeItem.Price * -1,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.Upgrade.ToString(),
                Description = "Upgrade energy limit to " + upgradeItem.Value
            });
            
            return new ResponseDto<UserViewModel>()
            {
                Message = "Upgrade is successful",
                Success = true,
                Data = user.ToUserViewModel()
            };
        }
        if (type == BoostType.MultiTap)
        {
            var upgradeItem = UpgradeItems().FirstOrDefault(x => x.Type == type & x.Level == (user.MultiTapLevel + 1));
            if (upgradeItem == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Upgrade item is not found",
                    Success = false
                };

            if (user.Balance < upgradeItem.Price)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Balance is not enough",
                    Success = false
                };
            
            user.Balance -= upgradeItem.Price;
            user.MultiTapLevel += 1;
            user.MultiTapValue = upgradeItem.Value;
            
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            
            
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)upgradeItem.Price * -1,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.Upgrade.ToString(),
                Description = "Upgrade multi tap to " + upgradeItem.Value
            });
            return new ResponseDto<UserViewModel>()
            {
                Message = "Upgrade is successful",
                Success = true,
                Data = user.ToUserViewModel()
            };
        }

        if(type == BoostType.RechargeSpeed)
        {
            var upgradeItem = UpgradeItems().FirstOrDefault(x => x.Type == BoostType.RechargeSpeed & x.Level == (user.RechargeSpeedLevel + 1));
            if (upgradeItem == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Upgrade item is not found",
                    Success = false
                };

            if (user.Balance < upgradeItem.Price)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Balance is not enough",
                    Success = false
                };
            
            user.Balance -= upgradeItem.Price;
            user.RechargeSpeedLevel += 1;
            user.RechargeSpeedValue = upgradeItem.Value;
            
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            
            
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)upgradeItem.Price * -1,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.Upgrade.ToString(),
                Description = "Upgrade recharge speed to " + upgradeItem.Value
            });
            
            return new ResponseDto<UserViewModel>()
            {
                Message = "Upgrade is successful",
                Success = true,
                Data = user.ToUserViewModel()
            };
        }
        if(type == BoostType.TapBot)
        {
            if (user.HaveTapBot)
            {
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Tap bot is already active",
                    Success = false,
                };
            }
            var upgradeItem = UpgradeItems().FirstOrDefault(x => x.Type == BoostType.TapBot);
            if (upgradeItem == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Upgrade item is not found",
                    Success = false
                };

            if (user.Balance < upgradeItem.Price)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "Balance is not enough",
                    Success = false
                };
            
            user.Balance -= upgradeItem.Price;
            user.LastTapped = DateTime.UtcNow; // Reset last tapped to calculate point for tap bot after 20 minutes from upgrade
            user.HaveTapBot = true;
            
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)upgradeItem.Price * -1,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.Upgrade.ToString(),
                Description = "Upgrade to tap bot"
            });
            
            return new ResponseDto<UserViewModel>()
            {
                Message = "Tap bot is activated.",
                Success = true,
                Data = user.ToUserViewModel()
            };
        }
        
        return new ResponseDto<UserViewModel>()
        {
            Message = "Upgrade item is invalid",
            Success = false
        };
    }
    public async Task<ResponseDto<long>> ClaimBotTap(string userId)
    {
        try
        {
            var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
            if (user == null)
                return new ResponseDto<long>()
                {
                    Message = "User is not found",
                    Success = false,
                    Data = 0
                };
            if (!user.HaveTapBot)
            {
                return new ResponseDto<long>()
                {
                    Message = "Tap bot is not active",
                    Success = false
                };
            }

            var totalSecond = (long)(DateTime.UtcNow - user.LastTapped.AddMinutes(20)).TotalSeconds;
            if (totalSecond <= 0)
                return new ResponseDto<long>()
                {
                    Message = "Tap bot yet to start.",
                    Success = true,
                    Data = 0
                };
            var earnedPoint = (decimal)totalSecond * 4;
            if (earnedPoint > 172800) earnedPoint = 172800;
            
            var totalSecondToCalculateAvailableTap = (long)(DateTime.UtcNow - user.LastTapped).TotalSeconds;
            totalSecondToCalculateAvailableTap = totalSecondToCalculateAvailableTap < 0 ? 0 : totalSecondToCalculateAvailableTap;
            var calculatedTotalSecondFromLastTapped =
                totalSecondToCalculateAvailableTap * user.RechargeSpeedValue;
            var totalAvailableTap = user.AvailableTapCount + calculatedTotalSecondFromLastTapped;
            if (totalAvailableTap > user.EnergyLimitValue) totalAvailableTap = user.EnergyLimitValue;

            user.Balance += earnedPoint;
            user.GrandBalance += earnedPoint;
            user.TapBalance += earnedPoint;
            user.TapBalanceUpdatedAt = DateTime.UtcNow;
            user.LastTapped = DateTime.UtcNow;
            user.AvailableTapCount = (int)totalAvailableTap;
            
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            await _statisticService.UpdateTotalTap((long)earnedPoint);
            await _statisticService.UpdateTotalSharedBalance((long)earnedPoint);
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)earnedPoint,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.TapBotReward.ToString()
            });
            return new ResponseDto<long>()
            {
                Message = $"You have earned {(long)earnedPoint} points from tap bot.",
                Success = true,
                Data = (long)earnedPoint
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await _statisticService.LogExceptionAsync("TapAsync:" + e.Message);
            return new ResponseDto<long>()
            {
                Message = "An error occurred while processing the request. Please try again later.",
                Success = false
            };
        }
    }
    public async Task<ResponseDto<UserViewModel>> TapAsync(string userId, int tapped, long startTime)
    {
        try
        {
            var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
            if (user == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "User is not found",
                    Success = false
                };
            var offset = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
            if (offset < user.LastTapped)
            {
                offset = DateTimeOffset.UtcNow;
            }

            var totalSecond = (long)(DateTime.UtcNow - offset.UtcDateTime).TotalSeconds;
            totalSecond = totalSecond < 0 ? 0 : totalSecond;
            var calculatedTotalSecondFromLastTapped =
                (long)((DateTime.UtcNow - user.LastTapped).TotalSeconds + totalSecond) * user.RechargeSpeedValue;
            var totalAvailableTap = user.AvailableTapCount + calculatedTotalSecondFromLastTapped;
            if (totalAvailableTap > user.EnergyLimitValue) totalAvailableTap = user.EnergyLimitValue;

            var tappedValue = tapped * user.MultiTapValue;

            if (totalAvailableTap < tappedValue)
            {
                tappedValue = (int)totalAvailableTap;
            }

            user.AvailableTapCount = (int)(totalAvailableTap - tappedValue);
            user.Balance += tappedValue;
            user.GrandBalance += tappedValue;
            user.LastTapped = DateTime.UtcNow;
            user.TapBalance += tappedValue;
            user.TapBalanceUpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
            await _statisticService.UpdateTotalTap((long)tappedValue);
            await _statisticService.UpdateTotalSharedBalance((long)tappedValue);

            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)tappedValue,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.TapReward.ToString()
            });
            return new ResponseDto<UserViewModel>()
            {
                Message = $"You have earned {tappedValue} points.",
                Success = true,
                Data = user.ToUserViewModel()
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await _statisticService.LogExceptionAsync("TapAsync:" + e.Message);
            return new ResponseDto<UserViewModel>()
            {
                Message = "An error occurred while processing the request. Please try again later.",
                Success = false
            };
        }
    }

    public async Task<ResponseDto<UserViewModel>> TapAsync(string userId, int tapped)
    {
        return await TapAsync(userId, tapped, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public async Task<ResponseDto<UserViewModel>> InfinityTapAsync(string userId, int tapped)
    {
        try
        {
            var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
            if (user == null)
                return new ResponseDto<UserViewModel>()
                {
                    Message = "User is not found",
                    Success = false
                };
            var userVM = user.ToUserViewModel();
            userVM = GetAdditionalUserInformation(userVM);
            if (userVM.InfinityTapUsed >= Constants.GameSettings.AllowedInfinityTap)
            {
                return new ResponseDto<UserViewModel>()
                {
                    Message = "You have reached the maximum limit of infinity tap.",
                    Success = false
                };
            }

            await _redisCacheService.IncreasePrefix(
                string.Format(Constants.GameSettings.AllowedInfinityTapRedisKey, userVM.TelegramId),
                userVM.InfinityTapUsed + 1);
            if (tapped > 500) tapped = 500;
            var tappedValue = tapped * user.MultiTapValue;
            user.Balance += tappedValue;
            user.GrandBalance += tappedValue;
            //user.LastTapped = DateTime.UtcNow;
            user.TapBalance += tappedValue;
            user.TapBalanceUpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);

            await _statisticService.UpdateTotalTap((long)tappedValue);
            await _statisticService.UpdateTotalSharedBalance((long)tappedValue);
            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = (long)tappedValue,
                Status = "Success",
                UserId = userId,
                TransactionType = InGameTransactionType.InfinityTapReward.ToString()
            });
            var userResponseDto = new ResponseDto<UserViewModel>()
            {
                Message = $"You have earned {tappedValue} points.",
                Success = true,
                Data = user.ToUserViewModel()
            };
            userResponseDto.Data = GetAdditionalUserInformation(userResponseDto.Data);
            return userResponseDto;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await _statisticService.LogExceptionAsync("InfinityTapAsync:" + e.Message);
            return new ResponseDto<UserViewModel>()
            {
                Message = "An error occurred while processing the request. Please try again later.",
                Success = false
            };
        }
    }
    
    public async Task<ResponseDto<UserViewModel>> RefillFullEnergy(string userId)
    {
        var user = _userCollection.Find(x => x.Id == userId).FirstOrDefault();
        if (user == null)
            return new ResponseDto<UserViewModel>()
            {
                Message = "User is not found",
                Success = false
            };

        var userVM = user.ToUserViewModel();
        userVM = GetAdditionalUserInformation(userVM);
        if (userVM.FullEnergyRefillUsed >= userVM.AllowedFullEnergyRefill)
        {
            return new ResponseDto<UserViewModel>()
            {
                Message = "You have reached the maximum limit of full energy refill.",
                Success = false
            };
        }
        await _redisCacheService.IncreasePrefix(string.Format(Constants.GameSettings.AllowedFullEnergyRefillRedisKey, userVM.TelegramId), userVM.FullEnergyRefillUsed + 1);
        user.AvailableTapCount = user.EnergyLimitValue;
        user.UpdatedAt = DateTime.UtcNow;
        await _userCollection.ReplaceOneAsync(x => x.Id == userId, user);
        userVM.FullEnergyRefillUsed += 1;
        userVM.AvailableEnergy = user.EnergyLimitValue;
        return new ResponseDto<UserViewModel>()
        {
            Message = "Full energy refill is successful.",
            Success = true,
            Data = userVM
        };
    }
    
    public async Task DistributeDailyRewardsForPremiumBot()
    {
        var users = _userCollection.Find(x => x.HavePremiumBot).ToList();
        foreach (var user in users)
        {
            try
            {
                var reward = Constants.GameSettings.DailyPremiumBotReward;
                if (user.PremiumBotAt.HasValue && user.PremiumBotAt.Value.AddDays(1) > DateTime.UtcNow)
                {
                    var remainingTime = (DateTime.UtcNow - user.PremiumBotAt.Value).TotalSeconds;
                    if (remainingTime > 86400) remainingTime = 86400;
                    reward = (long)(Constants.GameSettings.DailyPremiumBotReward * (remainingTime / 86400));
                }

                user.Balance += reward;
                user.GrandBalance += reward;
                user.BalanceUpdatedAt = DateTime.UtcNow;
                await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);
                await _statisticService.UpdateTotalSharedBalance(reward);
                await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
                {
                    Amount = reward,
                    Status = "Success",
                    UserId = user.Id,
                    Description = "Daily reward for premium bot",
                    TransactionType = InGameTransactionType.PremiumBotReward.ToString()
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _statisticService.LogExceptionAsync("DistributeDailyRewardsForPremiumBot:" + e.Message);
            }
        }
    }
    
    private UserViewModel GetAdditionalUserInformation(UserViewModel userViewModel)
    {
        userViewModel.InfinityTapUsed = (int)_redisCacheService.GetIncrement(string.Format(Constants.GameSettings.AllowedInfinityTapRedisKey, userViewModel.TelegramId));
        userViewModel.FullEnergyRefillUsed = (int)_redisCacheService.GetIncrement(string.Format(Constants.GameSettings.AllowedFullEnergyRefillRedisKey, userViewModel.TelegramId));
        return userViewModel;
    }
    
    
}