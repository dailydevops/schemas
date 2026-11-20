namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations;

using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Annotations.AnnotationModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>
/// The mapping of the <c>severity</c>, <c>role</c>, and <c>flag</c> values to the core <see cref="IssueSeverity"/>. Q4 (#106) decided that
/// <c>severity</c> is an opaque token without a vocabulary, so the table has two rows and the values never take part: a failed assert is
/// an <see cref="IssueSeverity.Error"/> and a successful report a <see cref="IssueSeverity.Warning"/> (design #137 AP-12). The token
/// travels on the finding unchanged. Severity filtering is #384.
/// </summary>
public class SeverityMappingTests
{
    [Test]
    [Arguments("fatal")]
    [Arguments("error")]
    [Arguments("warning")]
    [Arguments("info")]
    [Arguments("FATAL")]
    [Arguments("no-such-token")]
    [Arguments("42")]
    [Arguments("")]
    public async Task Severity_Token_NeverChangesIssueSeverity(string token)
    {
        var schema = Schema(
            Pattern(
                "p",
                Rule(
                    "r",
                    "a",
                    Failing(severity: token, message: "assert"),
                    Reporting(severity: token, message: "report")
                )
            )
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details.Count).IsEqualTo(2);
        await Assert.That(h.Log.Details[0].Issue!.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo(token);
        await Assert.That(h.Log.Details[1].Issue!.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(h.Log.Details[1].Severity).IsEqualTo(token);
    }

    [Test]
    [Arguments("fatal")]
    [Arguments("warning")]
    [Arguments("info")]
    [Arguments("no-such-token")]
    public async Task Severity_DynamicToken_NeverChangesIssueSeverity(string value)
    {
        var schema = Schema(
            Pattern(
                "p",
                RuleWith(
                    "r",
                    "a",
                    null,
                    null,
                    [Let("s")],
                    Failing(severity: "$s", message: "assert"),
                    Reporting(severity: "$s", message: "report")
                )
            )
        );
        using var h = new PatternHarness(schema, E("a"));
        h.Binding.Texts["$s"] = value;

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Issue!.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(h.Log.Details[0].Severity).IsEqualTo(value);
        await Assert.That(h.Log.Details[1].Issue!.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(h.Log.Details[1].Severity).IsEqualTo(value);
    }

    [Test]
    public async Task RoleAndFlags_NeverChangeIssueSeverity_AWarningRoleAssertStillInvalidates()
    {
        var schema = Schema(
            Pattern(
                "p",
                RuleWith(
                    "r",
                    "a",
                    "warning",
                    ["warning"],
                    null,
                    Failing(role: "info", flags: ["info"], severity: "info", message: "assert"),
                    Reporting(role: "error", flags: ["fatal"], severity: "fatal", message: "report")
                )
            )
        );
        using var h = new PatternHarness(schema, E("a"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Details[0].Issue!.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(h.Log.Details[1].Issue!.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(h.Collector.Count).IsEqualTo(2);
    }
}
