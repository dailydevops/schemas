namespace NetEvolve.Schemas.Benchmarks.Gate;

using System.Text.Json;

/// <summary>Reads a BenchmarkDotNet full JSON report.</summary>
internal static class ResultReader
{
    public static IReadOnlyList<BenchmarkResult> Read(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<BenchmarkResult>();
        foreach (var b in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
        {
            list.Add(
                new BenchmarkResult(
                    b.GetProperty("Type").GetString() ?? "",
                    b.GetProperty("Method").GetString() ?? "",
                    b.TryGetProperty("Parameters", out var p) ? p.GetString() ?? "" : "",
                    b.GetProperty("Statistics").GetProperty("Mean").GetDouble(),
                    b.GetProperty("Memory").GetProperty("BytesAllocatedPerOperation").GetInt64()
                )
            );
        }
        return list;
    }
}
