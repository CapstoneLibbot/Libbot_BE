using System.ComponentModel.DataAnnotations;

namespace LiBot.Api.Models;

/// <summary>API payload requesting that LiBot move to a target point.</summary>
public sealed record MoveLiBotRequest : IValidatableObject
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string EndpointId { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Theta { get; init; }

    [Required]
    [RegularExpression("^(NORMAL|SLOW_GUIDE|PATROL)$")]
    public required string SpeedMode { get; init; }

    [Required]
    [RegularExpression("^(WAIT_RESUME|REROUTE|DETECT_AND_WAIT)$")]
    public required string ObstaclePolicy { get; init; }

    [Range(1, 600)]
    public required int TimeoutSec { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is not null && string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name must not be empty or whitespace.", [nameof(Name)]);
        }

        if (EndpointId is not null && string.IsNullOrWhiteSpace(EndpointId))
        {
            yield return new ValidationResult("EndpointId must not be empty or whitespace.", [nameof(EndpointId)]);
        }

        if (!double.IsFinite(X))
        {
            yield return new ValidationResult("X must be a finite number.", [nameof(X)]);
        }

        if (!double.IsFinite(Y))
        {
            yield return new ValidationResult("Y must be a finite number.", [nameof(Y)]);
        }

        if (!double.IsFinite(Theta))
        {
            yield return new ValidationResult("Theta must be a finite number.", [nameof(Theta)]);
        }
    }
}
