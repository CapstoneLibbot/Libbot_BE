using System.Reflection;
using System.Text.RegularExpressions;

namespace LiBot.Infrastructure.Persistence.Migrations;

internal static partial class MigrationSql
{
    public static string GetCreateTablesIfMissing()
    {
        var sql = ReadResource("create-logical-erd-schema.sql");
        var foreignKeysStart = sql.IndexOf("ALTER TABLE dbo.", StringComparison.OrdinalIgnoreCase);
        if (foreignKeysStart >= 0) sql = sql[..foreignKeysStart];

        sql = GoBatchRegex().Replace(sql, string.Empty);
        return CreateTableRegex().Replace(sql, match =>
        {
            var table = match.Groups["table"].Value;
            return $"IF OBJECT_ID(N'dbo.{table}', N'U') IS NULL{Environment.NewLine}{match.Value}";
        });
    }

    public static IReadOnlyList<string> GetLegacyAlignmentBatches()
    {
        var sql = ReadResource("align-legacy-schema-to-logical-erd.sql");
        return GoBatchRegex().Split(sql)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();
    }

    private static string ReadResource(string fileName)
    {
        var resourceName = $"LiBot.Infrastructure.Persistence.Migrations.{fileName}";
        using var stream = typeof(MigrationSql).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"(?m)^\s*GO\s*\r?\n?", RegexOptions.IgnoreCase)]
    private static partial Regex GoBatchRegex();

    [GeneratedRegex(@"(?m)^CREATE TABLE dbo\.\[(?<table>[^\]]+)\]\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex CreateTableRegex();
}
