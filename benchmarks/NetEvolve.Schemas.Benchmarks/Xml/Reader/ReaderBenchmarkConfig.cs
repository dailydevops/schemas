namespace NetEvolve.Schemas.Benchmarks.Xml.Reader;

using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

/// <summary>Adds the <c>MB/s</c> column to the reader benchmarks: the input length divided by the mean.</summary>
internal sealed class ReaderBenchmarkConfig : ManualConfig
{
    public ReaderBenchmarkConfig() => AddColumn(new ThroughputColumn());

    private sealed class ThroughputColumn : IColumn
    {
        public string Id => "MBPerSecond";

        public string ColumnName => "MB/s";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Custom;

        public int PriorityInCategory => 0;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Dimensionless;

        public string Legend => "Input length in bytes divided by the mean, in MB (10^6 bytes) per second";

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
            GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            var length = ReaderBenchmarkInput.LengthFor(benchmarkCase.Descriptor.Type.Name);
            var mean = summary[benchmarkCase]?.ResultStatistics?.Mean;
            // Bytes per nanosecond times 1000 is MB per second.
            return length is { } bytes && mean is { } ns && ns > 0
                ? (bytes / ns * 1000).ToString("F1", CultureInfo.InvariantCulture)
                : "NA";
        }
    }
}
