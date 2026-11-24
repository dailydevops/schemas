namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Xml;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;

/// <summary>Parses <c>xs:dateTime</c> forms from a span, against <c>XmlConvert.ToDateTime</c> on the same text.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class DateTimeParseBenchmarks
#pragma warning restore CA1515
{
    private string _lexical = "";

    /// <summary>Gets or sets the input set: <c>Short</c>, <c>Typical</c>, or <c>Long</c>.</summary>
    [Params(DatatypeBenchmarkInput.Short, DatatypeBenchmarkInput.Typical, DatatypeBenchmarkInput.Long)]
    public string Size { get; set; } = DatatypeBenchmarkInput.Short;

    /// <summary>Prepares the lexical form and proves that both parsers accept it.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lexical = DatatypeBenchmarkInput.DateTime(Size);
        if (!XsdDateTime.Parse(_lexical.AsSpan(), XsdDateTimeKind.DateTime, XsdVersion.V11).Success)
        {
            throw new InvalidOperationException($"The library rejects '{_lexical}'.");
        }

        _ = XmlConvert.ToDateTime(_lexical, XmlDateTimeSerializationMode.RoundtripKind);
    }

    /// <summary>Baseline: the <c>System.Xml</c> conversion of the same lexical form.</summary>
    /// <returns>The parsed value.</returns>
    [Benchmark(Baseline = true)]
    public DateTime ParseDateTimeSystemXml() =>
        XmlConvert.ToDateTime(_lexical, XmlDateTimeSerializationMode.RoundtripKind);

    /// <summary>Parses the lexical form from a span.</summary>
    /// <returns>The parse outcome.</returns>
    [Benchmark]
    public bool ParseDateTime() =>
        XsdDateTime.Parse(_lexical.AsSpan(), XsdDateTimeKind.DateTime, XsdVersion.V11).Success;
}
