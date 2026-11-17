namespace NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

/// <summary>Follows the base chain of <see cref="FakeType"/> and rejects it when a method of the chain is blocked.</summary>
internal sealed class ChainDerivationCheck : IDerivationCheck
{
    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    )
    {
        var rule = FakeRules.Find(XsdRuleCodes.CosCtDerivedOk);
        var methods = XsdDerivationMethod.None;
        for (var t = (FakeType)derived; t != baseType; t = t.Base!)
        {
            if (t.Base is null)
            {
                return DerivationResult.Invalid(rule);
            }

            methods |= t.Method;
        }

        return (methods & blockSet) == XsdDerivationMethod.None
            ? DerivationResult.Valid
            : DerivationResult.Invalid(rule);
    }
}
