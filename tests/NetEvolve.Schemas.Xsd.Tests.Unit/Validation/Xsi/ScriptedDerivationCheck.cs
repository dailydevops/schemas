namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

/// <summary>
/// A fake derivation check behind the seam: a pair of types is valid when a test allowed it and the block set does not name the
/// method the test said the derived type was derived by. Every call is recorded.
/// </summary>
internal sealed class ScriptedDerivationCheck : IDerivationCheck
{
    private readonly Dictionary<(IXsdComponent Derived, IXsdComponent Base), XsdDerivationMethod> _allowed = [];

    public static XsdRule Rule { get; } = FakeRules.Find(XsdRuleCodes.CosCtExtends);

    public List<(
        IXsdComponent Derived,
        IXsdComponent Base,
        XsdDerivationMethod Method,
        XsdDerivationMethod Block
    )> Calls { get; } = [];

    /// <summary>Allows the derived type for the base type unless the block set names <paramref name="by"/>.</summary>
    public ScriptedDerivationCheck Allow(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod by = XsdDerivationMethod.Extension
    )
    {
        _allowed[(derived, baseType)] = by;
        return this;
    }

    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    )
    {
        Calls.Add((derived, baseType, method, blockSet));
        return _allowed.TryGetValue((derived, baseType), out var by) && (blockSet & by) == XsdDerivationMethod.None
            ? DerivationResult.Valid
            : DerivationResult.Invalid(Rule);
    }
}
