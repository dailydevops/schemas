namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:local-name-from-QName</c>: the local name of a QName as <c>xs:NCName</c>.</summary>
internal sealed class LocalNameFromQNameFunction : QNamePartFunction
{
    /// <summary>Initializes a new instance of the <see cref="LocalNameFromQNameFunction"/> class.</summary>
    public LocalNameFromQNameFunction()
        : base("local-name-from-QName") { }

    /// <inheritdoc />
    protected override XdmSequence Part(XdmQName qname) =>
        XdmSequence.Create(PrefixFromQNameFunction.NCNameItem(qname.Name.LocalName));
}
