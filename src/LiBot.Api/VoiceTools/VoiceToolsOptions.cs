namespace LiBot.Api.VoiceTools;

public sealed class VoiceToolsOptions
{
    public const string SectionName = "VoiceTools";

    /// <summary>Service-to-service bearer token used only by the Xiaozhi MCP client.</summary>
    public string ApiToken { get; init; } = string.Empty;
}
