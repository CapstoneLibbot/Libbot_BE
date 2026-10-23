using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class Feedback : Entity
{
    public Guid JobId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    public Feedback(Guid jobId, int rating, string? comment = null)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating));

        JobId = jobId;
        Rating = rating;
        Comment = comment?.Trim();
    }

    public void Update(int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating));

        Rating = rating;
        Comment = comment?.Trim();
    }
}
