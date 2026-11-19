namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.References;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.References;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class ElementIdentityConstraintsTests
{
    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);
    private readonly FakeComponentRegistry _registry = new([]);

    private XsdIssueReporter Reporter => new(_collector);

    // Phase one runs for every element before phase two, so a reference may name a constraint of a later element.
    private List<ElementIdentityConstraints> Build(XsdVersion version, params string[][] elements)
    {
        var nodes = ReferenceFixture.Elements(ReferenceFixture.Schema("urn:t", elements));
        var result = new List<ElementIdentityConstraints>();
        foreach (var node in nodes)
        {
            result.Add(ElementIdentityConstraints.Create(node, ReferenceFixture.Scope, _registry, version, Reporter));
        }

        foreach (var holder in result)
        {
            holder.Resolve(_registry, Reporter);
        }

        return result;
    }

    [Test]
    [Arguments("unique", "Unique")]
    [Arguments("key", "Key")]
    [Arguments("keyref", "Keyref")]
    public async Task Resolve_RefToAConstraintOfALaterElement_HoldsTheSameInstance(string kind, string expected)
    {
        var holders = Build(XsdVersion.V11, [$"<xs:{kind} ref=\"t:k\"/>"], [ReferenceFixture.Definition(kind, "k")]);

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(holders[0].Constraints).Count().IsEqualTo(1);
        await Assert.That(holders[1].Constraints).Count().IsEqualTo(1);
        await Assert.That(holders[0].Constraints[0]).IsSameReferenceAs(holders[1].Constraints[0]);
        await Assert.That(holders[0].Constraints[0].ConstraintKind.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Resolve_OwnDefinitionsAndReferences_KeepDocumentOrder()
    {
        var holders = Build(
            XsdVersion.V11,
            [
                ReferenceFixture.Definition("unique", "u"),
                "<xs:key ref=\"t:k\"/>",
                ReferenceFixture.Definition("key", "own"),
            ],
            [ReferenceFixture.Definition("key", "k")]
        );

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(holders[0].Constraints).Count().IsEqualTo(3);
        await Assert.That(holders[0].Constraints[0].Name.LocalName).IsEqualTo("u");
        await Assert.That(holders[0].Constraints[1]).IsSameReferenceAs(holders[1].Constraints[0]);
        await Assert.That(holders[0].Constraints[2].Name.LocalName).IsEqualTo("own");
    }

    [Test]
    public async Task Resolve_UnresolvedRef_IsReportedOnceAndLeftOut()
    {
        var holders = Build(XsdVersion.V11, ["<xs:key ref=\"t:missing\"/>", ReferenceFixture.Definition("key", "own")]);

        await Assert.That(holders[0].Constraints).Count().IsEqualTo(1);
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ReferenceFixture.CodeOf("src-resolve"));
    }

    [Test]
    public async Task Create_RefInStrict10_IsSchemaErrorAndHoldsNothing()
    {
        var holders = Build(XsdVersion.V10, ["<xs:key ref=\"t:k\"/>"], [ReferenceFixture.Definition("key", "k")]);

        await Assert.That(holders[0].Constraints).IsEmpty();
        await Assert.That(holders[1].Constraints).Count().IsEqualTo(1);
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ReferenceFixture.CodeOf("strict10-identity-constraint-ref"));
    }
}
