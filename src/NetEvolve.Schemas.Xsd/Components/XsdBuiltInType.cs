namespace NetEvolve.Schemas.Xsd.Components;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>A built-in type of the schema namespace: <c>anyType</c> or a datatype of the Xml package.</summary>
internal sealed class XsdBuiltInType : IXsdComponent
{
    internal XsdBuiltInType(
        XsdComponentKind kind,
        string localName,
        IDatatypeDescriptor? descriptor,
        XsdBuiltInType? baseType
    )
    {
        ArgumentNullException.ThrowIfNullOrEmpty(localName);

        Kind = kind;
        Name = new XPathQName(SchemaGrammar.Namespace, localName);
        Descriptor = descriptor;
        BaseType = baseType;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind { get; }

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <summary>Gets the location, which is unknown: a built-in type has no declaring syntax.</summary>
    public DocumentLocation Location => default;

    /// <summary>Gets the datatype, <see langword="null"/> for <c>anyType</c>.</summary>
    public IDatatypeDescriptor? Descriptor { get; }

    /// <summary>Gets the base type, <see langword="null"/> for <c>anyType</c> whose base is itself.</summary>
    public XsdBuiltInType? BaseType { get; }
}
