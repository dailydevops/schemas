namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The split of the suite into cases that run and cases that are excluded.</summary>
/// <param name="Selected">The cases to run, sorted by test set and id.</param>
/// <param name="Excluded">The cases left out, each with a reason.</param>
internal sealed record XstsSelection(
    IReadOnlyList<XstsSelectedCase> Selected,
    IReadOnlyList<XstsExcludedCase> Excluded
);
