namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Loading.XInclude;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// Runs the cases of the W3C XInclude test suite through the XInclude resolver of the Schematron loader and compares the merged tree
/// with the reference result through <see cref="XIncludeCanonicalForm"/>. The resolver is internal, so the runner drives it directly.
/// </summary>
/// <remarks>
/// Every resource is read through a <see cref="SandboxedFileSystemResourceResolver"/> rooted at the suite. A case that needs the
/// <c>xpointer()</c> scheme (DEF-58, #70) or the unparsed and unexpanded entities of a document type declaration (the tree keeps
/// neither) is skipped with its reason. An <c>error</c> case passes when the input is not well formed or the resolver reports a
/// problem; a <c>success</c> case passes when the merged tree equals the reference result.
/// </remarks>
internal static class XIncludeRunner
{
    private static readonly Dictionary<string, string> Unsupported = new(StringComparer.Ordinal)
    {
        ["xpointer-scheme"] = "Needs the xpointer() scheme, which is not supported (DEF-58, #70).",
        ["unexpanded-entities"] = "Needs unexpanded entity references, which the tree does not keep.",
        ["unparsed-entities"] = "Needs unparsed entities and notations, which the tree does not keep (#731).",
    };

    private const string LiveHttp =
        "Reads a live http resource with content negotiation; the suite runs offline and the resolver does no negotiation.";

    private static readonly HashSet<string> LiveHttpCases = new(StringComparer.Ordinal)
    {
        "harold-87",
        "harold-88",
        "harold-89",
        "harold-90",
    };

    /// <summary>Gets the reason a case is not run, or <see langword="null"/> when it is run.</summary>
    /// <param name="xcase">The case.</param>
    /// <returns>The reason.</returns>
    public static string? ExclusionReason(XIncludeCase xcase)
    {
        ArgumentNullException.ThrowIfNull(xcase);

        if (LiveHttpCases.Contains(xcase.Id))
        {
            return LiveHttp;
        }

        foreach (var feature in xcase.Features)
        {
            if (Unsupported.TryGetValue(feature, out var reason))
            {
                return reason;
            }
        }

        return null;
    }

    /// <summary>Runs the cases.</summary>
    /// <param name="root">The suite root.</param>
    /// <param name="cases">The cases.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case in the order of <paramref name="cases"/>; a skipped case carries its reason.</returns>
    public static IReadOnlyList<CaseResult> Run(
        string root,
        IReadOnlyList<XIncludeCase> cases,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(cases);

        var resolver = new SandboxedFileSystemResourceResolver(root);
        var results = new List<CaseResult>(cases.Count);
        foreach (var xcase in cases)
        {
            var reason = ExclusionReason(xcase);
            var failure = reason is null ? Judge(root, resolver, xcase, cancellationToken) : null;
            results.Add(
                reason is not null ? new CaseResult(xcase.Id, xcase.TestSet, CaseOutcome.Skipped, reason)
                : failure is null ? new CaseResult(xcase.Id, xcase.TestSet, CaseOutcome.Pass)
                : new CaseResult(xcase.Id, xcase.TestSet, CaseOutcome.Fail, failure)
            );
        }

        return results;
    }

    // Returns the failure reason, or null when the case holds.
    private static string? Judge(
        string root,
        SandboxedFileSystemResourceResolver resolver,
        XIncludeCase xcase,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(root, xcase.InputPath);
        var systemId = new Uri(path).AbsoluteUri;
        XmlNode merged;
        try
        {
            var tree = XmlDocumentLoader.Load(File.ReadAllBytes(path), baseUri: new Uri(systemId));
            var problems = new FakeProblemSink();
            var inclusion = new XIncludeResolver(resolver, ValidationLimits.Default);
            if (!inclusion.TryResolve(tree.Document, systemId, problems, cancellationToken, out merged))
            {
                return xcase.Type == XIncludeCaseType.Error
                    ? null
                    : $"Expected success but the resolver reported {problems.Problems[0].Code}: {problems.Problems[0].Message}";
            }
        }
        catch (XmlParseException failure)
        {
            return xcase.Type == XIncludeCaseType.Error ? null : "The input is not well formed: " + failure.Message;
        }

        if (xcase.Type == XIncludeCaseType.Error)
        {
            return "Expected a fatal error but the resolver reported none.";
        }

        return xcase.OutputPath is null ? null : Compare(root, xcase.OutputPath, merged);
    }

    private static string? Compare(string root, string outputPath, XmlNode merged)
    {
        string expected;
        try
        {
            var reference = XmlDocumentLoader.Load(File.ReadAllBytes(Path.Combine(root, outputPath)));
            expected = XIncludeCanonicalForm.Of(reference.Document);
        }
        catch (Exception failure) when (failure is XmlParseException or IOException)
        {
            return "The reference result cannot be read: " + failure.Message;
        }

        var actual = XIncludeCanonicalForm.Of(merged);
        return string.Equals(expected, actual, StringComparison.Ordinal)
            ? null
            : $"The merged tree differs from the reference result. Expected {Shorten(expected)} but was {Shorten(actual)}.";
    }

    private static string Shorten(string text) => text.Length <= 200 ? text : text[..200] + "...";
}
