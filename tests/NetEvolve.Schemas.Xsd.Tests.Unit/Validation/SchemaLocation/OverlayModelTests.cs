namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

public class OverlayModelTests
{
    private static readonly XPathQName BaseRoot = new(HintFixture.Base, "r");
    private static readonly XPathQName HintedRoot = new(HintFixture.Hinted, "root");
    private static readonly XPathQName HintedChild = new(HintFixture.Hinted, "a");

    private static SchemaCompilation Compile(string schema) =>
        new SchemaCompiler(XsdVersion.V11, DenyAllResourceResolver.Instance, ValidationLimits.Default).Compile(
            schema.AsSpan(),
            "file:///x.xsd",
            CancellationToken.None
        );

    private const string SequenceSchema =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\"urn:t\" elementFormDefault=\"qualified\"><xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"x\" type=\"xs:int\"/><xs:element name=\"y\" type=\"xs:int\"/></xs:sequence></xs:complexType></xs:element></xs:schema>";

    [Test]
    public async Task Extend_KeepsTheIdsAndProgramsOfTheBaseAndNumbersTheNewOnesAfterThem()
    {
        var baseModel = Compile(SequenceSchema).Model!;
        var overlay = Compile(HintFixture.HintedSchema);
        var set = overlay.Set!.WithParent(baseModel.Schema);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        var extended = baseModel.Extend(set, overlay.Components, reporter);

        await Assert.That(baseModel.TryGetNameId(new XPathQName("urn:t", "x"), out var x)).IsTrue();
        await Assert.That(baseModel.TryGetNameId(new XPathQName("urn:t", "y"), out var y)).IsTrue();
        await Assert.That(extended.TryGetNameId(new XPathQName("urn:t", "x"), out var keptX)).IsTrue();
        await Assert.That(extended.TryGetNameId(new XPathQName("urn:t", "y"), out var keptY)).IsTrue();
        await Assert.That(keptX).IsEqualTo(x);
        await Assert.That(keptY).IsEqualTo(y);
        await Assert.That(extended.TryGetNameId(HintedChild, out var id)).IsTrue();
        await Assert.That(id).IsGreaterThan(y);
        await Assert.That(extended.NameOf(id)).IsEqualTo(HintedChild);
        await Assert.That(extended.ProgramCount).IsGreaterThan(baseModel.ProgramCount);
    }

    [Test]
    public async Task Hints_Overlay_DoesNotMutateBaseSet_AcrossThreads()
    {
        var baseModel = Compile(HintFixture.BaseSchema).Model!;
        var countBefore = baseModel.Schema.Count;
        var programsBefore = baseModel.ProgramCount;
        var overlay = Compile(HintFixture.HintedSchema);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));

        _ = Parallel.For(0, 32, _ =>
            baseModel.Extend(overlay.Set!.WithParent(baseModel.Schema), overlay.Components, reporter)
        );

        await Assert.That(baseModel.Schema.Count).IsEqualTo(countBefore);
        await Assert.That(baseModel.ProgramCount).IsEqualTo(programsBefore);
        await Assert.That(baseModel.Schema.TryGet(XsdComponentKind.ElementDeclaration, HintedRoot, out _)).IsFalse();
        await Assert.That(baseModel.TryGetNameId(HintedChild, out _)).IsFalse();
        await Assert.That(baseModel.Schema.TryGet(XsdComponentKind.ElementDeclaration, BaseRoot, out _)).IsTrue();
    }

    [Test]
    public async Task Hints_Overlay_SharedValidatorServesConcurrentRunsWithAndWithoutHints()
    {
        var validator = HintFixture.Build(HintFixture.Resolver());
        var hinted = HintFixture.HintedRoot("xsi:schemaLocation=\"urn:h h.xsd\"", "<h:a>1</h:a>");
        var plain = HintFixture.HintedRoot(string.Empty, "<h:a>1</h:a>");

        var results = new bool[64];
        _ = Parallel.For(0, results.Length, i =>
            results[i] = HintFixture.Run(validator, i % 2 == 0 ? hinted : plain).IsValid
        );

        await Assert.That(results.Where((_, i) => i % 2 == 0).All(valid => valid)).IsTrue();
        await Assert.That(results.Where((_, i) => i % 2 != 0).Any(valid => valid)).IsFalse();
    }

    [Test]
    public async Task DefinesNamespace_AnswersForTheSetAndItsParent()
    {
        var baseSet = Compile(HintFixture.BaseSchema).Set!;
        var overlay = Compile(HintFixture.HintedSchema).Set!.WithParent(baseSet);

        await Assert.That(baseSet.DefinesNamespace(HintFixture.Base)).IsTrue();
        await Assert.That(baseSet.DefinesNamespace(HintFixture.Xs)).IsTrue();
        await Assert.That(baseSet.DefinesNamespace(HintFixture.Hinted)).IsFalse();
        await Assert.That(baseSet.DefinesNamespace(string.Empty)).IsFalse();
        await Assert.That(overlay.DefinesNamespace(HintFixture.Base)).IsTrue();
        await Assert.That(overlay.DefinesNamespace(HintFixture.Hinted)).IsTrue();
        await Assert.That(() => baseSet.DefinesNamespace(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Overlay_TheSchemaSetFindsTheParentFirstAndCountsBoth()
    {
        var baseModel = Compile(HintFixture.BaseSchema).Model!;
        var overlay = Compile(HintFixture.HintedSchema);

        var set = overlay.Set!.WithParent(baseModel.Schema);

        await Assert.That(set.Parent).IsSameReferenceAs(baseModel.Schema);
        await Assert.That(set.Count).IsEqualTo(overlay.Set.Count + baseModel.Schema.Count);
        await Assert.That(set.TryGet(XsdComponentKind.ElementDeclaration, BaseRoot, out var fromParent)).IsTrue();
        await Assert.That(set.TryGet(XsdComponentKind.ElementDeclaration, HintedRoot, out var own)).IsTrue();
        await Assert.That(fromParent).IsNotSameReferenceAs(own);
        await Assert
            .That(set.TryGet(XsdComponentKind.ElementDeclaration, new XPathQName("urn:x", "r"), out _))
            .IsFalse();
    }
}
