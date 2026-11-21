namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;
using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>
/// The Schematron and Xsd assemblies hold no tree builder, datatype, facet, XPath parser, document loader, or document pool
/// of their own, each proven against fixture duplicates.
/// </summary>
public class SeamDuplicateTests
{
    private const string Fixtures = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Duplicates";

    // The seams named by the scope of #193; removing one makes the set test red.
    private static readonly string[] ContractSeams =
    [
        "TreeBuilder",
        "Datatype",
        "Facet",
        "XPathParser",
        "DocumentLoader",
        "DocumentPool",
    ];

    private static readonly Dictionary<string, string[]> ExpectedDuplicates = new(StringComparer.Ordinal)
    {
        [ContractSeams[0]] = ["PooledTreeBuilder", "TreeSinkFixture"],
        [ContractSeams[1]] = ["UnitDatatype", "ValueSpaceFixture"],
        [ContractSeams[2]] = ["BoundedFacet", "ConstraintFixture"],
        [ContractSeams[3]] = ["XPathExpressionParser"],
        [ContractSeams[4]] = ["XmlDocumentLoader"],
        [ContractSeams[5]] = ["XPathDocumentPool"],
    };

    private static Assembly[] Libraries =>
        [SourceAssemblies.Load(SourceAssemblies.Schematron), SourceAssemblies.Load(SourceAssemblies.Xsd)];

    public static IEnumerable<string> SeamNames() => ContractSeams;

    [Test]
    public async Task SeamSet_EqualsTheSeamsOfTheIssue() =>
        await Assert.That(SeamDuplicates.Seams.Select(seam => seam.Name)).IsEquivalentTo(ContractSeams);

    [Test]
    public async Task Libraries_HoldNoDuplicate_BeyondTheApprovedException() =>
        await Assert
            .That(
                SeamDuplicates
                    .Find(Libraries, SeamDuplicates.Seams)
                    .Except(SeamDuplicates.ApprovedExceptions, StringComparer.Ordinal)
            )
            .IsEmpty();

    [Test]
    public async Task ApprovedException_IsStillADuplicate_SoTheListCannotOutliveIt() =>
        await Assert
            .That(SeamDuplicates.Find(Libraries, SeamDuplicates.Seams))
            .IsEquivalentTo(SeamDuplicates.ApprovedExceptions);

    [Test]
    [MethodDataSource(nameof(SeamNames))]
    public async Task FixtureDuplicate_IsReportedForItsSeam(string seam)
    {
        var found = SeamDuplicates
            .Find([typeof(SeamDuplicateTests).Assembly], SeamDuplicates.Seams)
            .Where(entry => entry.StartsWith(seam + ": ", StringComparison.Ordinal));

        await Assert.That(found).IsEquivalentTo(ExpectedDuplicates[seam].Select(name => $"{seam}: {Fixtures}.{name}"));
    }
}
