namespace LiBot.Api.Controllers;

/// <summary>Machine-readable error codes shared across guidance endpoints for Mobile kiosk localization.</summary>
internal static class GuidanceErrorCodes
{
    public const string SimUseRunButton = "SIM_USE_RUN_BUTTON";
    public const string CopyUnavailable = "COPY_UNAVAILABLE";
    public const string EndpointMismatch = "ENDPOINT_MISMATCH";
    public const string InvalidEndpoint = "INVALID_ENDPOINT";
    public const string NoActiveMap = "NO_ACTIVE_MAP";
    public const string WrongActiveMap = "WRONG_ACTIVE_MAP";
    public const string RobotRejected = "ROBOT_REJECTED";

    public static string FromRuntimeValidation(string message)
    {
        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return "RUNTIME_NOT_FOUND";
        if (message.Contains("offline", StringComparison.OrdinalIgnoreCase)
            || message.Contains("stale", StringComparison.OrdinalIgnoreCase))
            return "ROBOT_OFFLINE";
        if (message.Contains("localized", StringComparison.OrdinalIgnoreCase))
            return "NOT_LOCALIZED";
        if (message.Contains("Nav2", StringComparison.OrdinalIgnoreCase))
            return "NAV2_NOT_READY";
        if (message.Contains("lease", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Arm", StringComparison.OrdinalIgnoreCase))
            return "LEASE_MISMATCH";
        return "ROBOT_NOT_READY";
    }
}
