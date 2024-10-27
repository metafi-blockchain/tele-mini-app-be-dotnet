using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OkCoin.API.Enums;
using OkCoin.API.Extensions;
using OkCoin.API.Models;
using OkCoin.API.Options;
using OkCoin.API.Responses;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ITonChainService
{
    Task GetTransactionsAsync();
    Task<ResponseDto<bool>> WithdrawAsync(WithdrawRequestViewModel requestViewModel, string userId);
    Task<ResponseDto<bool>> GetTranStatus(string teleId, string? timestamp = null);
    Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawByUserIdAsync(string userId, ListWithdrawRequestViewModel request);
    Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawAsync(ListWithdrawRequestViewModel request);

    Task MigrateWalletAddressReceiveForOldUserAsync();
}

public class TonChainService : ITonChainService
{
    private readonly IExternalClientService _externalClientService;
    private readonly ICacheService _redisCacheService;
    private readonly TonChainSettings _tonChainSettings;
    private readonly EndpointsOutSideOption _endpointsOutSideOption;
    private readonly IMongoCollection<TonTransaction> _tonTransactionCollection;
    private readonly IMongoCollection<WithdrawRequest> _withdrawRequestCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IStatisticService _statisticService;

    public TonChainService(IOptions<DbSettings> myDatabaseSettings, IExternalClientService externalClientService, ICacheService redisCacheService, IOptions<TonChainSettings> tonChainSettings, IStatisticService statisticService, IOptions<EndpointsOutSideOption> endpointsOutSideOption)
    {
        _externalClientService = externalClientService;
        _redisCacheService = redisCacheService;
        _statisticService = statisticService;
        _tonChainSettings = tonChainSettings.Value;
        _endpointsOutSideOption = endpointsOutSideOption.Value;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _tonTransactionCollection = database.GetCollection<TonTransaction>(nameof(TonTransaction));
        _userCollection = database.GetCollection<User>(nameof(User));
        _withdrawRequestCollection = database.GetCollection<WithdrawRequest>(nameof(WithdrawRequest));
    }

