namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:prefix-from-QName</c>: the prefix of a QName as <c>xs:NCName</c>, the empty sequence for a name without a prefix.</summary>
internal sealed class PrefixFromQNameFunction : QNamePartFunction
{
    private static readonly XdmTypeId NCName = XdmTypes.Get("NCName");

    /// <summary>Initializes a new instance of the <see cref="PrefixFromQNameFunction"/> class.</summary>
    public PrefixFromQNameFunction()
        : base("prefix-from-QName") { }

    /// <summary>Creates an <c>xs:NCName</c> item.</summary>
    /// <param name="text">The name.</param>
    /// <returns>The item.</returns>
    internal static XdmItem NCNameItem(string text)
    {
        Argument.ThrowIfNull(text);
        return XdmItem.FromString(text).WithTypeId(NCName);
    }

    /// <inheritdoc />
    protected override XdmSequence Part(XdmQName qname) =>
        qname.Prefix is null ? XdmSequence.Empty : XdmSequence.Create(NCNameItem(qname.Prefix));
}
