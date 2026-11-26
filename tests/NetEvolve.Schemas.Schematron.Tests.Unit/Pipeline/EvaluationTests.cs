namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The composed validator on the real engines (SCH-55, #311): phases, variables, abstract patterns, the dynamic errors of expressions,
/// the XSLT functions of the XSLT bindings, the events of a caller sink, the issue limit, and the SVRL writer.
/// </summary>
public class EvaluationTests
{
    private const string Xs = "<ns prefix=\"xs\" uri=\"http://www.w3.org/2001/XMLSchema\"/>";

    private static string Rule(string assertions, string context = "a") =>
        "<pattern id=\"p\"><rule context=\"" + context + "\">" + assertions + "</rule></pattern>";

    private static string Messages(ValidationResult result) =>
        string.Join("|", result.Issues.Select(issue => issue.Message));

    [Test]
    public async Task Phase_SelectsTheActivePatterns()
    {
        var schema = Wrap(
            """
            <phase id="basic"><active pattern="p1"/></phase>
            <phase id="full"><active pattern="p1"/><active pattern="p2"/></phase>
            <pattern id="p1"><rule context="a"><assert test="false()">One.</assert></rule></pattern>
            <pattern id="p2"><rule context="a"><assert test="false()">Two.</assert></rule></pattern>
            """,
            " defaultPhase=\"basic\""
        );

        var byDefault = Build(schema);
        var basic = Builder(schema).WithPhase("basic").BuildNow();
        var full = Builder(schema).WithPhase("full").BuildNow();
        var all = Builder(schema).WithPhase("#ALL").BuildNow();

        await Assert.That(byDefault.EffectivePhase).IsEqualTo("basic");
        await Assert.That(Messages(Validate(byDefault, "<a/>"))).IsEqualTo("One.");
        await Assert.That(basic.EffectivePhase).IsEqualTo("basic");
        await Assert.That(full.EffectivePhase).IsEqualTo("full");
        await Assert.That(Messages(Validate(full, "<a/>"))).IsEqualTo("One.|Two.");
        await Assert.That(all.EffectivePhase).IsEqualTo("#ALL");
        await Assert.That(Messages(Validate(all, "<a/>"))).IsEqualTo("One.|Two.");
    }

    [Test]
    public async Task Variables_OfEveryLevel_AreVisibleWhereTheyAreDeclared()
    {
        var schema = Wrap(
            """
            <let name="schemaLevel" value="1"/>
            <phase id="ph"><let name="phaseLevel" value="2"/><active pattern="p"/></phase>
            <pattern id="p">
              <let name="patternLevel" value="3"/>
              <rule context="a">
                <let name="ruleLevel" value="count(b)"/>
                <assert test="$schemaLevel + $phaseLevel + $patternLevel + $ruleLevel = 8">Sum is <value-of select="string($schemaLevel + $phaseLevel + $patternLevel + $ruleLevel)"/>.</assert>
              </rule>
            </pattern>
            """
        );
        var validator = Builder(schema).WithPhase("ph").BuildNow();

        await Assert.That(Validate(validator, "<a><b/><b/></a>").IsValid).IsTrue();
        await Assert.That(Messages(Validate(validator, "<a><b/></a>"))).IsEqualTo("Sum is 7.");
    }

    [Test]
    public async Task RuleVariable_ShadowsAPatternVariableOfTheSameName()
    {
        var schema = Wrap(
            """
            <pattern id="p">
              <let name="v" value="'pattern'"/>
              <rule context="a"><let name="v" value="'rule'"/><assert test="$v = 'pattern'">Reads <value-of select="$v"/>.</assert></rule>
              <rule context="b"><assert test="$v = 'pattern'">Wrong.</assert></rule>
            </pattern>
            """
        );
        var validator = Build(schema);

        await Assert.That(Messages(Validate(validator, "<a/>"))).IsEqualTo("Reads rule.");
        await Assert.That(Validate(validator, "<b/>").IsValid).IsTrue();
    }

    [Test]
    public async Task AbstractPattern_IsExpandedWithItsParameters()
    {
        var schema = Wrap(
            """
            <pattern abstract="true" id="positive">
              <rule context="$item"><assert test="$value &gt; 0">The <name/> must be positive.</assert></rule>
            </pattern>
            <pattern is-a="positive" id="amount"><param name="item" value="amount"/><param name="value" value="."/></pattern>
            <pattern is-a="positive" id="count"><param name="item" value="count"/><param name="value" value="number(.)"/></pattern>
            """
        );
        var validator = Build(schema);

        var result = Validate(validator, "<r><amount>-1</amount><count>5</count></r>");

        await Assert.That(Messages(result)).IsEqualTo("The amount must be positive.");
    }

    [Test]
    public async Task Name_WithPath_InsertsTheValueOfTheExpression()
    {
        var schema = Wrap(
            """
            <pattern id="plain"><rule context="a"><report test="true()"><name path="@attribute"/></report></rule></pattern>
            <pattern abstract="true" id="abstract"><rule context="*"><assert test="false()"><name path="$expression"/></assert></rule></pattern>
            <pattern is-a="abstract" id="named"><param name="expression" value="name()"/></pattern>
            """
        );
        var validator = Build(schema);

        var result = Validate(validator, "<a attribute=\"value\"/>");

        await Assert.That(Messages(result)).IsEqualTo("value|a");
    }

    [Test]
    public async Task AbstractRule_IsExtended()
    {
        var schema = Wrap(
            """
            <pattern id="p">
              <rule abstract="true" id="shared"><assert test="@id">An id is required.</assert></rule>
              <rule context="a"><extends rule="shared"/></rule>
            </pattern>
            """
        );

        await Assert.That(Messages(Validate(Build(schema), "<a/>"))).IsEqualTo("An id is required.");
    }

    [Test]
    public async Task DynamicError_IsAnIssueAndTheOtherAssertionsRun()
    {
        var schema = Wrap(
            Xs + Rule("<assert test=\"xs:integer(.) &gt; 0\">Bad.</assert><assert test=\"false()\">Second.</assert>")
        );
        var validator = Build(schema);

        var result = Validate(validator, "<a>abc</a>");

        await Assert.That(result.Issues.Count).IsEqualTo(2);
        await Assert.That(result.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(result.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[0].Constraint).IsEqualTo("FORG0001");
        await Assert.That(result.Issues[1].Message).IsEqualTo("Second.");
    }

    [Test]
    public async Task UndeclaredXsPrefix_InRuleContextAndAssert_ResolvesToTheXmlSchemaNamespace()
    {
        var schema = Wrap(
            Rule("<assert test=\"xs:decimal(@n) gt 1\">Small.</assert>", "a[xs:decimal(@n) gt 0]"),
            " queryBinding=\"xslt2\""
        );

        var result = Validate(Build(schema), "<a n=\"1\"/>");

        await Assert.That(Messages(result)).IsEqualTo("Small.");
    }

    [Test]
    public async Task SchemaDeclaredXsPrefix_ForAnotherNamespace_KeepsItsOwnBinding()
    {
        var schema = Wrap(
            "<ns prefix=\"xs\" uri=\"urn:other\"/>" + Rule("<assert test=\"xs:decimal(@n) gt 1\">Small.</assert>"),
            " queryBinding=\"xslt2\""
        );

        var failure = BuildFailure(schema);

        await Assert.That(failure).IsNotNull();
    }

    [Test]
    public async Task SchemaWithoutXsPrefix_IsUnchanged()
    {
        var schema = Wrap(Rule("<assert test=\"number(@n) gt 1\">Small.</assert>"), " queryBinding=\"xslt2\"");

        var result = Validate(Build(schema), "<a n=\"1\"/>");

        await Assert.That(Messages(result)).IsEqualTo("Small.");
    }

    [Test]
    public async Task DefaultBinding_RunsInXPath1CompatibilityMode()
    {
        const string Test = "<report test=\"'abc' = 1\">Equal.</report>";

        var compatible = Validate(Build(Wrap(Rule(Test))), "<a/>");
        var strict = Validate(Build(Wrap(Rule(Test), " queryBinding=\"xslt2\"")), "<a/>");

        await Assert.That(compatible.Issues.Count).IsEqualTo(0);
        await Assert.That(strict.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(strict.Issues.Single().Constraint).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task XsltBinding_HasTheFunctionsOfXslt()
    {
        const string Xsl = "<ns prefix=\"xsl\" uri=\"http://www.w3.org/1999/XSL/Transform\"/>";
        var schema = Wrap(
            Xsl
                + Rule(
                    """
                    <assert test="current()/@id = 'x'">Current is <value-of select="current()/name()"/>.</assert>
                    <assert test="false()"><value-of select="format-number(1234.5, '#,##0.00')"/> <value-of select="system-property('xsl:version')"/></assert>
                    """
                ),
            " queryBinding=\"xslt2\""
        );

        var result = Validate(Build(schema), "<a id=\"y\"/>");

        await Assert.That(Messages(result)).IsEqualTo("Current is a.|1,234.50 2.0");
    }

    [Test]
    public async Task XpathBinding_HasNoFunctionsOfXslt()
    {
        var failure = BuildFailure(Wrap(Rule("<assert test=\"current()\">x</assert>"), " queryBinding=\"xpath2\""));

        await Assert.That(failure.Issues.Single().Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(failure.Issues.Single().Constraint).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task NamedDecimalFormat_IsUnknownAndNotUnset()
    {
        var schema = Wrap(Rule("<assert test=\"false()\"><value-of select=\"format-number(1, '0', 'nf')\"/></assert>"));

        var result = Validate(Build(schema), "<a/>");
        var failed = result.Issues.Single(issue => issue.Code == SchematronDiagnosticCodes.EvaluationFailed);

        // The slot of the decimal formats is set at the start of the run, so the name is unknown (XTDE1280), not the unset slot.
        await Assert.That(failed.Constraint).IsEqualTo("XTDE1280");
    }

    [Test]
    public async Task IssueLimit_StopsTheRunAtTheFirstDroppedError()
    {
        var schema = Wrap(Rule("<assert test=\"false()\">One.</assert><assert test=\"false()\">Two.</assert>"));
        var validator = Builder(schema).WithLimits(ValidationLimits.Default.WithMaxIssues(1)).BuildNow();

        var result = Validate(validator, "<a/>");

        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.HasDroppedErrors).IsTrue();
    }

    [Test]
    public async Task Sink_ReceivesTheEventsInOrderAndTheIssueBeforeTheFinding()
    {
        var validator = Build(Schema);
        var sink = new RecordingSink();

        var result = Validate(validator.WithReportSink(sink), Instance);

        await Assert
            .That(string.Join(" ", sink.Events))
            .IsEqualTo("begin pattern:p1 rule:r1 failed:a1 issue:True successful:rp1 issue:True finish:False");
        await Assert.That(result.Issues.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Sink_Begin_CarriesTheTitleVersionPhaseEditionNamespacesAndInstance()
    {
        var schema = Wrap(
            "<title>Invoice rules</title><ns prefix=\"i\" uri=\"urn:invoice\"/>"
                + Rule("<assert test=\"true()\">x</assert>"),
            " schemaVersion=\"1.2\" schematronEdition=\"2025\""
        );
        var sink = new RecordingSink();

        _ = Validate(Build(schema).WithReportSink(sink), "<a/>");

        await Assert.That(sink.Info).IsEqualTo($"Invoice rules|#ALL|1.2|Iso2025|1|{InstanceSystemId}");
    }

    [Test]
    public async Task Sink_Begin_OfAnInstanceWithoutASystemId_HasNoInstanceName()
    {
        var sink = new RecordingSink();

        var result = ValidateAnonymous(
            Build(Wrap(Rule("<assert test=\"false()\">x</assert>"))).WithReportSink(sink),
            "<a/>"
        );

        await Assert.That(sink.Info).IsEqualTo("|#ALL||Iso2016|0|");
        await Assert.That(result.Issues.Single().Location.Source).IsNull();
    }

    [Test]
    public async Task Sink_WithoutAnActivePatternAndWithoutASystemId_ReceivesTheNoActivePatternError()
    {
        var sink = new RecordingSink();

        var result = ValidateAnonymous(Build(Wrap(string.Empty)).WithReportSink(sink), "<a/>");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(sink.Events).Contains("error:NES3501");
    }

    [Test]
    public async Task MalformedInstanceWithoutASystemId_IsReportedWithoutASource()
    {
        var sink = new RecordingSink();

        var result = ValidateAnonymous(Build(Schema).WithReportSink(sink), "<a");

        await Assert.That(result.Issues.Single().Location.Source).IsNull();
        await Assert.That(sink.Events[1]).StartsWith("error:NES");
    }

    [Test]
    public async Task Sink_OfAMalformedInstance_ReceivesTheErrorAndNoPattern()
    {
        var sink = new RecordingSink();

        var result = Validate(Build(Schema).WithReportSink(sink), "<invoice>");

        await Assert.That(sink.Events[0]).IsEqualTo("begin");
        await Assert.That(sink.Events[1]).StartsWith("error:NES");
        await Assert.That(sink.Events.Last()).IsEqualTo("finish:False");
        await Assert.That(sink.Events).DoesNotContain("pattern:p1");
        await Assert.That(result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Sink_WithoutAnActivePattern_ReceivesTheNoActivePatternError()
    {
        var sink = new RecordingSink();

        var result = Validate(Build(Wrap(string.Empty)).WithReportSink(sink), "<a/>");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(string.Join(" ", sink.Events)).IsEqualTo("begin error:NES3501 finish:False");
    }

    [Test]
    public async Task Sink_CancelledByTheCaller_StopsTheRun()
    {
        using var source = new CancellationTokenSource();
        var sink = new RecordingSink { OnBegin = source.Cancel };
        var validator = Build(Schema).WithReportSink(sink);

        await Assert
            .That(() => validator.Validate(Instance.AsSpan(), InstanceSystemId, source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task SvrlSink_WritesTheReportOfTheRun()
    {
        var output = new ArrayBufferWriter<byte>();
        using var sink = new SvrlReportSink(output);
        var validator = Build(Schema).WithReportSink(sink);

        _ = Validate(validator, Instance);

        var svrl = Encoding.UTF8.GetString(output.WrittenSpan);
        await Assert.That(svrl).Contains("failed-assert");
        await Assert.That(svrl).Contains("successful-report");
        await Assert.That(svrl).Contains("An invoice needs a number, Müller.");
    }

    [Test]
    public async Task OneValidator_ServesConcurrentRunsWithIdenticalResults()
    {
        var validator = Build(Schema);
        var expected = Describe(Validate(validator, Instance));
        var results = new string[16];

        Parallel.For(0, results.Length, index => results[index] = Describe(Validate(validator, Instance)));

        foreach (var result in results)
        {
            await Assert.That(result).IsEqualTo(expected);
        }
    }

    private sealed class RecordingSink : ISchematronReportSink
    {
        internal List<string> Events { get; } = [];

        internal Action? OnBegin { get; init; }

        internal string Info { get; private set; } = string.Empty;

        public void Begin(in SchematronRunInfo info)
        {
            Events.Add("begin");
            Info =
                $"{info.Title.ToString()}|{info.Phase.ToString()}|{info.SchemaVersion.ToString()}|{info.Edition}|{info.Namespaces.Count}|{info.InstanceSystemId.ToString()}";
            OnBegin?.Invoke();
        }

        public void ActivePattern(in SchematronPatternEvent pattern) => Events.Add("pattern:" + pattern.Id.ToString());

        public void FiredRule(in SchematronRuleEvent rule) => Events.Add("rule:" + rule.Id.ToString());

        public void Finding(in SchematronFinding finding)
        {
            Events.Add((finding.IsReport ? "successful:" : "failed:") + finding.Id.ToString());
            Events.Add("issue:" + (finding.Issue is not null));
        }

        public void ErrorOccurred(in SchematronErrorEvent errorEvent) =>
            Events.Add("error:" + errorEvent.Code.ToString());

        public void Finish(in SchematronRunSummary summary) => Events.Add("finish:" + summary.IsTruncated);
    }
}
