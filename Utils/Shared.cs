using OkCoin.API.Models;

namespace OkCoin.API.Utils;

public static class Shared
{
    public static int CalculateAvailableTap(this User user)
    {
        var calculatedTotalSecondFromLastTapped = (int)(DateTime.UtcNow - user.LastTapped).TotalSeconds * user.RechargeSpeedValue;
        var totalAvailableTap = (int)(user.AvailableTapCount + calculatedTotalSecondFromLastTapped);
        if(totalAvailableTap > user.EnergyLimitValue) totalAvailableTap = user.EnergyLimitValue;
        return totalAvailableTap;
    }
}