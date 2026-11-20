namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Ids;

public class IdListenerEdgeTests
{
    private static readonly DocumentLocation Here = new("file:///i.xml", 3, 4);

    private static (IdListener Listener, ValidationIssueCollector Collector) Begin()
    {
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var listener = IdFixture.Listener(new ScriptedAssessor());
        listener.BeginRun(
            new ValidationRunContext(limits, new SpanInterningNameTable(100), new XsdIssueReporter(collector))
        );
        return (listener, collector);
    }

    private static void Attribute(IdListener listener, IXsdComponent? type, string value) =>
        listener.Attribute(new AttributeInfo(1, default, Here, null, value.AsSpan(), type, true, false));

    private static void Value(IdListener listener, IXsdComponent? type, string value) =>
        listener.SimpleValue(new SimpleValueInfo(1, value.AsSpan(), type, -1, true, false));

    [Test]
    public async Task Idref_Dangling_MoreTokensThanTheInitialBuffer_AreAllReportedInDocumentOrder()
    {
        // 40 elements with one reference and 10 with two: 60 waiting references, past the initial capacity of 16.
        var builder = new StringBuilder("<r>\n");
        var expected = new List<(int Line, string Token)>();
        var line = 2;
        for (var i = 0; i < 40; i++, line++)
        {
            _ = builder.Append("<e ref=\"tok").Append(i).Append("\"/>\n");
            expected.Add((line, "tok" + i));
        }

        for (var i = 0; i < 10; i++, line++)
        {
            _ = builder.Append("<e refs=\"many").Append(i).Append(" more").Append(i).Append("\"/>\n");
            expected.Add((line, "many" + i));
            expected.Add((line, "more" + i));
        }

        var issues = IdFixture.Run(IdFixture.Schema(), builder.Append("</r>").ToString());

        await Assert.That(issues.Count).IsEqualTo(expected.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            await Assert.That(issues[i].Constraint).IsEqualTo("cvc-id.1");
            await Assert.That(issues[i].Location.Line).IsEqualTo(expected[i].Line);
            await Assert.That(issues[i].Message).Contains("'" + expected[i].Token + "'");
        }
    }

    [Test]
    public async Task Idref_Dangling_LongerThan64Characters_IsCutAt64CharactersAndMarked()
    {
        var token = new string('x', 100);

        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e ref=\"" + token + "\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'" + new string('x', 64) + "...'");
        await Assert.That(issues[0].Message).DoesNotContain(new string('x', 65));
    }

    [Test]
    public async Task Id_Duplicate_LongerThan64Characters_IsCutAt64CharactersAndMarked()
    {
        var token = new string('y', 100);

        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e id=\"" + token + "\"/><e id=\"" + token + "\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'" + new string('y', 64) + "...'");
        await Assert.That(issues[0].Message).DoesNotContain(new string('y', 65));
    }

    [Test]
    public async Task Idref_Dangling_OfExactly64Characters_IsNotCut()
    {
        var token = new string('z', 64);

        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e ref=\"" + token + "\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("'" + token + "'");
    }

    [Test]
    public async Task Idref_OnAUnionWhoseMembersAllRejectTheValue_ReportsNothing()
    {
        var schema = new SchemaFixture();
        var union = IdFixture.UnionOf(schema, SchemaFixture.Simple("IDREF"), SchemaFixture.Simple("IDREF"));
        var fixture = IdFixture.Schema(idref: union);
        const string Xml = "<r><e ref=\"missing\"/></r>";

        // With a member that accepts the value the dangling reference is reported; with none, no member decides.
        var accepted = IdFixture.Run(fixture, Xml);
        var rejected = ValidationHarness.Run(
            fixture,
            Xml,
            new MembersRejectingAssessor(),
            IdFixture.Listener(new MembersRejectingAssessor())
        );

        await Assert.That(accepted.Count).IsEqualTo(1);
        await Assert.That(rejected.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Attribute_AndValue_WithoutAType_AreIgnored()
    {
        var (listener, collector) = Begin();

        Attribute(listener, null, "a");
        Attribute(listener, null, "a");
        Value(listener, null, "x");
        listener.EndRun();

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Attribute_AndValue_OfAComponentThatIsNoSimpleType_AreIgnored()
    {
        var (listener, collector) = Begin();
        var component = new NotationFake("ID");

        Attribute(listener, component, "a");
        Attribute(listener, component, "a");
        Value(listener, component, "x");
        listener.EndRun();

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Attribute_AndValue_BeforeBeginRun_AreIgnored_AndLeaveNothingForTheNextRun()
    {
        var listener = IdFixture.Listener(new ScriptedAssessor());
        var id = SchemaFixture.Simple("ID");
        var idref = SchemaFixture.Simple("IDREF");

        Attribute(listener, id, "a");
        Attribute(listener, id, "a");
        Value(listener, idref, "nowhere");
        listener.EndRun();

        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        listener.BeginRun(
            new ValidationRunContext(limits, new SpanInterningNameTable(100), new XsdIssueReporter(collector))
        );
        Attribute(listener, id, "a");
        listener.EndRun();

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    private sealed class MembersRejectingAssessor : ISimpleValueAssessor
    {
        // The anonymous union is valid; every named member type rejects the value.
        public ValidationResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex)
        {
            memberIndex = -1;
            return simpleType.Name.LocalName.Length == 0
                ? ValidationResult.Valid
                : ValidationResult.Invalid(DatatypeError.InvalidLexicalForm, FacetKind.None, 0);
        }
    }
}
