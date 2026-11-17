namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>The doctype seam of the reader with DTD processing enabled: it parses the internal subset into a model.</summary>
internal sealed class DtdDoctypeHandler : IXmlDoctypeHandler
{
    private readonly XmlReaderLimits _limits;

    internal DtdDoctypeHandler(XmlReaderLimits limits) => _limits = limits;

    /// <summary>Gets the declarations of the last accepted document type declaration, <see langword="null"/> before one was read or after an error.</summary>
    internal DtdDeclarations? Declarations { get; private set; }

    /// <inheritdoc />
    public XmlErrorCode? Handle(in XmlDoctypeInfo doctype)
    {
        var error = DtdSubsetParser.Parse(in doctype, _limits, out var declarations);
        Declarations = declarations;

        return error;
    }
}
