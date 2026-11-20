namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:namespace-uri-from-QName</c>: the namespace name of a QName as <c>xs:anyURI</c>, zero-length for a name in no namespace.</summary>
internal sealed class NamespaceUriFromQNameFunction : QNamePartFunction
{
    /// <summary>Initializes a new instance of the <see cref="NamespaceUriFromQNameFunction"/> class.</summary>
    public NamespaceUriFromQNameFunction()
        : base("namespace-uri-from-QName") { }

    /// <inheritdoc />
    protected override XdmSequence Part(XdmQName qname) =>
        XdmSequence.Create(XdmItem.FromAnyUri(qname.Name.NamespaceName ?? string.Empty));
}
