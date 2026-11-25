namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage5Grouping;

using NetEvolve.Arguments;

/// <summary>One own test document with the rules it must fire.</summary>
internal sealed record Stage5Document
{
    /// <summary>Initializes a new instance of the <see cref="Stage5Document"/> class.</summary>
    /// <param name="path">The path of the document relative to the <c>documents</c> folder of the stage.</param>
    /// <param name="expectedValid">Whether the document is valid: no failed assertion and no successful report.</param>
    /// <param name="expectedRuleIds">The ids of the rules that fire, compared as a set.</param>
    /// <param name="parameters">The external parameters the validator is built with.</param>
    public Stage5Document(
        string path,
        bool expectedValid,
        IReadOnlyList<string> expectedRuleIds,
        IReadOnlyDictionary<string, string> parameters
    )
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(expectedRuleIds);
        ArgumentNullException.ThrowIfNull(parameters);
        Path = path;
        ExpectedValid = expectedValid;
        ExpectedRuleIds = expectedRuleIds;
        Parameters = parameters;
    }

    /// <summary>Gets the path of the document relative to the <c>documents</c> folder of the stage.</summary>
    public string Path { get; init; }

    /// <summary>Gets a value indicating whether the document is valid: no failed assertion and no successful report.</summary>
    public bool ExpectedValid { get; init; }

    /// <summary>Gets the ids of the rules that fire, compared as a set.</summary>
    public IReadOnlyList<string> ExpectedRuleIds { get; init; }

    /// <summary>Gets the external parameters the validator is built with.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; }
}
