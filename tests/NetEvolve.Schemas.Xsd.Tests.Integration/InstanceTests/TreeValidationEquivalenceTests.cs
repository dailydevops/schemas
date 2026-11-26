namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using System.Collections.Concurrent;
using System.Text;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// Tree validation (<see cref="IXsdValidator.ValidateTree(XmlTree, CancellationToken)"/>) gives the same issues, by rule code and
/// source, as the streaming path for every instance test of XSD 1.0 of the suite (XSD-79, #361). A malformed instance cannot be loaded
/// into a tree, and a document type declaration is not part of the tree (#731); both are listed one by one in
/// <c>tree-exclusions.txt</c>, and any other difference is a defect and fails the test. The location is not compared: a tree keeps the
/// start position of elements only, so the streaming path alone locates attributes, character data, and end tags exactly (#606).
/// </summary>
[Explicit]
[Category(ConformanceCategory.Name)]
public class TreeValidationEquivalenceTests
{
    private const string Unloadable = "unloadable";
    private const string Doctype = "doctype";
    private const string Defect = "defect";
    private const string Agreed = "agreed";

    // The limits of the validator that the instances run through, so that a limit of the loader is not a difference of the two paths.
    private static readonly XmlReaderOptions LoaderOptions = XmlReaderOptions
        .Default.WithDtdProcessing(true)
        .WithLimits(
            XmlReaderLimits
                .Default.WithMaxElementDepth(ValidationLimits.Default.MaxDepth)
                .WithMaxAttributesPerElement(ValidationLimits.Default.MaxAttributesPerElement)
                .WithMaxEntityExpansionCharacters(ValidationLimits.Default.MaxEntityExpansion)
                .WithMaxEntityDepth(ValidationLimits.Default.MaxEntityDepth)
        );

    private static readonly string ListPath = Path.Combine(
        XstsSuite.RepositoryRoot,
        "tests",
        "NetEvolve.Schemas.Xsd.Tests.Integration",
        "InstanceTests",
        "tree-exclusions.txt"
    );

    private static async Task<List<string[]>> ReadListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>();
        foreach (var line in await File.ReadAllLinesAsync(ListPath, cancellationToken))
        {
            if (line.Length > 0 && line[0] != '#')
            {
                rows.Add(line.Split('\t'));
            }
        }

        return rows;
    }

    private static string Describe(ValidationResult result)
    {
        var text = new StringBuilder();
        foreach (var issue in result.Issues)
        {
            _ = text.Append(issue.Code).Append('@').Append(issue.Location.Source).Append('\n');
        }

        return text.Append(result.IsValid).Append(result.IsTruncated).ToString();
    }

    // The kind of the difference, Agreed when the two paths give the same issues, null when the case cannot be observed (the same cases the instance runner skips).
    private static async Task<string?> CompareAsync(
        XstsSelectedCase xstsCase,
        InstanceXstsValidator validators,
        string suiteRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var instance = xstsCase.Case.InstanceDocument;
        if (instance is null || xstsCase.Case.SchemaDocuments.Count == 0)
        {
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(suiteRoot, instance));
        var validator = await validators.ValidatorOf(xstsCase);
        if (validator is null || !File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return Compare(validator, bytes, new Uri(path).AbsoluteUri, cancellationToken);
    }

    private static string? Compare(
        IXsdValidator validator,
        byte[] bytes,
        string uri,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidationResult streamed;
        try
        {
            streamed = validator.Validate(bytes, uri, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return null;
        }

        XmlDocumentTree tree;
        try
        {
            tree = XmlDocumentLoader.Load(bytes, LoaderOptions, new Uri(uri));
        }
        catch (XmlParseException)
        {
            return Unloadable;
        }

        var result = validator.ValidateTree(tree, cancellationToken);
        if (string.Equals(Describe(result.Result), Describe(streamed), StringComparison.Ordinal))
        {
            return Agreed;
        }

        return Encoding.UTF8.GetString(bytes).Contains("<!DOCTYPE", StringComparison.Ordinal) ? Doctype : Defect;
    }

    [Test]
    public async Task Instances_TreeValidationGivesTheIssuesOfTheStreamingPath(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var root = await XstsSuite.LoadRootAsync(cancellationToken);
        var validators = new InstanceXstsValidator(root);
        var cases = selection
            .Selected.Where(c =>
                c.Case.Kind == XstsKind.Instance && string.Equals(c.Version, XstsVersions.V10, StringComparison.Ordinal)
            )
            .ToList();
        var observed = new ConcurrentBag<string>();
        var compared = 0;

        using var gate = new SemaphoreSlim(8);
        await Task.WhenAll(
            cases.Select(async xstsCase =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    var kind = await CompareAsync(xstsCase, validators, root, cancellationToken);
                    if (kind is not null)
                    {
                        _ = Interlocked.Increment(ref compared);
                    }

                    if (kind is not null and not Agreed)
                    {
                        observed.Add(xstsCase.TestSet + "	" + xstsCase.Id + "	" + kind);
                    }
                }
                finally
                {
                    _ = gate.Release();
                }
            })
        );

        var listed = (await ReadListAsync(cancellationToken)).Select(r => r[0] + "\t" + r[1] + "\t" + r[2]).ToList();
        var difference = observed.OrderBy(l => l, StringComparer.Ordinal).ToList();
        Directory.CreateDirectory(Path.Combine(XstsSuite.RepositoryRoot, "artifacts"));
        await File.WriteAllLinesAsync(
            Path.Combine(XstsSuite.RepositoryRoot, "artifacts", "tree-exclusions.observed.txt"),
            difference,
            cancellationToken
        );

        Console.WriteLine("Compared " + compared + " of " + cases.Count + " instance tests.");
        await Assert.That(compared).IsGreaterThan(10000);
        await Assert.That(difference.Any(l => l.EndsWith("\t" + Defect, StringComparison.Ordinal))).IsFalse();
        await Assert.That(difference).IsEquivalentTo(listed.OrderBy(l => l, StringComparer.Ordinal));
    }

    [Test]
    public async Task List_EveryLineHasATestSetACaseAKindAnIssueAndAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await ReadListAsync(cancellationToken);

        await Assert.That(rows).IsNotEmpty();
        foreach (var row in rows)
        {
            await Assert.That(row.Length).IsEqualTo(5);
            await Assert.That(row[0]).IsNotEmpty();
            await Assert.That(row[1].StartsWith("1.0/", StringComparison.Ordinal)).IsTrue();
            await Assert.That(row[2] is Unloadable or Doctype).IsTrue();
            await Assert
                .That(string.Equals(row[3], "-", StringComparison.Ordinal) || (row[3].Length > 1 && row[3][0] == '#'))
                .IsTrue();
            await Assert.That(row[4].Length).IsGreaterThan(10);
        }

        await Assert
            .That(rows.Select(r => r[0] + "|" + r[1]).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(rows.Count);
    }
}
