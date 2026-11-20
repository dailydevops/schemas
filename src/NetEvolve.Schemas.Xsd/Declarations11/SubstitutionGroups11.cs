namespace NetEvolve.Schemas.Xsd.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>The substitution groups of declarations with several heads.</summary>
internal sealed class SubstitutionGroups11
{
    /// <summary>Computes the substitution groups.</summary>
    /// <param name="elements">The element declarations; their order is the order of the members.</param>
    /// <param name="derivation">The type derivation check.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <returns>The groups.</returns>
    public static SubstitutionGroups11 Build(
        IReadOnlyList<IMultiHeadElement> elements,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    ) => throw new NotSupportedException();

    /// <summary>Gets the elements that are substitutable for a head.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The members.</returns>
    public IReadOnlyList<IMultiHeadElement> GetMembers(IMultiHeadElement head) => throw new NotSupportedException();

    /// <summary>Gets the elements that may appear in an instance where the head is expected.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The elements.</returns>
    public IReadOnlyList<IMultiHeadElement> GetInstanceElements(IMultiHeadElement head) =>
        throw new NotSupportedException();
}
