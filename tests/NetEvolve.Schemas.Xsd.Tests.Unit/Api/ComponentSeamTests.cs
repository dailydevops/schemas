namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class ComponentSeamTests
{
    [Test]
    public async Task Seam_BuilderThenResolver_RunInDocumentedOrderWithTheirArguments()
    {
        var log = new List<string>();
        var schema = new FakeSyntaxElement("schema", 1)
            .WithChild(new FakeSyntaxElement("element", 2).WithAttribute("name", "a"))
            .WithChild(new FakeSyntaxElement("element", 3).WithAttribute("name", "b"));
        var registry = new FakeComponentRegistry(log);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var builder = new FakeComponentBuilder(XsdComponentKind.ElementDeclaration, log);
        var resolver = new FakeComponentResolver(log);

        for (var i = 0; i < schema.ChildCount; i++)
        {
            _ = builder.Build(schema.GetChild(i), registry, reporter);
        }

        resolver.Resolve(registry, reporter);

        await Assert
            .That(string.Join("|", log))
            .IsEqualTo("Build:element@2|Add:ElementDeclaration:a|Build:element@3|Add:ElementDeclaration:b|Resolve:2");
    }

    [Test]
    public async Task Seam_Builder_ReturnsRegisteredComponentWithKindNameAndLocation()
    {
        var log = new List<string>();
        var registry = new FakeComponentRegistry(log);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var builder = new FakeComponentBuilder(XsdComponentKind.SimpleType, log);
        var element = new FakeSyntaxElement("simpleType", 7).WithAttribute("name", "code");

        var component = builder.Build(element, registry, reporter);
        var found = registry.TryGet(XsdComponentKind.SimpleType, new XPathQName("urn:test", "code"), out var lookedUp);
        var missing = registry.TryGet(XsdComponentKind.ComplexType, new XPathQName("urn:test", "code"), out _);

        await Assert.That(builder.Kind).IsEqualTo(XsdComponentKind.SimpleType);
        await Assert.That(component.Kind).IsEqualTo(XsdComponentKind.SimpleType);
        await Assert.That(component.Name).IsEqualTo(new XPathQName("urn:test", "code"));
        await Assert.That(component.Location.Line).IsEqualTo(7);
        await Assert.That(found).IsTrue();
        await Assert.That(lookedUp).IsSameReferenceAs(component);
        await Assert.That(missing).IsFalse();
    }

    [Test]
    public async Task Seam_SyntaxElement_ExposesNamesAttributesAndChildrenInDocumentOrder()
    {
        var root = new FakeSyntaxElement("schema", 1)
            .WithAttribute("targetNamespace", "urn:t")
            .WithAttribute("elementFormDefault", "qualified")
            .WithChild(new FakeSyntaxElement("import", 2))
            .WithChild(new FakeSyntaxElement("element", 3));
        var syntax = root;

        await Assert.That(syntax.Name.LocalName).IsEqualTo("schema");
        await Assert.That(syntax.AttributeCount).IsEqualTo(2);
        await Assert.That(syntax.GetAttribute(0).Name.LocalName).IsEqualTo("targetNamespace");
        await Assert.That(syntax.GetAttribute(1).Value).IsEqualTo("qualified");
        await Assert.That(syntax.ChildCount).IsEqualTo(2);
        await Assert.That(syntax.GetChild(0).Name.LocalName).IsEqualTo("import");
        await Assert.That(syntax.GetChild(1).Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Seam_AnonymousComponent_HasDefaultName()
    {
        var component = new FakeComponent(XsdComponentKind.ComplexType, string.Empty);

        await Assert.That(component.Name).IsEqualTo(default(XPathQName));
    }
}
