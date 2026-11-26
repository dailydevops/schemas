namespace NetEvolve.Schemas.Benchmarks.Xsd.V11;

using System.Text;
using NetEvolve.Schemas.Benchmarks.Xsd.Validation;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Builds the validators of the XSD 1.1 benchmarks in a given version.</summary>
internal static class Xsd11Validators
{
    /// <summary>Compiles one inline schema document.</summary>
    /// <param name="schema">The schema text.</param>
    /// <param name="version">The XSD version of the validator.</param>
    /// <returns>The validator.</returns>
    public static IXsdValidator Inline(string schema, XsdVersion version) =>
        SchemaValidator
            .ForXsd()
            .WithVersion(version)
            .AddSchema(Encoding.UTF8.GetBytes(schema), "urn:benchmark:inline.xsd")
            .Build();

    /// <summary>Compiles a pinned corpus of the XSD 1.0 benchmarks (XSD-66).</summary>
    /// <param name="corpus">The schema documents.</param>
    /// <param name="version">The XSD version of the validator.</param>
    /// <returns>The validator.</returns>
    public static IXsdValidator Compile(XsdBenchmarkInput.Corpus corpus, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(corpus);

        return SchemaValidator
            .ForXsd()
            .WithVersion(version)
            .WithResolver(new MemoryResourceResolver(corpus.Schemas))
            .AddSchema(corpus.Schemas[corpus.RootUri], corpus.RootUri.AbsoluteUri)
            .Build();
    }

    /// <summary>Validates through a stream.</summary>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance, UTF-8.</param>
    /// <returns>The number of issues.</returns>
    public static int ValidateStream(IXsdValidator validator, byte[] instance)
    {
        using var stream = new MemoryStream(instance, writable: false);

        return validator.Validate(stream).Issues.Count;
    }

    /// <summary>Validates through a span.</summary>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance, UTF-8.</param>
    /// <returns>The number of issues.</returns>
    public static int ValidateSpan(IXsdValidator validator, byte[] instance) =>
        validator.Validate(instance.AsSpan()).Issues.Count;
}
