namespace NetEvolve.Schemas.Benchmarks.Gate;

using System.Globalization;
using System.Text;

/// <summary>Renders a markdown table of results against recorded targets.</summary>
internal static class BenchmarkSummary
{
    private const string Header =
        "| Benchmark | Mean (ns) | Target mean | delta % | Allocated (B) | Target allocated | delta % | Ratio to System.Xml | Target ratio | Status |";
    private const string Separator = "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |";

    /// <summary>Renders one row per result; the status follows the rules of <see cref="BenchmarkGate"/>.</summary>
    public static string Render(IReadOnlyList<BenchmarkResult> results, IReadOnlyList<BenchmarkTarget> targets)
    {
        var byName = new Dictionary<string, BenchmarkTarget>(StringComparer.Ordinal);
        foreach (var t in targets)
        {
            byName[t.Name] = t;
        }

        var baselines = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);
        foreach (var r in results)
        {
            if (r.IsBaseline && !double.IsNaN(r.MeanNs))
            {
                baselines[r.Group] = r;
            }
        }

        var sb = new StringBuilder();
        _ = sb.Append(Header).Append('\n').Append(Separator).Append('\n');
        foreach (var r in results)
        {
            _ = byName.TryGetValue(r.Name, out var t);
            _ = baselines.TryGetValue(r.Group, out var baseline);
            double? ratio = baseline is null || double.IsNaN(r.MeanNs) ? null : r.MeanNs / baseline.MeanNs;
            _ = sb.Append(Row(r, t, ratio)).Append('\n');
        }
        return sb.ToString();
    }

    private static string Row(BenchmarkResult r, BenchmarkTarget? t, double? ratio)
    {
        var name = r.Name.Replace("|", @"\|", StringComparison.Ordinal);
        var isBaseline = r.IsBaseline && t is null;
        var hasTarget = t is not null && !isBaseline;
        return string.Join(
            " | ",
            "| " + name,
            Number(r.MeanNs),
            hasTarget ? Number(t!.MeanNs) : "-",
            hasTarget ? Delta(r.MeanNs, t!.MeanNs) : "-",
            r.AllocatedBytes < 0 ? "n/a" : r.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
            hasTarget ? t!.AllocatedBytes.ToString(CultureInfo.InvariantCulture) : "-",
            hasTarget ? Delta(r.AllocatedBytes, t!.AllocatedBytes) : "-",
            ratio is { } v ? Ratio(v) : "-",
            hasTarget && t!.BaselineRatio is { } tr ? Ratio(tr) : "-",
            Status(r, t, ratio, isBaseline) + " |"
        );
    }

    private static string Status(BenchmarkResult r, BenchmarkTarget? t, double? ratio, bool isBaseline)
    {
        if (double.IsNaN(r.MeanNs))
        {
            return "n/a";
        }
        if (isBaseline)
        {
            return "baseline";
        }
        if (t is null)
        {
            return "no target";
        }
        if (r.AllocatedBytes > t.AllocatedBytes)
        {
            return "violation";
        }
        if (t.BaselineRatio is { } target)
        {
            if (ratio is not { } actual || actual > target * BenchmarkGate.RatioTolerance)
            {
                return "violation";
            }
            return actual > target ? "within tolerance" : "ok";
        }
        return r.MeanNs > t.MeanNs * (1 + BenchmarkGate.AllowedRegression) ? "violation" : "ok";
    }

    private static string Number(double value) =>
        double.IsNaN(value) ? "n/a" : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Ratio(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Delta(double actual, double target) =>
        double.IsNaN(actual) || actual < 0 || (target == 0 && actual != 0) ? "-"
        : target == 0 ? "+0.0%"
        : ((actual / target) - 1).ToString("+0.0%;-0.0%;+0.0%", CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes the table for a report file or all <c>*-report-full.json</c> files of a directory, against all
    /// <c>*.json</c> targets. Always returns 0: the summary informs, <c>gate</c> decides.
    /// </summary>
    public static int Run(string resultsPath, string targetsDirectory, TextWriter output)
    {
        var reports = Directory.Exists(resultsPath)
            ? Directory.GetFiles(resultsPath, "*-report-full.json")
            : [resultsPath];
        Array.Sort(reports, StringComparer.Ordinal);
        var fullReports = reports.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targets = new List<BenchmarkTarget>();
        foreach (var file in Directory.GetFiles(targetsDirectory, "*.json"))
        {
            if (!fullReports.Contains(Path.GetFullPath(file)))
            {
                targets.AddRange(TargetReader.Read(File.ReadAllText(file)));
            }
        }

        var results = new List<BenchmarkResult>();
        foreach (var report in reports)
        {
            results.AddRange(ResultReader.Read(File.ReadAllText(report), allowMissingMeasurement: true));
        }

        output.Write(Render(results, targets));
        return 0;
    }
}
