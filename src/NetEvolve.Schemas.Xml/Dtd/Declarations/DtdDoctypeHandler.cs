namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>
/// The doctype seam of the reader with DTD processing enabled: it parses the internal subset into a model and, when the
/// options of external access are given, the external subset and the external parameter entities.
/// </summary>
internal sealed class DtdDoctypeHandler : IXmlDoctypeHandler
{
    private readonly XmlReaderLimits _limits;
    private readonly ExternalSubsetReader? _external;
    private readonly string? _baseUri;

    internal DtdDoctypeHandler(XmlReaderLimits limits)
    {
        Argument.ThrowIfNull(limits);

        _limits = limits;
    }

    /// <summary>Initializes a new instance of the <see cref="DtdDoctypeHandler"/> class that reads the external subset.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="options">The options of external access, <see langword="null"/> to leave the external subset unread.</param>
    /// <param name="baseUri">The absolute base URI of the document, or <see langword="null"/>.</param>
    internal DtdDoctypeHandler(XmlReaderLimits limits, DtdValidationOptions? options, string? baseUri)
        : this(limits)
    {
        _external = options is null ? null : new ExternalSubsetReader(options, limits);
        _baseUri = baseUri;
    }

    /// <summary>Gets the declarations of the last accepted document type declaration, <see langword="null"/> before one was read or after an error.</summary>
    internal DtdDeclarations? Declarations { get; private set; }

    /// <inheritdoc />
    public XmlErrorCode? Handle(in XmlDoctypeInfo doctype)
    {
        var error = DtdSubsetParser.Parse(in doctype, _limits, _external, _baseUri, out var declarations);
        Declarations = declarations;

        return error;
    }
}
