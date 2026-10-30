using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

/// <summary>Read-only catalogue search used by the voice MCP server.</summary>
public interface IVoiceAreaSearchService
{
    Task<VoiceAreaSearchResponse> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}
