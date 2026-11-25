namespace NetEvolve.Schemas.Benchmarks.Gate;

using System.Text.Json;

/// <summary>
/// Reads a BenchmarkDotNet full JSON report. With <c>allowMissingMeasurement</c> a benchmark without measurement
/// (<c>Statistics</c> or <c>Mean</c> is null, e.g. a failed run) becomes a NaN mean and <c>-1</c> allocated bytes
/// instead of an exception; only the summary does that, the gate still fails on such a report.
/// </summary>
internal static class ResultReader
{
    public static IReadOnlyList<BenchmarkResult> Read(string json, bool allowMissingMeasurement = false)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<BenchmarkResult>();
        foreach (var b in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
        {
            var missing =
                allowMissingMeasurement
                && (
                    b.GetProperty("Statistics").ValueKind == JsonValueKind.Null
                    || b.GetProperty("Statistics").GetProperty("Mean").ValueKind == JsonValueKind.Null
                );
            list.Add(
                new BenchmarkResult(
                    b.GetProperty("Type").GetString() ?? "",
                    b.GetProperty("Method").GetString() ?? "",
                    b.TryGetProperty("Parameters", out var p) ? p.GetString() ?? "" : "",
                    missing ? double.NaN : b.GetProperty("Statistics").GetProperty("Mean").GetDouble(),
                    missing ? -1 : b.GetProperty("Memory").GetProperty("BytesAllocatedPerOperation").GetInt64()
                )
            );
        }
        return list;
    }
}
