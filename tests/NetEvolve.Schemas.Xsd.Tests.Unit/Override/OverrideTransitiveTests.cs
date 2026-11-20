namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Override.OverrideFixture;

/// <summary>
/// The override applies to the whole target set: the documents the target includes and the documents it overrides
/// (Structures 1.1, 4.2.5 and F.2). Cycles end because a unit that would be the same is not loaded again.
/// </summary>
public class OverrideTransitiveTests
{
    private const string Simple = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>";
    private const string Replacement = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>";

    private static string Override(string location, string children) =>
        "\n  <xs:override schemaLocation=\"" + location + "\">" + children + "</xs:override>";

    [Test]
    public async Task Override_AppliesToAComponentOfADocumentTheTargetIncludes()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/><xs:element name=\"E\"/>"))
            .Add("file:///b.xsd", Schema(Simple + "<xs:element name=\"F\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(Names(set.Units[1])).IsEqualTo("element:E");
        await Assert.That(Names(set.Units[2])).IsEqualTo("simpleType:T,element:F");
        await Assert.That(ReferenceEquals(set.Units[2].Components[0], OverrideChild(set.Units[0], 0, 0))).IsTrue();
        await Assert.That(set.Units[1].Included.Contains(set.Units[2])).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Override_AppliesToAComponentOfADocumentTheTargetOverrides_AndTheOuterOverrideWins()
    {
        var resolver = new MapResolver()
            .Add(
                "file:///a.xsd",
                Schema(
                    Override(
                        "b.xsd",
                        "<xs:element name=\"T\" type=\"xs:byte\"/><xs:element name=\"U\" type=\"xs:byte\"/>"
                    )
                )
            )
            .Add("file:///b.xsd", Schema("<xs:element name=\"T\"/><xs:element name=\"U\"/><xs:element name=\"V\"/>"));

        var (set, issues) = Load(
            Schema(
                Override("a.xsd", "<xs:element name=\"T\" type=\"xs:int\"/><xs:element name=\"V\" type=\"xs:int\"/>")
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(Names(set.Units[2])).IsEqualTo("element:T,element:U,element:V");

        var outer = set.Units[0];
        var inner = set.Units[1];
        await Assert.That(ReferenceEquals(Component(set.Units[2], "T"), OverrideChild(outer, 0, 0))).IsTrue();
        await Assert.That(ReferenceEquals(Component(set.Units[2], "U"), OverrideChild(inner, 0, 1))).IsTrue();
        await Assert.That(ReferenceEquals(Component(set.Units[2], "V"), OverrideChild(outer, 0, 1))).IsTrue();
    }

    [Test]
    public async Task Override_DoesNotReachADocumentTheTargetImportsOrRedefines()
    {
        var resolver = new MapResolver()
            .Add(
                "file:///a.xsd",
                Schema(
                    "<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/><xs:redefine schemaLocation=\"c.xsd\"/>"
                )
            )
            .Add("file:///b.xsd", Schema(Simple, "urn:b"))
            .Add("file:///c.xsd", Schema(Simple));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(4);
        await Assert.That(Component(set.Units[2], "T").GetChildNode(0).FindAttribute("base")).IsEqualTo("xs:string");
        await Assert.That(Component(set.Units[3], "T").GetChildNode(0).FindAttribute("base")).IsEqualTo("xs:string");
    }

    [Test]
    public async Task OverrideCycle_ThroughAnInclude_Terminates_AndLoadsEachDocumentOnce()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>" + Simple))
            .Add("file:///b.xsd", Schema("<xs:include schemaLocation=\"a.xsd\"/><xs:element name=\"E\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
        await Assert.That(resolver.Requests).Contains("file:///a.xsd");
        await Assert.That(resolver.Requests).Contains("file:///b.xsd");
        await Assert.That(ReferenceEquals(set.Units[1].Components[0], OverrideChild(set.Units[0], 0, 0))).IsTrue();
    }

    [Test]
    public async Task OverrideCycle_BackToTheOverridingDocument_Terminates_AndLoadsEachDocumentOnce()
    {
        var resolver = new MapResolver().Add(
            "file:///b.xsd",
            Schema("<xs:include schemaLocation=\"root.xsd\"/>" + Simple + "<xs:element name=\"E\"/>")
        );

        var (set, issues) = Load(Schema(Override("b.xsd", Replacement) + "<xs:element name=\"R\"/>"), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
        await Assert.That(Names(set.Units[0])).IsEqualTo("element:R");
        await Assert.That(Names(set.Units[1])).IsEqualTo("simpleType:T,element:E");
        await Assert.That(set.Units[1].Included.Contains(set.Units[0])).IsTrue();
    }

    [Test]
    public async Task OverrideCycle_OfTwoDocumentsThatOverrideEachOther_Terminates()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema(Override("b.xsd", Replacement) + "<xs:element name=\"E\"/>"))
            .Add("file:///b.xsd", Schema(Override("a.xsd", Replacement) + Simple));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsLessThanOrEqualTo(4);
        await Assert.That(resolver.Requests.Count).IsLessThanOrEqualTo(2);
    }

    [Test]
    public async Task Override_CountsAgainstTheDocumentLimit_AlsoWhenRefused()
    {
        var (set, issues) = Load(
            Schema(Override("a.xsd", Replacement) + Override("b.xsd", Replacement)),
            DenyAllResourceResolver.Instance,
            ValidationLimits.Default.WithMaxExternalResources(1)
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(issues[1].Code).IsEqualTo(CoreDiagnosticCodes.ExternalResourceLimitExceeded);
    }

    [Test]
    public async Task Override_OfAMissingDocument_IsACodedError_AtTheDirective()
    {
        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), new MapResolver());

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }
}
