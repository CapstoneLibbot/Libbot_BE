namespace LiBot.Application.Models;

public class BaseResponse<T>(T? Data, string? Message = null, bool Success = true)
{
    public bool Success { get; } = Success;
    public string? Message { get; } = Message;
    public T? Data { get; } = Data;

    public static BaseResponse<T> Failure(string message) => new(default, message, false);

    public static BaseResponse<T> SuccessMessage(string message) => new(default, message, true);
}
