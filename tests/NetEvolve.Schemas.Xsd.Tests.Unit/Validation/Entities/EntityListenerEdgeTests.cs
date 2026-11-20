namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.Entities;

public class EntityListenerEdgeTests
{
    private static readonly DocumentLocation Here = new("file:///i.xml", 3, 4);

    private static (EntityListener Listener, ValidationIssueCollector Collector) Begin(string? subset = null)
    {
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var listener = new EntityListener(new ScriptedAssessor());
        listener.BeginRun(
            new ValidationRunContext(limits, new SpanInterningNameTable(100), new XsdIssueReporter(collector))
        );
        if (subset is not null)
        {
            Receive(listener, subset);
        }

        return (listener, collector);
    }

    private static void Receive(EntityListener listener, string subset) =>
        listener.ReceiveDoctype(new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan()));

    private static void Attribute(EntityListener listener, IXsdComponent? type, string value) =>
        listener.Attribute(new AttributeInfo(1, default, Here, null, value.AsSpan(), type, true, false));

    private static void Value(EntityListener listener, IXsdComponent? type, string value, int ordinal = 1) =>
        listener.SimpleValue(new SimpleValueInfo(ordinal, value.AsSpan(), type, -1, true, false));

    [Test]
    public async Task Interest_IsTheElementStartTheAttributesAndTheSimpleValues()
    {
        var listener = new EntityListener(new ScriptedAssessor());

        await Assert
            .That(listener.Interest)
            .IsEqualTo(ListenerInterest.StartElement | ListenerInterest.Attribute | ListenerInterest.SimpleValue);
    }

    [Test]
    public async Task Attribute_AndValue_WithoutAType_AreIgnored()
    {
        var (listener, collector) = Begin();

        Attribute(listener, null, "a");
        Value(listener, null, "x");
        listener.EndRun();

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Attribute_AndValue_OfAComponentThatIsNoSimpleType_AreIgnored()
    {
        var (listener, collector) = Begin();
        var component = new NotationFake("ENTITY");

        Attribute(listener, component, "a");
        Value(listener, component, "x");

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Attribute_AndValue_BeforeBeginRun_AreIgnored()
    {
        var listener = new EntityListener(new ScriptedAssessor());
        var entity = SchemaFixture.Simple("ENTITY");

        Attribute(listener, entity, "a");
        Value(listener, entity, "x");
        listener.EndRun();

        var (_, collector) = Begin();
        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Value_OfAnElementThatDidNotStartLast_HasNoLocation()
    {
        var (listener, collector) = Begin("");
        listener.StartElement(new ElementStartInfo(5, default, Here, null, null, 1));

        Value(listener, SchemaFixture.Simple("ENTITY"), "nope", ordinal: 6);
        Value(listener, SchemaFixture.Simple("ENTITY"), "nope", ordinal: 5);

        var issues = collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Location.Line).IsEqualTo(0);
        await Assert.That(issues[1].Location.Line).IsEqualTo(3);
        await Assert.That(issues[1].Location.Column).IsEqualTo(4);
    }

    [Test]
    public async Task Doctype_ThatIsNotWellFormed_LeavesEveryValueInvalid()
    {
        var (listener, collector) = Begin("<!ENTITY pic SYSTEM");

        Value(listener, SchemaFixture.Simple("ENTITY"), "pic");

        var issues = collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("no document type declaration");
    }

    [Test]
    public async Task Doctype_ReceivedTwice_KeepsOnlyTheLastDeclarations()
    {
        var (listener, collector) = Begin("<!NOTATION g SYSTEM \"g\"><!ENTITY first SYSTEM \"f.gif\" NDATA g>");
        Receive(listener, "<!NOTATION g SYSTEM \"g\"><!ENTITY second SYSTEM \"s.gif\" NDATA g>");

        Value(listener, SchemaFixture.Simple("ENTITY"), "first");
        Value(listener, SchemaFixture.Simple("ENTITY"), "second");

        var issues = collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'first'");
    }

    [Test]
    public async Task BeginRun_ForgetsTheDeclarationsOfThePreviousRun()
    {
        var (listener, _) = Begin("<!NOTATION g SYSTEM \"g\"><!ENTITY pic SYSTEM \"p.gif\" NDATA g>");
        listener.EndRun();

        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        listener.BeginRun(
            new ValidationRunContext(limits, new SpanInterningNameTable(100), new XsdIssueReporter(collector))
        );
        Value(listener, SchemaFixture.Simple("ENTITY"), "pic");

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Limits_OfTheListener_BoundTheParseOfTheDeclarations()
    {
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var listener = new EntityListener(new ScriptedAssessor(), XmlReaderLimits.Default.WithMaxNameLength(4));
        listener.BeginRun(
            new ValidationRunContext(limits, new SpanInterningNameTable(100), new XsdIssueReporter(collector))
        );
        Receive(listener, "<!NOTATION g SYSTEM \"g\"><!ENTITY longname SYSTEM \"p.gif\" NDATA g>");

        Value(listener, SchemaFixture.Simple("ENTITY"), "longname");

        var issues = collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("no document type declaration");
    }

    private static IXsdComponent BuiltIn(string local)
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, local, StringComparison.Ordinal))
            {
                return types[i];
            }
        }

        throw new InvalidOperationException("No built-in type " + local);
    }

    [Test]
    public async Task BuiltInEntity_AndEntities_AreCheckedPerName()
    {
        var (listener, collector) = Begin("<!NOTATION g SYSTEM \"g\"><!ENTITY pic SYSTEM \"p.gif\" NDATA g>");

        Attribute(listener, BuiltIn("ENTITY"), "pic");
        Attribute(listener, BuiltIn("ENTITY"), "nope");
        Value(listener, BuiltIn("ENTITIES"), "pic other");

        var issues = collector.ToResult().Issues;
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'nope'");
        await Assert.That(issues[1].Message).Contains("'other'");
    }

    [Test]
    public async Task BuiltInTypesOfOtherNames_AreNeverChecked()
    {
        var (listener, collector) = Begin("");

        Attribute(listener, BuiltIn("NCName"), "nope");
        Value(listener, BuiltIn("IDREFS"), "nope more");
        Value(listener, BuiltIn("anySimpleType"), "nope");

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }
}
