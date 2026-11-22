namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The error tables of the Xml package, flattened for the coverage matrix.</summary>
internal static class ConstraintCodeTables
{
    private const string WellFormednessPrefix = "WFC: ";
    private const string NamespacePrefix = "NSC: ";

    /// <summary>
    /// Reads <see cref="XmlErrorCodes"/> (a code is a constraint when its label starts with <c>WFC:</c> or <c>NSC:</c>) and
    /// <see cref="DtdValidityCodes"/> (a code is a constraint when it is one of the 26 validity constraints).
    /// </summary>
    /// <returns>Every code of both tables.</returns>
    internal static IReadOnlyList<ConstraintCode> Load()
    {
        var codes = new List<ConstraintCode>();
        foreach (var code in XmlErrorCodes.All)
        {
            codes.Add(Classify(code));
        }

        foreach (var code in DtdValidityCodes.All)
        {
            codes.Add(
                new ConstraintCode(
                    code.Alias,
                    code.Kind == DtdValidityCodeKind.ValidityConstraint ? ConstraintKind.Validity : null,
                    code.Name,
                    code.Section
                )
            );
        }

        return codes;
    }

    private static ConstraintCode Classify(XmlErrorCode code)
    {
        if (code.Constraint.StartsWith(WellFormednessPrefix, StringComparison.Ordinal))
        {
            return new ConstraintCode(
                code.Alias,
                ConstraintKind.WellFormedness,
                code.Constraint[WellFormednessPrefix.Length..],
                string.Empty
            );
        }

        return code.Constraint.StartsWith(NamespacePrefix, StringComparison.Ordinal)
            ? new ConstraintCode(
                code.Alias,
                ConstraintKind.Namespace,
                code.Constraint[NamespacePrefix.Length..],
                string.Empty
            )
            : new ConstraintCode(code.Alias, null, code.Constraint, string.Empty);
    }
}
