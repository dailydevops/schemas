namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A derivation check that records its arguments and rejects a method that the block set names.</summary>
internal sealed class FakeDerivationCheck : IDerivationCheck
{
    private readonly XsdRule _rule;

    public FakeDerivationCheck(XsdRule rule) => _rule = rule;

    public List<(
        IXsdComponent Derived,
        IXsdComponent Base,
        XsdDerivationMethod Method,
        XsdDerivationMethod Block
    )> Calls { get; } = [];

    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    )
    {
        Calls.Add((derived, baseType, method, blockSet));
        return (blockSet & method) != XsdDerivationMethod.None
            ? DerivationResult.Invalid(_rule)
            : DerivationResult.Valid;
    }
}
