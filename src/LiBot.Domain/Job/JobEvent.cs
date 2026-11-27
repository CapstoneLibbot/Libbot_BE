using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using LiBot.Domain.Common;

namespace LiBot.Domain.Job;

public class JobEvent : Entity
{
    public Guid JobStepId { get; private set; }

    [NotMapped]
    public string EventType => JobEventDataEnvelope.Read(Data).EventType;

    [System.ComponentModel.DataAnnotations.Schema.Column("EventTime")]
    public DateTime EventTimeUtc { get; private set; }
    public string? Data { get; private set; }

    private JobEvent() { }

    public JobEvent(Guid jobStepId, string eventType, string? data = null, DateTime eventTimeUtc = default)
    {
        if (jobStepId == Guid.Empty)
            throw new ArgumentException("Job step id is required.", nameof(jobStepId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));

        JobStepId = jobStepId;
        Data = JobEventDataEnvelope.Write(eventType.Trim(), data);
        EventTimeUtc = eventTimeUtc == default ? DateTime.UtcNow : eventTimeUtc;
    }
}

public static class JobEventDataEnvelope
{
    public static string Write(string eventType, string? data) =>
        JsonSerializer.Serialize(new { event_type = eventType, data });

    public static (string EventType, string? Data) Read(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (string.Empty, null);
        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("event_type", out var type)
                && type.ValueKind == JsonValueKind.String)
            {
                string? data = null;
                if (root.TryGetProperty("data", out var payload))
                    data = payload.ValueKind switch
                    {
                        JsonValueKind.String => payload.GetString(),
                        JsonValueKind.Null => null,
                        _ => payload.GetRawText()
                    };
                return (type.GetString() ?? string.Empty, data);
            }
        }
        catch (JsonException)
        {
            // Older rows can contain plain text event payloads.
        }

        return (string.Empty, value);
    }
}
