namespace NetEvolve.Schemas.Xsd.Substitution;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

internal sealed class SubstitutionGroups
{
    public static SubstitutionGroups Build(
        IReadOnlyList<ISubstitutionElement> elements,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    ) => throw new NotSupportedException();

    public IReadOnlyList<ISubstitutionElement> GetMembers(ISubstitutionElement head) =>
        throw new NotSupportedException();

    public IReadOnlyList<ISubstitutionElement> GetInstanceElements(ISubstitutionElement head) =>
        throw new NotSupportedException();
}
