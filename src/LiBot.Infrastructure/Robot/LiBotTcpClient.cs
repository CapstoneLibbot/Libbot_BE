using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LiBot.Application.Robot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiBot.Infrastructure.Robot;

public sealed class LiBotTcpClient : ILiBotTcpClient
{
    private const int MaximumResponseBytes = 1024 * 1024;
    private const int MaximumLoggedResponseCharacters = 2048;
    private readonly LiBotTcpOptions _options;
    private readonly ILogger<LiBotTcpClient> _logger;

    public LiBotTcpClient(
        IOptions<LiBotTcpOptions> options,
        ILogger<LiBotTcpClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<JsonDocument> SendAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var requestJson = JsonSerializer.Serialize(request);
        var command = GetCommand(requestJson);
        var outcome = "error";

        try
        {
            var isIpAddress = IPAddress.TryParse(_options.Host, out var ipAddress);
            using var client = isIpAddress
                ? new TcpClient(ipAddress!.AddressFamily)
                : new TcpClient();
            var connectTask = isIpAddress
                ? client.ConnectAsync(ipAddress!, _options.Port)
                : client.ConnectAsync(_options.Host, _options.Port);
            try
            {
                await connectTask.WaitAsync(
                    TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds),
                    cancellationToken);
            }
            catch (TimeoutException exception)
            {
                client.Dispose();
                ObserveConnectTask(connectTask);
                throw new LiBotTcpException(
                    LiBotTcpFailure.ConnectTimeout,
                    $"Timed out connecting to LiBot at {_options.Host}:{_options.Port}.",
                    exception);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                client.Dispose();
                ObserveConnectTask(connectTask);
                throw;
            }
            catch (SocketException exception) when (exception.SocketErrorCode == SocketError.ConnectionRefused)
            {
                throw new LiBotTcpException(
                    LiBotTcpFailure.ConnectionRefused,
                    $"LiBot refused the TCP connection at {_options.Host}:{_options.Port}.",
                    exception);
            }
            catch (SocketException exception)
            {
                throw new LiBotTcpException(
                    LiBotTcpFailure.NetworkError,
                    $"A network error occurred connecting to LiBot at {_options.Host}:{_options.Port}.",
                    exception);
            }

            await using var networkStream = client.GetStream();
            await using var limitedStream = new LimitedReadStream(networkStream, MaximumResponseBytes);
            using var reader = new StreamReader(
                limitedStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            using var writer = new StreamWriter(
                networkStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                leaveOpen: true);

            await writer.WriteAsync(requestJson.AsMemory(), cancellationToken);
            await writer.WriteAsync("\n".AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);

            string? response;
            using (var responseTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                responseTimeout.CancelAfter(TimeSpan.FromSeconds(_options.ResponseTimeoutSeconds));
                try
                {
                    response = await reader.ReadLineAsync(responseTimeout.Token);
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new LiBotTcpException(
                        LiBotTcpFailure.ResponseTimeout,
                        $"Timed out waiting for a response from LiBot at {_options.Host}:{_options.Port}.",
                        exception);
                }
                catch (ResponseTooLargeException exception)
                {
                    throw new LiBotTcpException(
                        LiBotTcpFailure.ResponseTooLarge,
                        $"LiBot response exceeded the {MaximumResponseBytes}-byte limit.",
                        exception);
                }
            }

            if (response is null)
            {
                throw new LiBotTcpException(
                    LiBotTcpFailure.EmptyResponse,
                    "LiBot closed the TCP connection without returning a response.");
            }

            try
            {
                var document = JsonDocument.Parse(response);
                outcome = "response received";
                return document;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    "Received invalid JSON from LiBot at {Host}:{Port}. Raw response excerpt: {ResponseExcerpt}",
                    _options.Host,
                    _options.Port,
                    response[..Math.Min(response.Length, MaximumLoggedResponseCharacters)]);

                throw new LiBotTcpException(
                    LiBotTcpFailure.InvalidJsonResponse,
                    "LiBot returned a response that is not valid JSON.",
                    exception);
            }
        }
        catch (LiBotTcpException exception)
        {
            outcome = exception.Failure.ToString();
            throw;
        }
        catch (SocketException exception)
        {
            outcome = LiBotTcpFailure.NetworkError.ToString();
            throw new LiBotTcpException(
                LiBotTcpFailure.NetworkError,
                $"A network error occurred communicating with LiBot at {_options.Host}:{_options.Port}.",
                exception);
        }
        catch (IOException exception)
        {
            outcome = LiBotTcpFailure.NetworkError.ToString();
            throw new LiBotTcpException(
                LiBotTcpFailure.NetworkError,
                $"A network error occurred communicating with LiBot at {_options.Host}:{_options.Port}.",
                exception);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            _logger.LogInformation(
                "LiBot TCP request completed. Host: {Host}, Port: {Port}, Command: {Command}, ElapsedMs: {ElapsedMs}, Result: {Result}",
                _options.Host,
                _options.Port,
                command,
                stopwatch.ElapsedMilliseconds,
                outcome);
        }
    }

    private static string GetCommand(string requestJson)
    {
        using var document = JsonDocument.Parse(requestJson);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("command", out var command)
                ? command.GetString() ?? "unknown"
                : "unknown";
    }

    private static void ObserveConnectTask(Task connectTask)
    {
        _ = connectTask.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed class LimitedReadStream(Stream innerStream, int maximumBytes) : Stream
    {
        private int _remainingBytes = maximumBytes;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            EnsureWithinLimit(buffer.Length);
            var read = innerStream.Read(buffer[..Math.Min(buffer.Length, _remainingBytes)]);
            _remainingBytes -= read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            EnsureWithinLimit(buffer.Length);
            var read = await innerStream.ReadAsync(
                buffer[..Math.Min(buffer.Length, _remainingBytes)],
                cancellationToken);
            _remainingBytes -= read;
            return read;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private void EnsureWithinLimit(int requestedBytes)
        {
            if (requestedBytes > 0 && _remainingBytes == 0)
            {
                throw new ResponseTooLargeException();
            }
        }
    }

    private sealed class ResponseTooLargeException()
        : Exception("The LiBot response exceeded the configured size limit.");
}
