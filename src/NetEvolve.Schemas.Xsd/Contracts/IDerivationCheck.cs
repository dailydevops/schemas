namespace NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Checks that a derived type is validly derived from a base type (Type Derivation OK, Derivation Valid).</summary>
internal interface IDerivationCheck
{
    /// <summary>Checks a derivation.</summary>
    /// <param name="derived">The derived type.</param>
    /// <param name="baseType">The base type.</param>
    /// <param name="method">The derivation method that is asked for.</param>
    /// <param name="blockSet">The methods that are blocked.</param>
    /// <returns>The result, naming the violated rule when invalid.</returns>
    DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    );
}
