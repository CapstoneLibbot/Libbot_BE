namespace LiBot.Application.Models;

/// <summary>
/// Stable discriminator used to select the database-managed book-guidance workflow.
/// Tasks, step types, configuration and transitions are stored in the database.
/// </summary>
public static class BookGuidanceWorkflow
{
    public const string Type = "SEARCH_AND_GUIDE_TO_BOOK";
}