    public async Task GetTransactionsAsync()
    {
        Console.WriteLine($"{nameof(GetTransactionsAsync)} - Start.");
        var lastBlock = await _redisCacheService.GetAsync<string>("TONLastBlockTime");
        
        try
        {
            var transactions = await GetHistoryTransactionFromOutside("0");

            // Iterate through each transaction and extract the required details
            var index = 0;
            if (transactions != null)
                foreach (var transaction in transactions)
                {
                    index++;
                    
                    var tonTransactions = GetTonTransactions(transaction);
                    foreach (var tran in tonTransactions)
                    {
                        Console.WriteLine($"transaction = {JsonConvert.SerializeObject(tran)}");
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

                            if (tran.Status != TransactionState.Success.ToString()) 
                            {
                                continue;
                            }
                            
                            if (tran.TransactionType == TransactionState.Sent.ToString())
                            {
                                // transaction withdraw
                                await UpdateInfoWithTransactionSent(tran);
                            }
                            else
                            {
                                // transaction deposit
                                await UpdateInfoWithTransactionReceived(tran);
                            } 
                        }
                    }

                    if (index == 1)
                    {
                        // Update the last block time in the cache
                        var logicalTime = transaction["lt"]?.ToString();
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

    public async Task<JToken?> GetHistoryTransactionFromOutside(string? lastBlock)
    {
        try
        {
            var baseUrl = _tonChainSettings.IsMainNet ? _endpointsOutSideOption.MainNet : _endpointsOutSideOption.TestNet;
            
            var apiUrl = $"{baseUrl}/v2/blockchain/accounts/{_tonChainSettings.WalletAddress}/transactions?limit=100&sort_order=desc";
            
            if(!string.IsNullOrEmpty(lastBlock)) apiUrl += "&after_lt=" + lastBlock;

            var transactionJson = await _externalClientService.GetDataAsync<JObject>(apiUrl);
            
            var transactions = transactionJson["transactions"];

            return transactions;
        }
        catch (System.Exception ex)
        {
             Console.WriteLine($"{nameof(GetHistoryTransactionFromOutside)} - Error: {ex.Message}");
             return null;
        } 
    }

    private List<TonTransaction> GetTonTransactions(JToken transaction, bool isGetAll = true)
    {
        var trans = new List<TonTransaction>();
        var transactionHash = transaction["hash"]?.ToString();
        var outMessage = transaction["out_msgs"];
        var isOutgoing = outMessage != null && outMessage.Any();
        var status = transaction["success"]?.ToString();
        var transactionType = isOutgoing ? TransactionState.Sent.ToString() : TransactionState.Received.ToString();
        var fee = transaction["total_fees"].ToString().StringToLong();

        if (isOutgoing)
        {
            if (!isGetAll) return trans;

            // sent
            trans.AddRange(outMessage.Select(c => new TonTransaction{
                TransactionHash = transactionHash ?? "",
                FromAddress = c["source"]?["address"]?.ToString(),
                ToAddress = c["destination"]?["address"]?.ToString(),
                TransactionType = transactionType,
                BodyText = c["decoded_body"]?["text"]?.ToString() ?? "",
                Currency = "TON",
                Status = (status?.ToLower() == "true" || status == "1") ? TransactionState.Success.ToString() : TransactionState.Failed.ToString(),
                Amount = c["value"].ToString().StringToLong(),
                Fee = fee
            }));
        }
        else
        {
            // received

            var amountStr = transaction["in_msg"]?["value"]?.ToString();

            trans.Add(new TonTransaction{
                TransactionHash = transactionHash ?? "",
                FromAddress = transaction["in_msg"]?["source"]?["address"]?.ToString(),
                ToAddress = transaction["in_msg"]?["destination"]?["address"]?.ToString(),
                TransactionType = isOutgoing ? TransactionState.Sent.ToString() : TransactionState.Received.ToString(),
                BodyText = transaction["in_msg"]?["decoded_body"]?["text"]?.ToString() ?? "",
                Currency = "TON",
                Status = (status?.ToLower() == "true" || status == "1") ? TransactionState.Success.ToString() : TransactionState.Failed.ToString(),
                Amount = amountStr.StringToLong(),
                Fee = fee
            });
        }  
        
        return trans;
    }

    private async Task UpdateInfoWithTransactionSent(TonTransaction tran)
    {
        Console.WriteLine($"{nameof(UpdateInfoWithTransactionSent)} - Start");
        var user = await _userCollection.Find(x => x.TelegramId == tran.BodyText).FirstOrDefaultAsync();
        if (user is null) return;
        
        // update status in list withdraw by userId
        var withdrawRequest = await _withdrawRequestCollection.Find(c => c.UserId == user.Id && c.Status == WithdrawRequestStatus.Pending.ToString() && c.Amount <= tran.Amount).FirstOrDefaultAsync();
        
        if (withdrawRequest is not null)
        {
            Console.WriteLine($"Withdraw request = {JsonConvert.SerializeObject(withdrawRequest)}");

            user.TonBalance = user.TonBalance < tran.Amount ? 0 : user.TonBalance - tran.Amount;

            withdrawRequest.Status = WithdrawRequestStatus.Completed.ToString();

            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            await _withdrawRequestCollection.ReplaceOneAsync(c => c.Id == withdrawRequest.Id, withdrawRequest);

            await _statisticService.LogInGameTransactionAsync(new InGameTransaction()
            {
                Amount = tran.Amount,
                Status = "Success",
                UserId = user.Id,
                TransactionType = InGameTransactionType.PayCommission.ToString(),
                Currency = "TON"
            });
        }
         Console.WriteLine($"{nameof(UpdateInfoWithTransactionSent)} - End.");
    }

    private async Task UpdateInfoWithTransactionReceived(TonTransaction tran)
    {
        Console.WriteLine($"{nameof(UpdateInfoWithTransactionReceived)} - Start");
        if (tran.Amount >= Constants.GameSettings.PremiumBotPriceInNanoTon)
        {
            if (long.TryParse(tran.BodyText, out long telegramId))
            {
                var user = await _userCollection.Find(x => x.TelegramId == tran.BodyText).FirstOrDefaultAsync();
                
                if (user != null)
                {
                    user.HavePremiumBot = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    user.PremiumBotAt = DateTime.UtcNow;

                    var address = await GetWalletAddressDetailFromOutside(tran.FromAddress);
                    user.ReceiveAddress = JsonConvert.SerializeObject(address);

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
                        await TonRewardForReferral(user);    
                    }
                }
            }
        }        

        Console.WriteLine($"{nameof(UpdateInfoWithTransactionReceived)} - End");
    }

    private async Task<AddressModel> GetWalletAddressDetailFromOutside(string address)
    {
        var response = new AddressModel{Hex = address};
        try
        {
            var endpoint = $"{_endpointsOutSideOption.WalletAddressDetail}/{address}";
            var addressDetail = await _externalClientService.GetDataAsync<JObject>(endpoint);
            if (addressDetail != null)
            {
                response.MainNet = addressDetail["mainnet"]?["base64urlsafe"]?["non_bounceable"].ToString();
                response.TestNet = addressDetail["testnet"]?["base64urlsafe"]?["non_bounceable"].ToString();
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"{nameof(GetWalletAddressDetailFromOutside)} - Error: {ex.Message}");
        }

        return response;
    }

    private async Task TonRewardForReferral(User user)
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
            Address = user.ReceiveAddress,
            Amount = requestViewModel.Amount,
            Currency = "TON",
            UserId = userId,
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

    public async Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawByUserIdAsync(string userId, ListWithdrawRequestViewModel request)
    {
        var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        if (user == null) {

            return new ResponseDto<IEnumerable<WithdrawResponseModel>>
            {
                Message = "User is not found",
                Success = false
            };
        }

        var withdraws = request.IsGetAll
                        ? await _withdrawRequestCollection.Find(c => c.UserId == userId).ToListAsync()
                        : await _withdrawRequestCollection.Find(c => c.UserId == userId && c.Status.ToLower() == request.Status.ToLower()).ToListAsync();

        if (!withdraws.Any())
        {
            return new ResponseDto<IEnumerable<WithdrawResponseModel>>
            {
                Success = true,
                Data = Enumerable.Empty<WithdrawResponseModel>()
            };
        }

        var dataResponse = withdraws.Select(c =>  new WithdrawResponseModel 
                                    { 
                                        TelegramId = user.TelegramId,
                                        FirstName = user.FirstName,
                                        LastName = user.LastName,
                                        Address = c.Address, 
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

    public async Task<ResponseDto<IEnumerable<WithdrawResponseModel>>> GetListWithdrawAsync(ListWithdrawRequestViewModel request)
    {
        var withdraws = request.IsGetAll
                        ? await _withdrawRequestCollection.Find(_ => true).ToListAsync()
                        : await _withdrawRequestCollection.Find(c => c.Status.ToLower() == request.Status.ToLower()).ToListAsync();


        if (!withdraws.Any())
        {
            return new ResponseDto<IEnumerable<WithdrawResponseModel>>
            {
                Success = true,
                Data = Enumerable.Empty<WithdrawResponseModel>()
            };
        }

        var userIds = withdraws.Select(c => c.UserId).Distinct();

        var users = await _userCollection.Find(c => userIds.Contains(c.Id)).ToListAsync();

        var dataResponse = withdraws.Select(c => {
            var user = users.First(u => u.Id == c.UserId);
            return new WithdrawResponseModel
            {
                TelegramId = user.TelegramId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = c.Address,
                Amount = c.Amount,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            };
        });

        return new ResponseDto<IEnumerable<WithdrawResponseModel>>
        {
            Success = true,
            Data = dataResponse
        };
    }

    public async Task MigrateWalletAddressReceiveForOldUserAsync()
    {
        Console.WriteLine($"{nameof(MigrateWalletAddressReceiveForOldUserAsync)} - Start");
        try
        {
            var transactions = await GetHistoryTransactionFromOutside("0");

            if (transactions == null)
            {
                Console.WriteLine($"{nameof(MigrateWalletAddressReceiveForOldUserAsync)} - transactions are empty.");
                return;
            }

            foreach (var transaction in transactions)
            {
                var tonTransactions = GetTonTransactions(transaction, false);

                foreach (var tran in tonTransactions)
                {
                    if(tran.Status != TransactionState.Success.ToString() 
                        || tran.TransactionType != TransactionState.Received.ToString()
                        || string.IsNullOrEmpty(tran.BodyText) 
                        || tran.Amount < Constants.GameSettings.PremiumBotPriceInNanoTon) 
                    {
                        continue;
                    }
                    
                    var user = await _userCollection.Find(x => x.TelegramId == tran.BodyText).FirstOrDefaultAsync();
                    
                    if (user == null)
                    {
                        continue;
                    }

                    var address = await GetWalletAddressDetailFromOutside(tran.FromAddress);
                    user.ReceiveAddress = JsonConvert.SerializeObject(address);   

                    await _userCollection.ReplaceOneAsync(c => c.Id == user.Id, user);
                }
                
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"{nameof(MigrateWalletAddressReceiveForOldUserAsync)} - Error: {ex.Message}");
        }
    }
}