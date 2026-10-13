using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LiBot.Api.Controllers;
using LiBot.Api.Models;
using LiBot.Application.Robot;
using LiBot.Contracts.Robot;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LiBot.Infrastructure.Tests;

public sealed class LiBotTcpClientTests
{
    private const string SuccessfulResponse =
        """{"status":"ok","step":"move_test","step_type":"MOVE","result":{"status":"ok","type":"MOVE","move_status":"COMPLETED","target":{"x":-0.8,"y":-0.7,"theta":0,"endpoint_id":"TEST_POINT_2"},"final_pose":null,"nav2_result_code":"SUCCEEDED","duration_ms":15000}}""";

    [Fact]
    public async Task SendAsync_WritesSnakeCaseJsonEndingWithNewline()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            var requestLine = await ReadLineAsync(connection.GetStream());
            await WriteResponseAsync(connection.GetStream(), SuccessfulResponse + "\n");
            return requestLine;
        });
        var client = CreateClient(port);

        using var response = await client.SendAsync(CreateRequest("move_test"));
        var requestLine = await server;
        using var request = JsonDocument.Parse(requestLine);

        Assert.Equal("ok", response.RootElement.GetProperty("status").GetString());
        Assert.Equal("run_step", request.RootElement.GetProperty("command").GetString());
        var step = request.RootElement.GetProperty("step");
        Assert.Equal("MOVE", step.GetProperty("type").GetString());
        Assert.Equal("move_test", step.GetProperty("name").GetString());
        Assert.Equal("TEST_POINT_2", step.GetProperty("target").GetProperty("endpoint_id").GetString());
        Assert.Equal("NORMAL", step.GetProperty("speed_mode").GetString());
        Assert.Equal("WAIT_RESUME", step.GetProperty("obstacle_policy").GetString());
        Assert.Equal(60, step.GetProperty("timeout_sec").GetInt32());
        Assert.DoesNotContain("endpointId", requestLine);
    }

    [Fact]
    public async Task SendAsync_ReadsResponseSplitAcrossMultipleWrites()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            _ = await ReadLineAsync(connection.GetStream());
            var responseBytes = Encoding.UTF8.GetBytes(SuccessfulResponse + "\n");
            await connection.GetStream().WriteAsync(responseBytes.AsMemory(0, 12));
            await connection.GetStream().FlushAsync();
            await Task.Delay(40);
            await connection.GetStream().WriteAsync(responseBytes.AsMemory(12));
            await connection.GetStream().FlushAsync();
        });

        using var response = await CreateClient(port).SendAsync(CreateRequest("fragmented"));
        await server;

        Assert.Equal("COMPLETED", response.RootElement.GetProperty("result").GetProperty("move_status").GetString());
    }

    [Fact]
    public async Task SendAsync_ThrowsResponseTimeoutWhenPiDoesNotRespond()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            _ = await ReadLineAsync(connection.GetStream());
            await Task.Delay(TimeSpan.FromSeconds(2));
        });

        var exception = await Assert.ThrowsAsync<LiBotTcpException>(
            () => CreateClient(port, responseTimeoutSeconds: 1).SendAsync(CreateRequest("timeout")));
        await server;

        Assert.Equal(LiBotTcpFailure.ResponseTimeout, exception.Failure);
    }

    [Fact]
    public async Task SendAsync_ReportsClosedPortConnectionFailure()
    {
        using var listener = StartListener(out var port);
        listener.Stop();

        var exception = await Assert.ThrowsAsync<LiBotTcpException>(
            () => CreateClient(port).SendAsync(CreateRequest("refused")));

        Assert.True(
            exception.Failure is LiBotTcpFailure.ConnectionRefused or LiBotTcpFailure.ConnectTimeout,
            $"{exception.Failure} ({exception.Message}): {exception.InnerException}");
    }

    [Fact]
    public async Task SendAsync_ReportsInvalidJsonResponse()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            _ = await ReadLineAsync(connection.GetStream());
            await WriteResponseAsync(connection.GetStream(), "not-json\n");
        });

        var exception = await Assert.ThrowsAsync<LiBotTcpException>(
            () => CreateClient(port).SendAsync(CreateRequest("invalid-json")));
        await server;

        Assert.Equal(LiBotTcpFailure.InvalidJsonResponse, exception.Failure);
    }

    [Fact]
    public async Task SendAsync_ReportsWhenPiClosesWithoutResponse()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            _ = await ReadLineAsync(connection.GetStream());
        });

        var exception = await Assert.ThrowsAsync<LiBotTcpException>(
            () => CreateClient(port).SendAsync(CreateRequest("closed")));
        await server;

        Assert.Equal(LiBotTcpFailure.EmptyResponse, exception.Failure);
    }

    [Fact]
    public async Task Move_MapsApiRequestAndReturnsWholePiFailure()
    {
        const string failedResponse =
            """{"status":"error","step":"move_test","result":{"move_status":"FAILED","nav2_result_code":"ABORTED","message":"blocked","duration_ms":25,"final_pose":null}}""";
        var controllerTcpClient = new FixedResponseTcpClient(failedResponse);
        var controller = new LiBotController(controllerTcpClient);
        var request = new MoveLiBotRequest
        {
            Name = "move_test",
            EndpointId = "TEST_POINT_2",
            X = -0.8,
            Y = -0.7,
            Theta = 0,
            SpeedMode = "NORMAL",
            ObstaclePolicy = "WAIT_RESUME",
            TimeoutSec = 60
        };

        var actionResult = await controller.Move(request, CancellationToken.None);
        var result = Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
        var body = Assert.IsType<JsonElement>(result.Value);

        Assert.Equal("error", body.GetProperty("status").GetString());
        Assert.Equal("ABORTED", body.GetProperty("result").GetProperty("nav2_result_code").GetString());
        Assert.Equal("blocked", body.GetProperty("result").GetProperty("message").GetString());
        Assert.Equal(25, body.GetProperty("result").GetProperty("duration_ms").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("result").GetProperty("final_pose").ValueKind);

        using var sentRequest = JsonDocument.Parse(Assert.IsType<string>(controllerTcpClient.LastRequestJson));
        Assert.Equal("run_step", sentRequest.RootElement.GetProperty("command").GetString());
        var sentStep = sentRequest.RootElement.GetProperty("step");
        Assert.Equal("MOVE", sentStep.GetProperty("type").GetString());
        Assert.Equal("move_test", sentStep.GetProperty("name").GetString());
        Assert.Equal("TEST_POINT_2", sentStep.GetProperty("target").GetProperty("endpoint_id").GetString());
        Assert.Equal("NORMAL", sentStep.GetProperty("speed_mode").GetString());
        Assert.Equal("WAIT_RESUME", sentStep.GetProperty("obstacle_policy").GetString());
        Assert.Equal(60, sentStep.GetProperty("timeout_sec").GetInt32());
    }

    [Fact]
    public async Task Move_ReturnsOkWhenPiCompletesTheMove()
    {
        var controller = new LiBotController(new FixedResponseTcpClient(SuccessfulResponse));
        var request = CreateValidApiRequest();

        var result = await controller.Move(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Move_ReturnsUnprocessableEntityForTopLevelPiError()
    {
        const string failedResponse =
            """{"status":"error","message":"bad command","nav2_result_code":"ABORTED","duration_ms":25,"final_pose":null}""";
        var controller = new LiBotController(new FixedResponseTcpClient(failedResponse));

        var actionResult = await controller.Move(CreateValidApiRequest(), CancellationToken.None);

        var result = Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
        var body = Assert.IsType<JsonElement>(result.Value);
        Assert.Equal("error", body.GetProperty("status").GetString());
        Assert.Equal("bad command", body.GetProperty("message").GetString());
        Assert.Equal("ABORTED", body.GetProperty("nav2_result_code").GetString());
        Assert.Equal(25, body.GetProperty("duration_ms").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("final_pose").ValueKind);
    }

    [Theory]
    [InlineData(LiBotTcpFailure.ConnectionRefused, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(LiBotTcpFailure.NetworkError, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(LiBotTcpFailure.ConnectTimeout, StatusCodes.Status504GatewayTimeout)]
    [InlineData(LiBotTcpFailure.ResponseTimeout, StatusCodes.Status504GatewayTimeout)]
    public async Task Move_MapsTcpFailuresToHttpStatusCodes(
        LiBotTcpFailure failure,
        int expectedStatusCode)
    {
        var controller = new LiBotController(new ThrowingTcpClient(failure));

        var actionResult = await controller.Move(CreateValidApiRequest(), CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(expectedStatusCode, result.StatusCode);
        Assert.Equal(expectedStatusCode, problem.Status);
    }

    [Theory]
    [InlineData(" ", "TEST_POINT_2", nameof(MoveLiBotRequest.Name))]
    [InlineData("move_test", "\t", nameof(MoveLiBotRequest.EndpointId))]
    public void MoveLiBotRequest_RejectsWhitespaceOnlyNameAndEndpointId(
        string name,
        string endpointId,
        string expectedMemberName)
    {
        var request = CreateValidApiRequest() with
        {
            Name = name,
            EndpointId = endpointId
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(
            results,
            result => result.MemberNames.Contains(expectedMemberName));
    }

    [Fact]
    public async Task SendAsync_PropagatesCancellation()
    {
        using var listener = StartListener(out var port);
        var receivedRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync();
            _ = await ReadLineAsync(connection.GetStream());
            receivedRequest.SetResult();
            var buffer = new byte[1];
            _ = await connection.GetStream().ReadAsync(buffer);
        });

        using var cancellation = new CancellationTokenSource();
        var send = CreateClient(port).SendAsync(CreateRequest("cancel"), cancellation.Token);
        await receivedRequest.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        await server;
    }

    [Fact]
    public async Task SendAsync_HandlesConcurrentRequestsWithoutMixingResponses()
    {
        using var listener = StartListener(out var port);
        var server = Task.Run(async () =>
        {
            var handlers = new List<Task>();
            for (var index = 0; index < 2; index++)
            {
                var connection = await listener.AcceptTcpClientAsync();
                handlers.Add(HandleConcurrentRequestAsync(connection));
            }

            await Task.WhenAll(handlers);
        });
        var client = CreateClient(port);

        var responses = await Task.WhenAll(
            client.SendAsync(CreateRequest("alpha")),
            client.SendAsync(CreateRequest("beta")));
        await server;

        try
        {
            var returnedSteps = responses
                .Select(response => response.RootElement.GetProperty("step").GetString())
                .Order()
                .ToArray();
            Assert.Equal(new[] { "alpha", "beta" }, returnedSteps);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task SendAsync_PropagatesCallerCancellationDuringConnect()
    {
        using var listener = StartListener(out var port);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateClient(port).SendAsync(CreateRequest("cancel-connect"), cancellation.Token));
    }

    private static async Task HandleConcurrentRequestAsync(TcpClient connection)
    {
        using (connection)
        {
            var requestLine = await ReadLineAsync(connection.GetStream());
            using var request = JsonDocument.Parse(requestLine);
            var name = request.RootElement.GetProperty("step").GetProperty("name").GetString();
            var response = JsonSerializer.Serialize(new
            {
                status = "ok",
                step = name,
                result = new { move_status = "COMPLETED" }
            }) + "\n";
            await WriteResponseAsync(connection.GetStream(), response);
        }
    }

    private static LiBotTcpClient CreateClient(int port, int responseTimeoutSeconds = 5) =>
        new(
            Options.Create(new LiBotTcpOptions
            {
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                ConnectTimeoutSeconds = 2,
                ResponseTimeoutSeconds = responseTimeoutSeconds
            }),
            NullLogger<LiBotTcpClient>.Instance);

    private static LiBotRunStepRequest CreateRequest(string name) =>
        new(
            "run_step",
            new LiBotMoveStep(
                "MOVE",
                name,
                new LiBotMoveTarget("TEST_POINT_2", -0.8, -0.7, 0),
                "NORMAL",
                "WAIT_RESUME",
                60));

    private static MoveLiBotRequest CreateValidApiRequest() =>
        new()
        {
            Name = "move_test",
            EndpointId = "TEST_POINT_2",
            X = -0.8,
            Y = -0.7,
            Theta = 0,
            SpeedMode = "NORMAL",
            ObstaclePolicy = "WAIT_RESUME",
            TimeoutSec = 60
        };

    private static TcpListener StartListener(out int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadLineAsync() ?? throw new InvalidOperationException("Client closed before sending a line.");
    }

    private static async Task WriteResponseAsync(NetworkStream stream, string response)
    {
        var bytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    private sealed class FixedResponseTcpClient(string response) : ILiBotTcpClient
    {
        public string? LastRequestJson { get; private set; }

        public Task<JsonDocument> SendAsync<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequestJson = JsonSerializer.Serialize(request);
            return Task.FromResult(JsonDocument.Parse(response));
        }
    }

    private sealed class ThrowingTcpClient(LiBotTcpFailure failure) : ILiBotTcpClient
    {
        public Task<JsonDocument> SendAsync<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromException<JsonDocument>(
                new LiBotTcpException(failure, $"Simulated {failure} failure."));
    }
}
