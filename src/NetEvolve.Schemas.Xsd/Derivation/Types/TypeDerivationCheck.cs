namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Checks the derivation of types (Structures 3.4.6 and 3.14.6).</summary>
internal sealed class TypeDerivationCheck : IDerivationCheck
{
    internal TypeDerivationCheck(XsdVersion version) => throw new NotSupportedException();

    /// <inheritdoc />
    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    ) => throw new NotSupportedException();

    /// <summary>Reports the violations of the derivation of one complex type as located issues.</summary>
    /// <param name="type">The type.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(ComplexTypeDefinition type, XsdIssueReporter reporter) => throw new NotSupportedException();

    /// <summary>Reports the violations of the derivation of one complex type from a given base type as located issues.</summary>
    /// <param name="type">The type, whose derivation method is the one that is checked.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(ComplexTypeDefinition type, IXsdComponent baseType, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Reports the violations of the derivation of every complex type of a list of components.</summary>
    /// <param name="components">The components.</param>
    /// <param name="reporter">The reporter.</param>
    internal void ReportAll(IReadOnlyList<IXsdComponent> components, XsdIssueReporter reporter) =>
        throw new NotSupportedException();
}
