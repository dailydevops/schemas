namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The schema and instance cases of the XSTS 1.0 corpus as differential cases.</summary>
internal static class DifferentialCorpus
{
    /// <summary>Gets the case id used in the deviation files: the test set and the case id of the selection.</summary>
    /// <param name="selected">The selected case.</param>
    /// <returns>The id, <c>testSet/version/group/name</c>.</returns>
    public static string IdOf(XstsSelectedCase selected) => selected.TestSet + "/" + selected.Id;

    /// <summary>Selects the cases of XSD 1.0, sorted by id.</summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>One differential case per selected case of XSD 1.0.</returns>
    public static async Task<IReadOnlyList<DifferentialCase>> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var root = await XstsSuite.LoadRootAsync(cancellationToken);
        var cases = new List<DifferentialCase>();
        foreach (var selected in selection.Selected)
        {
            if (!string.Equals(selected.Version, XstsVersions.V10, StringComparison.Ordinal))
            {
                continue;
            }

            cases.Add(CaseOf(selected, root));
        }

        cases.Sort(static (a, b) => string.CompareOrdinal(a.Id, b.Id));
        return cases;
    }

    internal static DifferentialCase CaseOf(XstsSelectedCase selected, string root)
    {
        var schemas = new List<string>(selected.Case.SchemaDocuments.Count);
        foreach (var relative in selected.Case.SchemaDocuments)
        {
            schemas.Add(Path.GetFullPath(Path.Combine(root, relative)));
        }

        var verdict = selected.ExpectValid ? Verdict.Valid : Verdict.Invalid;
        return selected.Case.Kind == XstsKind.Schema
            ? new DifferentialCase(IdOf(selected), schemas, null, verdict, Verdict.NotRun)
            : new DifferentialCase(
                IdOf(selected),
                schemas,
                Path.GetFullPath(Path.Combine(root, selected.Case.InstanceDocument!)),
                Verdict.Valid,
                verdict
            );
    }
}
