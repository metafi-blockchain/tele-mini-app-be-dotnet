namespace OkCoin.API.ViewModels;

public class ResponseDto<T>
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; } = false;
    public T Data { get; set; } = default!;
}