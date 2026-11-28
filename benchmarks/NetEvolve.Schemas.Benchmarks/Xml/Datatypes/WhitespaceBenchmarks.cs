namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Text;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>Validates a long <c>xs:base64Binary</c> form through its descriptor, which collapses white space before the datatype check.</summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class WhitespaceBenchmarks
#pragma warning restore CA1515
{
    private const int Length = 4096;

    private readonly IDatatypeDescriptor _base64 = BuiltInDatatypes.For(XsdVersion.V11).Get("base64Binary");
    private string _lexical = "";

    /// <summary>Gets or sets the layout: <c>Unwrapped</c> or <c>Wrapped</c> at 76 characters per line.</summary>
    [Params("Unwrapped", "Wrapped")]
    public string Layout { get; set; } = "Unwrapped";

    /// <summary>Prepares the lexical form and proves that the descriptor accepts it.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var builder = new StringBuilder(Length + Length / 76 * 2);
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for (var i = 0; i < Length; i++)
        {
            _ = builder.Append(alphabet[i * 7 % alphabet.Length]);
            if (Layout == "Wrapped" && (i + 1) % 76 == 0 && i + 1 < Length)
            {
                _ = builder.Append("\r\n");
            }
        }

        _lexical = builder.ToString();
        if (!_base64.Validate(_lexical.AsSpan()).IsValid)
        {
            throw new InvalidOperationException("The library rejects the input form.");
        }
    }

    /// <summary>Validates the long form through the descriptor.</summary>
    /// <returns>The validation outcome.</returns>
    [Benchmark]
    public bool ValidateBase64() => _base64.Validate(_lexical.AsSpan()).IsValid;
}
