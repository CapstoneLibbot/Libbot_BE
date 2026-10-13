using System.Text.Json;

namespace LiBot.Application.Robot;

public interface ILiBotTcpClient
{
    Task<JsonDocument> SendAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default);
}
