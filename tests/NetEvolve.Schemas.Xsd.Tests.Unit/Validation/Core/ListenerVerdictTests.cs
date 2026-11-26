namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class ListenerVerdictTests
{
    private static SchemaFixture Schema()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(SchemaFixture.Part("c", SchemaFixture.Simple("int"), 0, long.MaxValue))
            )
        );
        return schema;
    }

    [Test]
    public async Task EndElementViolation_MarksTheElementAndItsAncestorsInvalid_BeforeLaterListeners()
    {
        var recorder = new RecordingListener();

        var issues = ValidationHarness.Run(Schema(), "<r><c>1</c><c>2</c></r>", null, new Reporting(2), recorder);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(recorder.Events).Contains("end #1 (invalid)");
        await Assert.That(recorder.Events).Contains("end #2");
        await Assert.That(recorder.Events).Contains("end #3 (invalid)");
    }

    [Test]
    public async Task StartElementViolation_MarksTheElementInvalid()
    {
        var recorder = new RecordingListener();

        _ = ValidationHarness.Run(Schema(), "<r><c>1</c></r>", null, new Reporting(2, atStart: true), recorder);

        await Assert.That(recorder.Events).Contains("end #2 (invalid)");
        await Assert.That(recorder.Events).Contains("end #1 (invalid)");
    }

    [Test]
    public async Task WarningFromAListener_DoesNotMarkTheElementInvalid()
    {
        var recorder = new RecordingListener();

        _ = ValidationHarness.Run(Schema(), "<r><c>1</c></r>", null, new Reporting(2, warning: true), recorder);

        await Assert.That(recorder.Events).Contains("end #2");
        await Assert.That(recorder.Events).Contains("end #1");
    }

    private sealed class Reporting(int ordinal, bool atStart = false, bool warning = false) : ValidationListener
    {
        private XsdIssueReporter? _reporter;

        public override ListenerInterest Interest => ListenerInterest.StartElement | ListenerInterest.EndElement;

        public override void BeginRun(in ValidationRunContext context) => _reporter = context.Reporter;

        public override void StartElement(in ElementStartInfo info)
        {
            if (atStart && info.Ordinal == ordinal)
            {
                Report(info.Location);
            }
        }

        public override void EndElement(in ElementEndInfo info)
        {
            if (!atStart && info.Ordinal == ordinal)
            {
                Report(default);
            }
        }

        private void Report(DocumentLocation location) =>
            _reporter!.Report(
                warning
                    ? new XsdRule(
                        ValidationRules.ComplexType.Code,
                        "w",
                        "w",
                        XsdRuleKind.Engine,
                        XsdVersionMask.All,
                        string.Empty,
                        IssueSeverity.Warning
                    )
                    : ValidationRules.ComplexType,
                location,
                "listener violation"
            );
    }
}
