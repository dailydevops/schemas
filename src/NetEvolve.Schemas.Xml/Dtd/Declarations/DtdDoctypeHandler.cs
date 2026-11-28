namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>
/// The doctype seam of the reader with DTD processing enabled: it parses the internal subset into a model and, when the
/// options of external access are given, the external subset and the external parameter entities. It also answers the
/// attribute definitions of an element type (<see cref="IXmlAttributeDeclarations"/>, XML-16) from the model it parsed.
/// </summary>
internal sealed class DtdDoctypeHandler : IXmlDoctypeHandler, IXmlAttributeDeclarations
{
    private readonly XmlReaderLimits _limits;
    private readonly ExternalSubsetReader? _external;
    private readonly string? _baseUri;
    private DtdAttributeTable? _attributes;

    internal DtdDoctypeHandler(XmlReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

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

    /// <summary>Gets the reader of the external entities, <see langword="null"/> when the options of external access are not given.</summary>
    internal ExternalSubsetReader? External => _external;

    /// <summary>Gets the absolute base URI of the document, <see langword="null"/> when it is not known.</summary>
    internal string? BaseUri => _baseUri;

    /// <summary>Gets the declarations of the last accepted document type declaration, <see langword="null"/> before one was read or after an error.</summary>
    internal DtdDeclarations? Declarations { get; private set; }

    /// <inheritdoc />
    public DtdElementAttributes? Find(ReadOnlySpan<char> elementName) =>
        (_attributes ??= new DtdAttributeTable(this)).Find(elementName);

    /// <inheritdoc />
    public XmlErrorCode? Handle(in XmlDoctypeInfo doctype)
    {
        var error = DtdSubsetParser.Parse(in doctype, _limits, _external, _baseUri, out var declarations);
        Declarations = declarations;

        // The default values of the attribute definitions are checked once the declarations are complete (XML-16); the parser
        // returns the model exactly when it returns no error.
        return error ?? DtdDefaultReferences.Validate(declarations!);
    }
}
