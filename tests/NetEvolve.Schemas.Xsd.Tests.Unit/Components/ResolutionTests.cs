namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class ResolutionTests
{
    [Test]
    public async Task ForwardReference_RegisteredLater_IsBound()
    {
        var registry = NewRegistry(out var collector);
        var first = new RefComponent(XsdComponentKind.ElementDeclaration, "e", 1, Ref(XsdSymbolSpace.Types, "later"));
        var later = new RefComponent(XsdComponentKind.ComplexType, "later", 9);
        registry.Add(first);
        registry.Add(later);

        var issues = Resolve(registry, collector);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(first.Bound[0]).IsSameReferenceAs(later);
    }

    [Test]
    public async Task ReferenceCycle_ThroughElementAndType_IsNotACircularDefinition()
    {
        var registry = NewRegistry(out var collector);
        registry.Add(
            new RefComponent(XsdComponentKind.ComplexType, "t", 1, Ref(XsdSymbolSpace.Elements, "e", structural: false))
        );
        registry.Add(
            new RefComponent(
                XsdComponentKind.ElementDeclaration,
                "e",
                2,
                Ref(XsdSymbolSpace.Types, "t", structural: false)
            )
        );

        await Assert.That(Resolve(registry, collector).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("SimpleType", "Types", "st-props-correct")]
    [Arguments("ComplexType", "Types", "ct-props-correct")]
    [Arguments("ModelGroupDefinition", "Groups", "mg-props-correct")]
    [Arguments("AttributeGroup", "AttributeGroups", "ag-props-correct")]
    public async Task CircularDefinition_IsALocatedSchemaError(string kindName, string spaceName, string rule)
    {
        var kind = Enum.Parse<XsdComponentKind>(kindName);
        var space = Enum.Parse<XsdSymbolSpace>(spaceName);
        var registry = NewRegistry(out var collector);
        registry.Add(new RefComponent(kind, "a", 3, Ref(space, "b", 4)));
        registry.Add(new RefComponent(kind, "b", 7, Ref(space, "c", 8)));
        registry.Add(new RefComponent(kind, "c", 11, Ref(space, "a", 12)));

        var issues = Resolve(registry, collector, XsdVersion.V10);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf(rule));
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Location.Source).IsEqualTo("file:///a.xsd");
    }

    [Test]
    public async Task CircularType_ThroughSelfReference_IsReportedOnce()
    {
        var registry = NewRegistry(out var collector);
        registry.Add(new RefComponent(XsdComponentKind.SimpleType, "s", 2, Ref(XsdSymbolSpace.Types, "s", 3)));

        var issues = Resolve(registry, collector);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("st-props-correct.2");
    }

    [Test]
    public async Task CircularAttributeGroup_IsAllowedInVersion11()
    {
        var registry = NewRegistry(out var collector);
        var space = XsdSymbolSpace.AttributeGroups;
        registry.Add(new RefComponent(XsdComponentKind.AttributeGroup, "a", 1, Ref(space, "b")));
        registry.Add(new RefComponent(XsdComponentKind.AttributeGroup, "b", 2, Ref(space, "a")));

        await Assert.That(Resolve(registry, collector, XsdVersion.V11).Count).IsEqualTo(0);
    }

    [Test]
    public async Task MissingComponent_IsSrcResolveAtTheReference()
    {
        var registry = NewRegistry(out var collector);
        registry.Add(
            new RefComponent(XsdComponentKind.ElementDeclaration, "e", 1, Ref(XsdSymbolSpace.Types, "nope", 5))
        );

        var issues = Resolve(registry, collector);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-resolve"));
        await Assert.That(issues[0].Location.Line).IsEqualTo(5);
        await Assert.That(issues[0].Message).Contains("urn:test").And.Contains("nope");
    }

    [Test]
    public async Task Lookup_OfMissingComponent_IsFalse()
    {
        var registry = NewRegistry(out _);

        await Assert.That(registry.TryGet(XsdComponentKind.ElementDeclaration, Name("none"), out var found)).IsFalse();
        await Assert.That(found).IsNull();
    }
}
