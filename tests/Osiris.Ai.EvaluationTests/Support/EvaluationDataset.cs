using System.Text.Json;

namespace Osiris.Ai.EvaluationTests.Support;

public sealed record EvaluationCase(
    string Id,
    string Question,
    string ExpectedTool,
    string[] ForbiddenTools,
    bool RequireSources);

/// <summary>Loads the versioned tool-selection dataset (embedded JSONL) used to drive the evaluation gates.</summary>
internal static class EvaluationDataset
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<EvaluationCase> Cases { get; } = Load();

    public static IEnumerable<object[]> CaseIds() => Cases.Select(scenario => new object[] { scenario.Id });

    public static EvaluationCase Get(string id) => Cases.Single(scenario => scenario.Id == id);

    private static IReadOnlyList<EvaluationCase> Load()
    {
        var assembly = typeof(EvaluationDataset).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("tool-selection.jsonl", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The evaluation dataset resource was not found.");
        using var reader = new StreamReader(stream);

        var cases = new List<EvaluationCase>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            cases.Add(JsonSerializer.Deserialize<EvaluationCase>(line, Options)
                ?? throw new InvalidOperationException($"Could not parse evaluation case: {line}"));
        }

        return cases;
    }
}
