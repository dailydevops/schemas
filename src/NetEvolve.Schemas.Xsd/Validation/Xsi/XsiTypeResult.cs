namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The result of <see cref="XsiTypeResolver.Resolve"/>.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XsiTypeResult
{
    internal XsiTypeResult(XsiTypeStatus status, IXsdComponent? type, XPathQName name, XsdRule? violatedRule)
    {
        Status = status;
        Type = type;
        Name = name;
        ViolatedRule = violatedRule;
    }

    /// <summary>Gets the outcome.</summary>
    public XsiTypeStatus Status { get; }

    /// <summary>Gets the named type, <see langword="null"/> unless <see cref="Status"/> is <see cref="XsiTypeStatus.Resolved"/>.</summary>
    public IXsdComponent? Type { get; }

    /// <summary>Gets the expanded name the value resolved to, <see langword="default"/> when the value is no QName.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the rule the derivation check named, <see langword="null"/> unless <see cref="Status"/> is <see cref="XsiTypeStatus.NotDerived"/>.</summary>
    public XsdRule? ViolatedRule { get; }
}
