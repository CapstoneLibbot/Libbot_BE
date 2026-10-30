namespace LiBot.Application.Models;

/// <summary>Safe, voice-oriented area search result. It intentionally contains no navigation coordinates or IDs.</summary>
public sealed record VoiceAreaSearchResponse(int TotalMatches, IReadOnlyList<VoiceAreaSearchItem> Areas);

public sealed record VoiceAreaSearchItem(string Name, string FloorName, int FloorNumber);
