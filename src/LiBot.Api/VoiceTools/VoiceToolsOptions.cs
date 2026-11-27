namespace LiBot.Api.VoiceTools;

public sealed class VoiceToolsOptions
{
    public const string SectionName = "VoiceTools";

    /// <summary>Service-to-service bearer token used only by the Xiaozhi MCP client.</summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>Robot assigned to voice-guidance jobs from the kiosk.</summary>
    public string? GuidanceRobotId { get; init; }

    /// <summary>Runtime assigned to voice-guidance jobs from the kiosk.</summary>
    public string GuidanceRuntimeId { get; init; } = "real";
}
