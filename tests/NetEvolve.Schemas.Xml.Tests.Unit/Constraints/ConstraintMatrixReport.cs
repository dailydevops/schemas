namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;

/// <summary>Renders the coverage matrix as the lines of the test output.</summary>
internal static class ConstraintMatrixReport
{
    /// <summary>Renders one line per entry and a summary line.</summary>
    /// <param name="entries">The entries of the matrix.</param>
    /// <returns>The lines of the report.</returns>
    internal static IReadOnlyList<string> Render(IReadOnlyList<ConstraintEntry> entries) =>
        throw new NotSupportedException("The report is not implemented yet.");
}
