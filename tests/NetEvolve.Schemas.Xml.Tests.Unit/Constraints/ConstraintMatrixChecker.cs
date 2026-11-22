namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;

/// <summary>Checks the coverage matrix against the error tables and the tests that exist.</summary>
internal static class ConstraintMatrixChecker
{
    /// <summary>Checks the matrix.</summary>
    /// <param name="entries">The entries of the matrix.</param>
    /// <param name="codes">The rows of the error tables.</param>
    /// <param name="testExists">Tells whether a unit test reference exists.</param>
    /// <param name="xmlConfCaseExists">Tells whether a case of the conformance suite exists.</param>
    /// <returns>Every broken rule; empty when the matrix is complete.</returns>
    internal static IReadOnlyList<ConstraintViolation> Check(
        IReadOnlyList<ConstraintEntry> entries,
        IReadOnlyList<ConstraintCode> codes,
        Func<string, bool> testExists,
        Func<string, bool> xmlConfCaseExists
    ) => throw new NotSupportedException("The check is not implemented yet.");
}
