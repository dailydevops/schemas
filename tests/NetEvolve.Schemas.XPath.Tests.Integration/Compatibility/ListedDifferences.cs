namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;

/// <summary>
/// The cases in which the library in compatibility mode gives another result than System.Xml.XPath, each with the result of the
/// library (<c>Ours</c>) and the reason. A reason is a rule of XPath 2.0 (Appendix I, or the sections it names) that makes the difference follow
/// from the specification; a difference without a rule is a defect and has its own issue instead. A difference that is not listed
/// fails the tests, and so does a listed difference that has gone.
/// </summary>
internal static class ListedDifferences
{
    /// <summary>Gets the listed differences by <see cref="CorpusCase.Key"/>.</summary>
    public static IReadOnlyDictionary<string, (string Ours, string Reason)> All { get; } =
        new Dictionary<string, (string Ours, string Reason)>(StringComparer.Ordinal) { };
}
