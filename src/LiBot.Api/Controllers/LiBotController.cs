using System.Text.Json;
using LiBot.Application.Robot;
using LiBot.Contracts.Robot;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/robots/libot")]
public sealed class LiBotController(ILiBotTcpClient tcpClient) : ControllerBase
{
    /// <summary>Moves the LiBot robot to the requested target.</summary>
    /// <param name="request">The target, movement policy, and timeout.</param>
    /// <param name="cancellationToken">Token propagated from the HTTP request.</param>
    /// <returns>The response returned by the Raspberry Pi.</returns>
    [HttpPost("move")]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonElement), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Move(
        [FromBody] MoveLiBotRequest request,
        CancellationToken cancellationToken)
    {
        var tcpRequest = new LiBotRunStepRequest(
            "run_step",
            new LiBotMoveStep(
                "MOVE",
                request.Name,
                new LiBotMoveTarget(request.EndpointId, request.X, request.Y, request.Theta),
                request.SpeedMode,
                request.ObstaclePolicy,
                request.TimeoutSec));

        try
        {
            using var response = await tcpClient.SendAsync(tcpRequest, cancellationToken);
            var responseBody = response.RootElement.Clone();

            if (IsMoveCompleted(responseBody))
            {
                return Ok(responseBody);
            }

            return UnprocessableEntity(responseBody);
        }
        catch (LiBotTcpException exception)
        {
            var (statusCode, title) = exception.Failure switch
            {
                LiBotTcpFailure.ConnectionRefused or LiBotTcpFailure.NetworkError =>
                    (StatusCodes.Status503ServiceUnavailable, "LiBot is unavailable"),
                LiBotTcpFailure.ConnectTimeout or LiBotTcpFailure.ResponseTimeout =>
                    (StatusCodes.Status504GatewayTimeout, "LiBot request timed out"),
                _ => (StatusCodes.Status502BadGateway, "Invalid response from LiBot")
            };

            return Problem(
                statusCode: statusCode,
                title: title,
                detail: exception.Message);
        }
    }

    private static bool IsMoveCompleted(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object
        && response.TryGetProperty("result", out var result)
        && result.ValueKind == JsonValueKind.Object
        && result.TryGetProperty("move_status", out var moveStatus)
        && moveStatus.ValueKind == JsonValueKind.String
        && string.Equals(moveStatus.GetString(), "COMPLETED", StringComparison.OrdinalIgnoreCase);

}
