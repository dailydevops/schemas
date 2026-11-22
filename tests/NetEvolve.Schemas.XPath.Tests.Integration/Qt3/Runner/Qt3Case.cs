namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>One QT3 test case.</summary>
/// <param name="TestSet">The test set name.</param>
/// <param name="Name">The case name.</param>
/// <param name="Query">The inline query text.</param>
/// <param name="QueryFile">The path of the query file when the test element refers to one.</param>
/// <param name="Environment">The referenced environment name, if any.</param>
/// <param name="Assertion">The expected result.</param>
/// <param name="Dependencies">The case-level dependencies.</param>
/// <remarks>
/// <see cref="ResultVariable"/> is set only on the derived case an evaluated assertion hands to the engine, so the assertion expression
/// is evaluated through the same seam as the query.
/// </remarks>
internal sealed record Qt3Case(
    string TestSet,
    string Name,
    string Query,
    string? QueryFile,
    string? Environment,
    Qt3Assertion Assertion,
    IReadOnlyList<Qt3Dependency> Dependencies
)
{
    /// <summary>
    /// Gets the items bound to the variable <c>$result</c> when <see cref="Query"/> is the expression of an evaluated assertion;
    /// <see langword="null"/> for the query of the case itself.
    /// </summary>
    public IReadOnlyList<object>? ResultVariable { get; init; }

    /// <summary>Gets the stable case id, <c>&lt;test-set&gt;/&lt;case-name&gt;</c>.</summary>
    public string Id => TestSet + "/" + Name;
}
