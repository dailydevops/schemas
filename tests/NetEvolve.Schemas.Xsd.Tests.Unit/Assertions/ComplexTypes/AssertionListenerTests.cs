namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class AssertionListenerTests
{
    [Test]
    public async Task Constructor_NullAssertions_Throws() =>
        await Assert.That(() => new AssertionListener(null!, new ScriptedAssessor())).Throws<ArgumentNullException>();

    [Test]
    public async Task Constructor_NullAssessor_Throws() =>
        await Assert
            .That(() =>
                new AssertionListener(
                    new ComplexTypeAssertions(
                        new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>()
                    ),
                    null!
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Interest_WithoutAssertions_IsNone()
    {
        var listener = new AssertionListener(
            new ComplexTypeAssertions(new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>()),
            new ScriptedAssessor()
        );

        await Assert.That(listener.Interest).IsEqualTo(ListenerInterest.None);
    }

    [Test]
    public async Task Interest_WithAssertions_IsEveryHook()
    {
        var instance = AssertionFixture.Instance(["true()"], []);
        var listener = new AssertionListener(instance.Assertions, new ScriptedAssessor());

        await Assert.That(listener.Interest).IsEqualTo(ListenerInterest.All);
    }

    [Test]
    public async Task Listener_ReusedForASecondRun_StartsClean()
    {
        var instance = AssertionFixture.Instance(["@k = 'x'"], []);
        var assessor = new ScriptedAssessor();
        var listener = new AssertionListener(instance.Assertions, assessor);
        var model = instance.Schema.Model();

        var first = Run(model, assessor, "\n<r k=\"y\"/>", listener);
        var second = Run(model, assessor, "\n<r k=\"x\"/>", listener);
        var third = Run(model, assessor, "\n<r k=\"y\"/>", listener);

        await Assert.That(first.Count).IsEqualTo(1);
        await Assert.That(second).IsEmpty();
        await Assert.That(third.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Listener_RunWithoutAnAssertionBearingElement_AllocatesNothingPerElement()
    {
        // Only the type of c has an assertion, and the document holds no c, so no subtree is ever kept.
        var instance = AssertionFixture.Instance([], ["true()"]);
        var assessor = new ScriptedAssessor();
        var model = instance.Schema.Model();
        var body = new StringBuilder("\n<r>");
        for (var i = 0; i < 2000; i++)
        {
            _ = body.Append("<s>text</s>");
        }

        var xml = body.Append("</r>").ToString();

        var plain = AllocationAssert.Measure(() => _ = Run(model, assessor, xml));
        var listening = AllocationAssert.Measure(() =>
            _ = Run(model, assessor, xml, new AssertionListener(instance.Assertions, assessor))
        );

        await Assert.That(Math.Abs(listening - plain)).IsLessThan(4096);
    }

    [Test]
    public async Task Listener_SubtreeOfTheRoot_AllocatesWithTheSubtreeOnly()
    {
        // The root has an assertion, so its whole subtree is kept: the cost grows with the subtree, as the limitation states.
        var instance = AssertionFixture.Instance(["true()"], []);
        var assessor = new ScriptedAssessor();
        var model = instance.Schema.Model();
        var body = new StringBuilder("\n<r>");
        for (var i = 0; i < 2000; i++)
        {
            _ = body.Append("<s>text</s>");
        }

        var xml = body.Append("</r>").ToString();

        var plain = AllocationAssert.Measure(() => _ = Run(model, assessor, xml));
        var listening = AllocationAssert.Measure(() =>
            _ = Run(model, assessor, xml, new AssertionListener(instance.Assertions, assessor))
        );

        await Assert.That(listening - plain).IsGreaterThan(2000L * 16);
    }

    private static IReadOnlyList<ValidationIssue> Run(
        ValidationModel model,
        ScriptedAssessor assessor,
        string xml,
        params ValidationListener[] listeners
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var validator = new StreamingValidator(
            model,
            assessor,
            new XsdIssueReporter(collector),
            ValidationLimits.Default,
            ValidationHarness.Source,
            listeners
        );
        using var reader = XmlPipelineReader.Create(xml.AsSpan());
        _ = reader.Pump(ref validator);
        return collector.ToResult().Issues;
    }
}
