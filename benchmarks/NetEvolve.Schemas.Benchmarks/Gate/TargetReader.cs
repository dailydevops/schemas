namespace NetEvolve.Schemas.Benchmarks.Gate;

using System.Text.Json;

/// <summary>Reads a targets file: a JSON array of <c>{name, meanNs, allocatedBytes, baselineRatio?}</c>.</summary>
internal static class TargetReader
{
    public static IReadOnlyList<BenchmarkTarget> Read(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<BenchmarkTarget>();
        foreach (var t in doc.RootElement.EnumerateArray())
        {
            list.Add(
                new BenchmarkTarget(
                    t.GetProperty("name").GetString() ?? "",
                    t.GetProperty("meanNs").GetDouble(),
                    t.GetProperty("allocatedBytes").GetInt64(),
                    t.TryGetProperty("baselineRatio", out var r) && r.ValueKind == JsonValueKind.Number
                        ? r.GetDouble()
                        : null
                )
            );
        }
        return list;
    }
}
