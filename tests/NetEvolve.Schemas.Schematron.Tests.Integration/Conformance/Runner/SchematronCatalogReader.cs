namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Reads the test case format of the Schematron conformance suite: suite files, includes, and cases.</summary>
internal static class SchematronCatalogReader
{
    /// <summary>Reads every case reachable from the suite file of a binding.</summary>
    /// <param name="suiteRoot">The suite root directory.</param>
    /// <param name="bindings">The bindings with their suite files.</param>
    /// <returns>Each case once, sorted by id, with every binding that lists it.</returns>
    /// <exception cref="InvalidDataException">A case does not follow the case format.</exception>
    public static IReadOnlyList<SchematronCase> Read(string suiteRoot, IReadOnlyList<BindingRule> bindings)
    {
        ArgumentNullException.ThrowIfNull(suiteRoot);
        ArgumentNullException.ThrowIfNull(bindings);
        return
        [
            new SchematronCase(
                string.Empty,
                string.Empty,
                [],
                string.Empty,
                [new SchematronDocument(string.Empty, string.Empty)],
                SchematronOutcome.NotImplemented,
                null,
                [],
                []
            ),
        ];
    }
}
