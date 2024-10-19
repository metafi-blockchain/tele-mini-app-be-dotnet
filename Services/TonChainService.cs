using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ITonChainService
{
    Task GetTransactionsAsync();
    Task<ResponseDto<bool>> WithdrawAsync(WithdrawRequestViewModel requestViewModel, string userId);
    Task<ResponseDto<bool>> GetTranStatus(string teleId, string? timestamp = null);
    Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawByUserIdAsync(string userId);
}

public class TonChainService : ITonChainService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _redisCacheService;
    private readonly TonChainSettings _tonChainSettings;
    private readonly IMongoCollection<TonTransaction> _tonTransactionCollection;
    private readonly IMongoCollection<WithdrawRequest> _withdrawRequestCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IStatisticService _statisticService;

    public TonChainService(IOptions<DbSettings> myDatabaseSettings, HttpClient httpClient, ICacheService redisCacheService, IOptions<TonChainSettings> tonChainSettings, IStatisticService statisticService)
    {
        _httpClient = httpClient;
        _redisCacheService = redisCacheService;
        _statisticService = statisticService;
        _tonChainSettings = tonChainSettings.Value;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _tonTransactionCollection = database.GetCollection<TonTransaction>(nameof(TonTransaction));
        _userCollection = database.GetCollection<User>(nameof(User));
        _withdrawRequestCollection = database.GetCollection<WithdrawRequest>(nameof(WithdrawRequest));
    }

    public async Task GetTransactionsAsync()
    {
        var lastBlock = await _redisCacheService.GetAsync<string>("TONLastBlockTime");
        var baseUrl = _tonChainSettings.IsMainNet ? "https://tonapi.io" : "https://testnet.tonapi.io";
        var apiUrl = $"{baseUrl}/v2/blockchain/accounts/{_tonChainSettings.WalletAddress}/transactions?limit=100&sort_order=desc";
        if(!string.IsNullOrEmpty(lastBlock)) apiUrl += "&after_lt=" + lastBlock;
        try
        {
            // Send GET request to the API
            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            // Read the response
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse the JSON response
            var json = JObject.Parse(responseBody);
            var transactions = json["transactions"];

            // Iterate through each transaction and extract the required details
            var index = 0;
            if (transactions != null)
                foreach (var transaction in transactions)
                {
                    index++;
                    var transactionHash = transaction["hash"]?.ToString();
                    var logicalTime = transaction["lt"]?.ToString();
                    // Check if the transaction is incoming or outgoing
                    var isOutgoing = transaction["in_msg"]?["source"]?["address"]?.ToString() == _tonChainSettings.WalletAddress;
                    // Decoded body text
                    var decodedBody = transaction["in_msg"]?["decoded_body"]?["text"]?.ToString();
                    var status = transaction["success"]?.ToString();

                    var tran = new TonTransaction()
                    {
                        TransactionHash = transactionHash ?? "",
                        FromAddress = transaction["in_msg"]?["source"]?["address"]?.ToString(),
                        ToAddress = transaction["in_msg"]?["destination"]?["address"]?.ToString(),
                        TransactionType = isOutgoing ? "Sent" : "Received",
                        BodyText = decodedBody ?? "",
                        Currency = "TON",
                        Status = (status?.ToLower() == "true" || status == "1") ? "Success" : "Failed",
                    };

                    // Convert amount from nanoTONs to TONs (divide by 1,000,000,000)
                    var amountStr = transaction["in_msg"]?["value"]?.ToString();
                    if (long.TryParse(amountStr, out long amountNanoTon))
                    {
                        tran.Amount = amountNanoTon;
                        //var amountTon = amountNanoTon / 1_000_000_000m;
                        //var formattedAmount = amountTon.ToString("F2");
                    }
                    // Transaction fee (if available)
                    var feeStr = transaction["total_fees"]?.ToString();
                    if (long.TryParse(feeStr, out long feeNanoTon))
                    {
                        tran.Fee = feeNanoTon;
                        //var feeTon = feeNanoTon / 1_000_000_000m;
                        //var formattedFee = feeTon.ToString("F2");
                    }
                    
                    var existingTran = await _tonTransactionCollection.Find(x => x.TransactionHash == tran.TransactionHash).FirstOrDefaultAsync();
                    if (existingTran != null)
                    {
                        // Update the existing transaction
                        tran.Id = existingTran.Id;
                        tran.IsProcessed = true;
                        tran.CreatedAt = existingTran.CreatedAt;
                        tran.UpdatedAt = DateTime.UtcNow;
                        await _tonTransactionCollection.ReplaceOneAsync(x => x.Id == tran.Id, tran);
                    }
                    else
                    {
                        // Save the transaction to the database
                        if(string.IsNullOrEmpty(tran.BodyText) || tran.Amount <= 0) continue;
                        tran.IsProcessed = true;
                        await _tonTransactionCollection.InsertOneAsync(tran);
                        if (tran.Status == "Success" && tran.Amount >= Constants.GameSettings.PremiumBotPriceInNanoTon)
                        {
                            if (long.TryParse(tran.BodyText, out long telegramId))
                            {
                                var user = await _userCollection.Find(x => x.TelegramId == tran.BodyText).FirstOrDefaultAsync();
                                if (user != null)
                                {
                                    user.HavePremiumBot = true;
                                    user.UpdatedAt = DateTime.UtcNow;
                                    user.PremiumBotAt = DateTime.UtcNow;
                                    user.ReceiveAddress = transaction["in_msg"]?["source"]?["address"]?.ToString();
                                    await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);
                                    
                                    await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
                                    {
                                        Amount = tran.Amount,
                                        Status = "Success",
                                        UserId = user.Id,
                                        TransactionType = InGameTransactionType.TopUp.ToString(),
                                        Currency = "TON"
                                    });
                                    await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
                                    {
                                        Amount = Constants.GameSettings.PremiumBotPriceInNanoTon * -1,
                                        Status = "Success",
                                        UserId = user.Id,
                                        TransactionType = InGameTransactionType.BuyPremium.ToString(),
                                        Currency = "TON",
                                        Description = "Buy Premium Bot"
                                    });
                                    
                                    // + 0.1 TON to the referrer level 1 and 0.05 TON to the referrer level 2
                                    if (Constants.GameSettings.TonRewardForReferralLevel1 > 0)
                                    {
                                        var referrer = await _userCollection.Find(x => x.TelegramId == user.RefererId).FirstOrDefaultAsync();
                                        if (referrer != null)
                                        {
                                            const long ref1Amount = (long)(Constants.GameSettings.TonRewardForReferralLevel1 * Constants.GameSettings.TonInNano);
                                            referrer.TonBalance += ref1Amount;
                                            referrer.UpdatedAt = DateTime.UtcNow;
                                            await _userCollection.ReplaceOneAsync(x => x.Id == referrer.Id, referrer);
                                            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
                                            {
                                                Amount = ref1Amount,
                                                Status = "Success",
                                                UserId = referrer.Id,
                                                TransactionType = InGameTransactionType.ReferralReward.ToString(),
                                                Currency = "TON",
                                                Description = $"Referral reward level 1 from {user.TelegramId} - {user.TelegramUsername}"
                                            });
                                            if (!string.IsNullOrEmpty(referrer.RefererId))
                                            {
                                                var referrer2 = await _userCollection.Find(x => x.TelegramId == referrer.RefererId).FirstOrDefaultAsync();
                                                if (referrer2 != null)
                                                {
                                                    const long ref2Amount = (long)(Constants.GameSettings.TonRewardForReferralLevel2 * Constants.GameSettings.TonInNano);
                                                    referrer2.TonBalance += ref2Amount;
                                                    referrer2.UpdatedAt = DateTime.UtcNow;
                                                    await _userCollection.ReplaceOneAsync(x => x.Id == referrer2.Id, referrer2);
                                                    
                                                    await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
                                                    {
                                                        Amount = ref2Amount,
                                                        Status = "Success",
                                                        UserId = referrer.Id,
                                                        TransactionType = InGameTransactionType.ReferralReward.ToString(),
                                                        Currency = "TON",
                                                        Description = $"Referral reward level 2 from {referrer.TelegramId} - {referrer.TelegramUsername}"
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (index == 1)
                    {
                        // Update the last block time in the cache
                        await _redisCacheService.Set("TONLastBlockTime", logicalTime);
                    }
                }
            Console.WriteLine($"Processed transaction: {index}/{transactions?.Count() ?? 0} transactions");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Request error: " + e.Message);
        }
    }

    public async Task<ResponseDto<bool>> WithdrawAsync(WithdrawRequestViewModel requestViewModel, string userId)
    {
        if(requestViewModel.Amount <= 0)
        {
            return await Task.FromResult(new ResponseDto<bool>
            {
                Success = false,
                Message = "Invalid request data"
            });
        }
        
        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        if (user == null)
        {
            return await Task.FromResult(new ResponseDto<bool>
            {
                Success = false,
                Message = "User not found"
            });
        }

        if (string.IsNullOrEmpty(user.ReceiveAddress)){
            return new ResponseDto<bool> {
                Success = false,
                Message = "You need make a deposit (for example buying AI bot) before making a withdrawal request."
            };
        }

        if(user.TonBalance < requestViewModel.Amount)
        {
            return await Task.FromResult(new ResponseDto<bool>
            {
                Success = false,
                Message = "Insufficient balance"
            });
        }

        if(user.TonBalance < Constants.GameSettings.MinimumWithdrawAmountInNanoTon)
        {
            return await Task.FromResult(new ResponseDto<bool>
            {
                Success = false,
                Message = $"Minimum withdraw amount is {(Constants.GameSettings.MinimumWithdrawAmountInNanoTon/Constants.GameSettings.TonInNano):F1} TON"
            });
        }
        
        var withdrawExist = await _withdrawRequestCollection.Find(c => c.UserId == userId && c.Status == WithdrawRequestStatus.Pending.ToString()).FirstOrDefaultAsync();
        if (withdrawExist != null) 
        {
            return new ResponseDto<bool>
            {
                Success = false,
                Message = "You had a pending withdrawal request. Please wait for it to be completed to make another request. "
            };
        }

        var request = new WithdrawRequest()
        {
            Amount = requestViewModel.Amount,
            Currency = "TON",
            UserId = userId
        };
        
        await _withdrawRequestCollection.InsertOneAsync(request);
        return await Task.FromResult(new ResponseDto<bool>
        {
            Success = true,
            Message = "Withdraw request submitted successfully"
        });
    }

    public Task<ResponseDto<bool>> GetTranStatus(string teleId, string? timestamp = null)
    {
        var fromTime = string.IsNullOrEmpty(timestamp) ? DateTime.UtcNow.AddMinutes(-20) : DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(timestamp)).DateTime;
        var tran = _tonTransactionCollection.Find(x => x.BodyText == teleId && x.CreatedAt >= fromTime).FirstOrDefault();
        if (tran == null)
        {
            return Task.FromResult(new ResponseDto<bool>
            {
                Success = false,
                Message = "Transaction not found"
            });
        }
        return Task.FromResult(new ResponseDto<bool>
        {
            Success = true,
            Message = tran.Status == "Success" ? "Transaction successful" : "Transaction failed",
            Data = tran.Status == "Success"
        });
    }

    public async Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawByUserIdAsync(string userId)
    {
        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        if (user == null) {

            return new ResponseDto<IEnumerable<WithdrawResponseModel>>
            {
                Message = "User is not found",
                Success = false
            };
        }

        var widthdraws = await _withdrawRequestCollection.Find(c => c.UserId == userId).ToListAsync();


        var dataResponse = widthdraws.Select(c => new WithdrawResponseModel 
                                    { 
                                        Address = user.ReceiveAddress, 
                                        Amount = c.Amount, 
                                        Status = c.Status, 
                                        CreatedAt = c.CreatedAt 
                                    });
     
        return new ResponseDto<IEnumerable<WithdrawResponseModel>>
        {
            Success = true,
            Data = dataResponse
        };
    }
}