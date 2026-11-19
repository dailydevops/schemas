namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>Parses a document type declaration whose external text comes from a scripted resolver and dumps the model.</summary>
internal static class SubsetHarness
{
    internal const string DocumentUri = LoaderHarness.DocumentUri;
    internal const string SubsetUri = "http://example.org/doc/ext.dtd";

    /// <summary>A resolver that serves <paramref name="subset"/> as the external subset <c>ext.dtd</c> next to the document.</summary>
    internal static RecordingResolver Serving(string subset) =>
        new RecordingResolver().Add(SubsetUri, LoaderHarness.Utf8(subset));

    internal static (string Model, XmlErrorCode? Error) Parse(
        string internalSubset,
        IResourceResolver resolver,
        string systemId = "ext.dtd",
        XmlReaderLimits? limits = null,
        DtdValidationOptions? options = null,
        string? baseUri = DocumentUri
    ) => Dump(ParseModel(internalSubset, resolver, systemId, limits, options, baseUri));

    internal static (DtdDeclarations? Model, XmlErrorCode? Error) ParseModel(
        string internalSubset,
        IResourceResolver resolver,
        string systemId = "ext.dtd",
        XmlReaderLimits? limits = null,
        DtdValidationOptions? options = null,
        string? baseUri = DocumentUri
    )
    {
        limits ??= XmlReaderLimits.Default;
        var reader = new ExternalSubsetReader(options ?? LoaderHarness.Open(resolver), limits);
        var info = new XmlDoctypeInfo("doc".AsSpan(), default, systemId.AsSpan(), internalSubset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, limits, reader, baseUri, out var declarations);

        return (declarations, error);
    }

    /// <summary>Parses the external subset text with an empty internal subset.</summary>
    internal static (string Model, XmlErrorCode? Error) ParseSubset(string subset, XmlReaderLimits? limits = null) =>
        Parse(string.Empty, Serving(subset), limits: limits);

    private static (string Model, XmlErrorCode? Error) Dump((DtdDeclarations? Model, XmlErrorCode? Error) run) =>
        (run.Model is null ? "<none>" : DtdHarness.Dump(run.Model), run.Error);
}
