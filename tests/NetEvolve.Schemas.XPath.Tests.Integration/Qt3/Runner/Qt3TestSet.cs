namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>One QT3 test set.</summary>
/// <param name="Name">The test set name.</param>
/// <param name="Dependencies">The set-level dependencies.</param>
/// <param name="Cases">The cases in file order.</param>
internal sealed record Qt3TestSet(string Name, IReadOnlyList<Qt3Dependency> Dependencies, IReadOnlyList<Qt3Case> Cases);
