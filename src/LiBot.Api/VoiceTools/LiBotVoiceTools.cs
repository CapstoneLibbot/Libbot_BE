using System.ComponentModel;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Application.Workflow.FacilityGuidance;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Options;

namespace LiBot.Api.VoiceTools;

/// <summary>Allow-listed voice tools available to the Xiaozhi LLM.</summary>
[McpServerToolType]
public sealed class LiBotVoiceTools(
    IBookGuidanceService bookGuidance,
    IFacilityGuidanceService facilityGuidance,
    IMissionDataService missionData,
    IRobotRuntimeService runtimes,
    IConfiguration configuration,
    IOptions<VoiceToolsOptions> options)
{
    private const int ResultLimit = 3;
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(5);

    [McpServerTool(Name = "search_books", UseStructuredContent = true)]
    [Description("Tìm đầu sách theo tên sách. Không dùng để tìm theo tác giả hoặc danh mục. Kết quả đã giới hạn tối đa ba đầu sách.")]
    public async Task<VoiceBookSearchResult> SearchBooksAsync(
        [Description("Tên sách cần tìm.")] string? query = null)
    {
        if (!TryNormalizeQuery(query, out var normalizedQuery))
            return VoiceBookSearchResult.InvalidInput();

        try
        {
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var response = await bookGuidance.SearchTitlesAsync(
                normalizedQuery,
                page: 1,
                pageSize: ResultLimit,
                cancellationToken: timeout.Token);

            return new VoiceBookSearchResult(
                ErrorCode: string.Empty,
                TotalMatches: response.TotalCount,
                Books: response.Books.Select(book => new VoiceBookSearchItem(
                    book.BookId,
                    book.Title,
                    book.Author,
                    book.AvailableCopies > 0)).ToList());
        }
        catch (OperationCanceledException)
        {
            return VoiceBookSearchResult.Timeout();
        }
    }

    [McpServerTool(Name = "search_locations", UseStructuredContent = true)]
    [Description("Tìm địa điểm trong thư viện theo đúng tên Endpoint, ví dụ Phòng họp hoặc Bàn thủ thư. Kết quả đã giới hạn tối đa ba Endpoint.")]
    public async Task<VoiceLocationSearchResult> SearchLocationsAsync(
        [Description("Tên địa điểm/Endpoint cần tìm.")] string? query = null)
    {
        if (!TryNormalizeQuery(query, out var normalizedQuery))
            return VoiceLocationSearchResult.InvalidInput();

        try
        {
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var response = await facilityGuidance.SearchAsync(
                normalizedQuery, "FUZZY", page: 1, pageSize: ResultLimit, cancellationToken: timeout.Token);
            return new VoiceLocationSearchResult(
                string.Empty,
                response.TotalCount,
                response.Facilities.Select(location => new VoiceLocationSearchItem(
                    location.FacilityId, location.Name, true)).ToList());
        }
        catch (OperationCanceledException)
        {
            return VoiceLocationSearchResult.Timeout();
        }
    }

    [McpServerTool(Name = "start_book_guidance", UseStructuredContent = true)]
    [Description("Bắt đầu dẫn người dùng đến một đầu sách đã được tìm thấy. Chỉ gọi sau khi người dùng đã chọn sách và đồng ý dẫn đường.")]
    public async Task<VoiceBookGuidanceResult> StartBookGuidanceAsync(
        [Description("ID của đầu sách mà người dùng đã chọn từ search_books.")] Guid bookId,
        [Description("Câu tìm kiếm ban đầu, chỉ dùng để ghi nhận mission.")] string? query = null)
    {
        if (!TryGetGuidanceConfiguration(out var robotId, out var runtimeId))
            return VoiceBookGuidanceResult.Unavailable();

        try
        {
            if (!await missionData.RobotExistsAsync(robotId, CancellationToken.None))
                return VoiceBookGuidanceResult.Unavailable();

            var book = await bookGuidance.GetBookAsync(bookId, CancellationToken.None);
            if (book is null) return VoiceBookGuidanceResult.NotFound();

            var selectedCopy = book.BookItems.FirstOrDefault(item =>
                string.Equals(item.Status, "Available", StringComparison.OrdinalIgnoreCase) &&
                item.Location?.Endpoint is { } endpoint &&
                double.IsFinite(endpoint.X) && double.IsFinite(endpoint.Y) && double.IsFinite(endpoint.Theta));
            if (selectedCopy is null) return VoiceBookGuidanceResult.NotAvailable();

            if (runtimes.ValidateVoiceGuidanceStart(runtimeId) is not null)
                return VoiceBookGuidanceResult.Unavailable();

            var backendBaseUrl = configuration["LiBot:BackendBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var result = await bookGuidance.StartAsync(new StartBookGuidanceRequest(
                string.IsNullOrWhiteSpace(query) ? book.Title : query.Trim(),
                robotId,
                BackendBaseUrl: backendBaseUrl,
                RuntimeId: runtimeId,
                BookId: book.BookId,
                BookItemId: selectedCopy.Id), backendBaseUrl, timeout.Token);

            return result.IsAccepted
                ? VoiceBookGuidanceResult.Started(book.Title, result.MissionId, runtimeId)
                : VoiceBookGuidanceResult.Failed(book.Title, result.MissionId, runtimeId);
        }
        catch (OperationCanceledException)
        {
            return VoiceBookGuidanceResult.Unavailable();
        }
        catch (BookGuidanceRobotUnavailableException)
        {
            return VoiceBookGuidanceResult.Unavailable();
        }
        catch (BookGuidanceWorkflowConfigurationException)
        {
            return VoiceBookGuidanceResult.Unavailable();
        }
    }

    [McpServerTool(Name = "start_location_guidance", UseStructuredContent = true)]
    [Description("Bắt đầu dẫn người dùng đến Endpoint đã tìm thấy. Chỉ gọi sau khi người dùng đã chọn địa điểm và đồng ý dẫn đường.")]
    public async Task<VoiceLocationGuidanceResult> StartLocationGuidanceAsync(
        [Description("ID Endpoint đã chọn từ search_locations.")] Guid endpointId,
        [Description("Câu tìm kiếm ban đầu, chỉ dùng để ghi nhận mission.")] string? query = null)
    {
        if (!TryGetGuidanceConfiguration(out var robotId, out var runtimeId))
            return VoiceLocationGuidanceResult.Unavailable();

        try
        {
            if (!await missionData.RobotExistsAsync(robotId, CancellationToken.None))
                return VoiceLocationGuidanceResult.Unavailable();

            var location = await facilityGuidance.GetFacilityAsync(endpointId, CancellationToken.None);
            if (location is null) return VoiceLocationGuidanceResult.NotFound();
            var endpoint = location.Location.Endpoint;
            if (!double.IsFinite(endpoint.X) || !double.IsFinite(endpoint.Y) || !double.IsFinite(endpoint.Theta)
                || runtimes.ValidateVoiceGuidanceStart(runtimeId) is not null)
                return VoiceLocationGuidanceResult.Unavailable();

            var backendBaseUrl = configuration["LiBot:BackendBaseUrl"]?.TrimEnd('/') ?? string.Empty;
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var result = await facilityGuidance.StartAsync(new StartFacilityGuidanceRequest(
                string.IsNullOrWhiteSpace(query) ? location.Name : query.Trim(),
                robotId,
                BackendBaseUrl: backendBaseUrl,
                RuntimeId: runtimeId,
                FacilityId: endpointId), backendBaseUrl, timeout.Token);
            return result.IsAccepted
                ? VoiceLocationGuidanceResult.Started(location.Name, result.MissionId, runtimeId)
                : VoiceLocationGuidanceResult.Failed(location.Name, result.MissionId, runtimeId);
        }
        catch (OperationCanceledException)
        {
            return VoiceLocationGuidanceResult.Unavailable();
        }
        catch (FacilityGuidanceRobotUnavailableException)
        {
            return VoiceLocationGuidanceResult.Unavailable();
        }
        catch (FacilityGuidanceWorkflowConfigurationException)
        {
            return VoiceLocationGuidanceResult.Unavailable();
        }
    }

    private bool TryGetGuidanceConfiguration(out Guid robotId, out string runtimeId)
    {
        robotId = Guid.Empty;
        runtimeId = options.Value.GuidanceRuntimeId?.Trim() ?? string.Empty;
        return Guid.TryParse(options.Value.GuidanceRobotId, out robotId)
            && robotId != Guid.Empty
            && !string.IsNullOrWhiteSpace(runtimeId);
    }

    private static bool TryNormalizeQuery(string? query, out string normalizedQuery)
    {
        normalizedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        return normalizedQuery.Length is > 0 and <= 100;
    }

}

