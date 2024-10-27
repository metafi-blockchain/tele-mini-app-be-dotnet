
namespace OkCoin.API.Extensions;

public static class StringExtension
{
    public static long StringToLong(this string? str)
    {
         if (long.TryParse(str, out long amountNanoTon))
        {
            return amountNanoTon;
        }

        return 0;
    }    
}
