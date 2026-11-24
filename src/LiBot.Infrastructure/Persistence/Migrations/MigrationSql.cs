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

    public static string GetLogicalErdFieldList()
    {
        var sql = ReadResource("update-logical-erd.sql");
        var fields = new List<string>();
        foreach (Match tableMatch in TargetTableRegex().Matches(sql))
        {
            var table = tableMatch.Groups["table"].Value;
            foreach (Match columnMatch in TargetColumnRegex().Matches(tableMatch.Groups["body"].Value))
            {
                var column = columnMatch.Groups["column"].Value;
                fields.Add($"(N'{table.Replace("'", "''", StringComparison.Ordinal)}', N'{column.Replace("'", "''", StringComparison.Ordinal)}')");
            }
        }

        if (fields.Count == 0)
            throw new InvalidOperationException("The logical ERD SQL did not contain any table fields.");

        return string.Join(",\n", fields);
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

    [GeneratedRegex(@"(?is)CREATE\s+TABLE\s+\[(?<table>[^\]]+)\]\s*\((?<body>.*?)\n\);", RegexOptions.IgnoreCase)]
    private static partial Regex TargetTableRegex();

    [GeneratedRegex(@"(?m)^\s*\[(?<column>[^\]]+)\]\s+", RegexOptions.IgnoreCase)]
    private static partial Regex TargetColumnRegex();
}