public sealed record VoiceBookSearchResult(
    string ErrorCode,
    int TotalMatches,
    IReadOnlyList<VoiceBookSearchItem> Books)
{
    public static VoiceBookSearchResult InvalidInput() => new("INVALID_INPUT", 0, []);
    public static VoiceBookSearchResult Timeout() => new("TOOL_TIMEOUT", 0, []);
}

public sealed record VoiceBookSearchItem(
    Guid BookId,
    string Title,
    string Author,
    bool CanGuide);

public sealed record VoiceLocationSearchResult(
    string ErrorCode,
    int TotalMatches,
    IReadOnlyList<VoiceLocationSearchItem> Locations)
{
    public static VoiceLocationSearchResult InvalidInput() => new("INVALID_INPUT", 0, []);
    public static VoiceLocationSearchResult Timeout() => new("TOOL_TIMEOUT", 0, []);
}

public sealed record VoiceLocationSearchItem(Guid EndpointId, string Name, bool CanGuide);

public sealed record VoiceBookGuidanceResult(
    string ErrorCode,
    string Title,
    Guid? JobId,
    string? ExecutionId,
    string? RuntimeId)
{
    public static VoiceBookGuidanceResult Started(string title, Guid jobId, string runtimeId) =>
        new(string.Empty, title, jobId, jobId.ToString(), runtimeId);
    public static VoiceBookGuidanceResult Failed(string title, Guid jobId, string runtimeId) =>
        new("GUIDANCE_FAILED", title, jobId, jobId.ToString(), runtimeId);
    public static VoiceBookGuidanceResult NotFound() => new("BOOK_NOT_FOUND", string.Empty, null, null, null);
    public static VoiceBookGuidanceResult NotAvailable() => new("BOOK_NOT_AVAILABLE", string.Empty, null, null, null);
    public static VoiceBookGuidanceResult Unavailable() => new("GUIDANCE_UNAVAILABLE", string.Empty, null, null, null);
}

public sealed record VoiceLocationGuidanceResult(
    string ErrorCode,
    string Name,
    Guid? JobId,
    string? ExecutionId,
    string? RuntimeId)
{
    public static VoiceLocationGuidanceResult Started(string name, Guid jobId, string runtimeId) =>
        new(string.Empty, name, jobId, jobId.ToString(), runtimeId);
    public static VoiceLocationGuidanceResult Failed(string name, Guid jobId, string runtimeId) =>
        new("GUIDANCE_FAILED", name, jobId, jobId.ToString(), runtimeId);
    public static VoiceLocationGuidanceResult NotFound() => new("LOCATION_NOT_FOUND", string.Empty, null, null, null);
    public static VoiceLocationGuidanceResult Unavailable() => new("GUIDANCE_UNAVAILABLE", string.Empty, null, null, null);
}
