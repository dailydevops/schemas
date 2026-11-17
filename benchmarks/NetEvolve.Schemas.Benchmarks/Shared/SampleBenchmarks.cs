namespace NetEvolve.Schemas.Benchmarks.Shared;

using System.Xml;
using BenchmarkDotNet.Attributes;

/// <summary>Sample benchmark proving the harness. It has no target file: run it with <c>--filter *Sample*</c> and do not gate it.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class SampleBenchmarks
#pragma warning restore CA1515
{
    private readonly string _document = InputData.Document(100);

    /// <summary>Baseline: counts nodes with <c>System.Xml</c>.</summary>
    [Benchmark(Baseline = true)]
    public int CountNodesSystemXml()
    {
        using var reader = XmlReader.Create(new StringReader(_document));
        var count = 0;
        while (reader.Read())
        {
            count++;
        }
        return count;
    }

    /// <summary>Counts element starts with a plain scan.</summary>
    [Benchmark]
    public int CountMarkup()
    {
        var count = 0;
        foreach (var c in _document)
        {
            if (c == '<')
            {
                count++;
            }
        }
        return count;
    }
}
