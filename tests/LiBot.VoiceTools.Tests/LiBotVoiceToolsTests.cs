using LiBot.Api.VoiceTools;
using Xunit;

namespace LiBot.VoiceTools.Tests;

public sealed class LiBotVoiceToolsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchBooksAsync_rejects_empty_query(string query)
    {
        var result = await new LiBotVoiceTools(null!, null!, null!, null!, null!, null!)
            .SearchBooksAsync(query);

        Assert.Equal("INVALID_INPUT", result.ErrorCode);
        Assert.Empty(result.Books);
    }

    [Fact]
    public async Task SearchBooksAsync_rejects_query_over_100_characters()
    {
        var result = await new LiBotVoiceTools(null!, null!, null!, null!, null!, null!)
            .SearchBooksAsync(new string('a', 101));

        Assert.Equal("INVALID_INPUT", result.ErrorCode);
        Assert.Empty(result.Books);
    }
}
