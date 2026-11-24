namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using NetEvolve.Arguments;

/// <summary>One document of the CEN EN 16931 corpus with the rules it must fire.</summary>
/// <remarks>
/// The unit-test corpus keeps many documents in one test-set file. A rule fires per document when an assertion with its id
/// fails; the rules are compared as a set of ids, because a rule fires once per matching context node.
/// </remarks>
internal sealed record En16931Case
{
    /// <summary>Initializes a new instance of the <see cref="En16931Case"/> class.</summary>
    /// <param name="syntax">The syntax, <c>ubl</c> or <c>cii</c>.</param>
    /// <param name="id">The stable case id, <c>&lt;file&gt;#&lt;position&gt;</c> or the file name of an example.</param>
    /// <param name="document">The UTF-8 bytes of the document.</param>
    /// <param name="scope">
    /// The ids the case is about; a rule outside the scope is ignored, because the documents of the corpus are fragments that
    /// break many other rules on purpose. Empty for an example, which has no scope.
    /// </param>
    /// <param name="expectedRuleIds">The ids of the rules that must fire; empty for a document that is valid.</param>
    /// <param name="isExample">Whether the document is an official example that must be valid.</param>
    public En16931Case(
        string syntax,
        string id,
        byte[] document,
        IReadOnlyList<string> scope,
        IReadOnlyList<string> expectedRuleIds,
        bool isExample
    )
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(syntax);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(expectedRuleIds);
        Syntax = syntax;
        Id = id;
        Document = document;
        Scope = scope;
        ExpectedRuleIds = expectedRuleIds;
        IsExample = isExample;
    }

    /// <summary>Gets the syntax, <c>ubl</c> or <c>cii</c>.</summary>
    public string Syntax { get; init; }

    /// <summary>Gets the stable case id.</summary>
    public string Id { get; init; }

    /// <summary>Gets the UTF-8 bytes of the document.</summary>
    public byte[] Document { get; init; }

    /// <summary>Gets the ids the case is about; empty for an example.</summary>
    public IReadOnlyList<string> Scope { get; init; }

    /// <summary>Gets the ids of the rules that must fire.</summary>
    public IReadOnlyList<string> ExpectedRuleIds { get; init; }

    /// <summary>Gets a value indicating whether the document is an official example that must be valid.</summary>
    public bool IsExample { get; init; }
}
