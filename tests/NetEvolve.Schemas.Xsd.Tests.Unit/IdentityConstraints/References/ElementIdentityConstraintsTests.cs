namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.References;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.References;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class ElementIdentityConstraintsTests
{
    // A document without targetNamespace that another schema includes is a chameleon (Structures 3.15.3).
    private static readonly SchemaNamespaceScope ChameleonScope = new("urn:t", true);

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

    private List<ElementIdentityConstraints> BuildChameleon(params string[][] elements)
    {
        var text = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">\n";
        for (var i = 0; i < elements.Length; i++)
        {
            text +=
                "<xs:element name=\"e"
                + i
                + "\" type=\"xs:string\">\n"
                + string.Join("\n", elements[i])
                + "\n</xs:element>\n";
        }

        var result = new List<ElementIdentityConstraints>();
        foreach (var node in ReferenceFixture.Elements(text + "</xs:schema>"))
        {
            result.Add(ElementIdentityConstraints.Create(node, ChameleonScope, _registry, XsdVersion.V11, Reporter));
        }

        foreach (var holder in result)
        {
            holder.Resolve(_registry, Reporter);
        }

        return result;
    }

    [Test]
    public async Task Create_NamedConstraintOfAChameleonDocument_IsRegisteredInTheIncludingNamespace()
    {
        var holders = BuildChameleon([ReferenceFixture.Definition("key", "k")]);

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(holders[0].Constraints[0].Name).IsEqualTo(new XPathQName("urn:t", "k"));
        await Assert
            .That(_registry.TryGet(XsdComponentKind.IdentityConstraint, new XPathQName("urn:t", "k"), out _))
            .IsTrue();
        await Assert
            .That(_registry.TryGet(XsdComponentKind.IdentityConstraint, new XPathQName(string.Empty, "k"), out _))
            .IsFalse();
    }

    [Test]
    public async Task Resolve_RefToAChameleonConstraint_FindsItThroughTheIncludingNamespace()
    {
        var holders = BuildChameleon(["<xs:key ref=\"k\"/>"], [ReferenceFixture.Definition("key", "k")]);

        await Assert.That(_collector.Count).IsEqualTo(0);
        await Assert.That(holders[0].Constraints).Count().IsEqualTo(1);
        await Assert.That(holders[0].Constraints[0]).IsSameReferenceAs(holders[1].Constraints[0]);
    }

    [Test]
    public async Task ResolveRefer_KeyrefOfAChameleonDocument_FindsTheKeyThroughTheIncludingNamespace()
    {
        var holders = BuildChameleon([
            ReferenceFixture.Definition("key", "k"),
            "<xs:keyref name=\"r\" refer=\"k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:keyref>",
        ]);
        var key = holders[0].Constraints[0];
        var keyref = holders[0].Constraints[1];

        keyref.ResolveRefer(_registry, Reporter);

        await Assert.That(keyref.Refer).IsEqualTo(new XPathQName("urn:t", "k"));
        await Assert.That(keyref.ReferencedConstraint).IsSameReferenceAs(key);
        await Assert.That(_collector.Count).IsEqualTo(0);
    }
}
