using System.ComponentModel;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Workflow.BookGuidance;
using ModelContextProtocol.Server;

namespace LiBot.Api.VoiceTools;

/// <summary>Allow-listed, read-only tools available to the Xiaozhi LLM.</summary>
[McpServerToolType]
public sealed class LiBotVoiceTools(
    IBookGuidanceService bookGuidance,
    IVoiceAreaSearchService areaSearch)
{
    private const int ResultLimit = 3;
    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(5);

    [McpServerTool(Name = "search_books", UseStructuredContent = true)]
    [Description("Tìm đầu sách theo tên, tác giả, ISBN hoặc danh mục. Chỉ dùng khi người dùng hỏi về sách trong thư viện. Kết quả đã giới hạn tối đa ba đầu sách.")]
    public async Task<VoiceBookSearchResult> SearchBooksAsync(
        [Description("Tên sách, tên tác giả hoặc ISBN cần tìm.")] string query,
        [Description("Danh mục sách nếu người dùng nêu rõ; bỏ trống nếu không có.")] string? category = null)
    {
        if (!TryNormalizeQuery(query, out var normalizedQuery))
            return VoiceBookSearchResult.InvalidInput();

        if (!string.IsNullOrWhiteSpace(category) && category.Trim().Length > 100)
            return VoiceBookSearchResult.InvalidInput();

        try
        {
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var response = await bookGuidance.SearchAsync(
                normalizedQuery,
                string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
                "FUZZY",
                page: 1,
                pageSize: ResultLimit,
                timeout.Token);

            return new VoiceBookSearchResult(
                ErrorCode: string.Empty,
                TotalMatches: response.TotalCount,
                Books: response.Books.Select(book => new VoiceBookSearchItem(
                    book.BookId,
                    book.Title,
                    book.Author,
                    book.Isbn,
                    book.Classification ?? string.Empty,
                    book.AvailableCopies)).ToList());
        }
        catch (OperationCanceledException)
        {
            return VoiceBookSearchResult.Timeout();
        }
    }

    [McpServerTool(Name = "search_areas", UseStructuredContent = true)]
    [Description("Tìm khu vực trong thư viện theo tên hoặc mô tả. Chỉ dùng khi người dùng hỏi vị trí một khu vực. Kết quả đã giới hạn tối đa ba khu vực.")]
    public async Task<VoiceAreaSearchResult> SearchAreasAsync(
        [Description("Tên hoặc mô tả khu vực cần tìm.")] string query)
    {
        if (!TryNormalizeQuery(query, out var normalizedQuery))
            return VoiceAreaSearchResult.InvalidInput();

        try
        {
            using var timeout = new CancellationTokenSource(ToolTimeout);
            var response = await areaSearch.SearchAsync(normalizedQuery, ResultLimit, timeout.Token);
            return new VoiceAreaSearchResult(
                ErrorCode: string.Empty,
                TotalMatches: response.TotalMatches,
                Areas: response.Areas.Select(area => new VoiceAreaSearchItem(
                    area.Name,
                    area.FloorName,
                    area.FloorNumber)).ToList());
        }
        catch (OperationCanceledException)
        {
            return VoiceAreaSearchResult.Timeout();
        }
    }

    private static bool TryNormalizeQuery(string? query, out string normalized)
    {
        normalized = query?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= 100;
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
    string Isbn,
    string Classification,
    int AvailableCopies);

public sealed record VoiceAreaSearchResult(
    string ErrorCode,
    int TotalMatches,
    IReadOnlyList<VoiceAreaSearchItem> Areas)
{
    public static VoiceAreaSearchResult InvalidInput() => new("INVALID_INPUT", 0, []);
    public static VoiceAreaSearchResult Timeout() => new("TOOL_TIMEOUT", 0, []);
}

public sealed record VoiceAreaSearchItem(string Name, string FloorName, int FloorNumber);
