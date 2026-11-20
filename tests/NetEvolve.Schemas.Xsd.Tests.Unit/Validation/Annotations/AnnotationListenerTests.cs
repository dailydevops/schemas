namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AnnotationListenerTests
{
    private static readonly DocumentLocation Where = new("file:///a.xml", 1, 1);

    private static (AnnotationListener Listener, RecordingSink Sink, XmlName Name) Begin()
    {
        var sink = new RecordingSink();
        var listener = new AnnotationListener(sink);
        var limits = ValidationLimits.Default;
        var names = new SpanInterningNameTable(100);
        listener.BeginRun(
            new ValidationRunContext(limits, names, new XsdIssueReporter(new ValidationIssueCollector(limits)))
        );
        return (listener, sink, names.Intern("", "a", ""));
    }

    private static AttributeInfo Attribute(int owner, XmlName name, IXsdComponent? type, bool valid) =>
        new(owner, name, Where, null, "1".AsSpan(), type, valid, false);

    [Test]
    public async Task Interest_WithoutASink_IsNone() =>
        await Assert.That(new AnnotationListener(null).Interest).IsEqualTo(ListenerInterest.None);

    [Test]
    public async Task Interest_WithASink_IsAttributesAndElementEnds() =>
        await Assert
            .That(new AnnotationListener(new RecordingSink()).Interest)
            .IsEqualTo(ListenerInterest.Attribute | ListenerInterest.EndElement);

    [Test]
    public async Task Attribute_IndexCountsPerOwner_AndRestartsForTheNextOwner()
    {
        var (listener, sink, name) = Begin();
        var type = SchemaFixture.Simple("integer");

        listener.Attribute(Attribute(1, name, type, true));
        listener.Attribute(Attribute(1, name, type, true));
        listener.Attribute(Attribute(2, name, type, true));

        await Assert.That(sink.Attributes.Count).IsEqualTo(3);
        await Assert.That(sink.Attributes[0].AttributeIndex).IsEqualTo(0);
        await Assert.That(sink.Attributes[1].AttributeIndex).IsEqualTo(1);
        await Assert.That(sink.Attributes[2].AttributeIndex).IsEqualTo(0);
        await Assert.That(sink.Attributes[2].Ordinal).IsEqualTo(2);
    }

    [Test]
    public async Task Attribute_WithoutAType_TakesAnIndexButHasNoRecord()
    {
        var (listener, sink, name) = Begin();

        listener.Attribute(Attribute(1, name, null, true));
        listener.Attribute(Attribute(1, name, SchemaFixture.Simple("integer"), false));

        await Assert.That(sink.Attributes.Count).IsEqualTo(1);
        await Assert.That(sink.Attributes[0].AttributeIndex).IsEqualTo(1);
        await Assert.That(sink.Attributes[0].Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(sink.Attributes[0].IsNilled).IsFalse();
    }

    [Test]
    public async Task Attribute_ValidWithAType_IsValid()
    {
        var (listener, sink, name) = Begin();

        listener.Attribute(Attribute(7, name, SchemaFixture.Simple("integer"), true));

        await Assert.That(sink.Attributes[0].Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(sink.Attributes[0].Type).IsNotNull();
    }

    [Test]
    public async Task BeginRun_RestartsTheAttributeIndex_ForTheNextRun()
    {
        var (listener, sink, name) = Begin();
        listener.Attribute(Attribute(1, name, SchemaFixture.Simple("integer"), true));
        var limits = ValidationLimits.Default;
        listener.BeginRun(
            new ValidationRunContext(
                limits,
                new SpanInterningNameTable(100),
                new XsdIssueReporter(new ValidationIssueCollector(limits))
            )
        );

        listener.Attribute(Attribute(1, name, SchemaFixture.Simple("integer"), true));

        await Assert.That(sink.Attributes[1].AttributeIndex).IsEqualTo(0);
    }

    [Test]
    public async Task EndElement_WithoutAType_HasNoRecord()
    {
        var (listener, sink, _) = Begin();

        listener.EndElement(new ElementEndInfo(1, true, false, null));

        await Assert.That(sink.Elements.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EndElement_Valid_IsValid_AndKeepsTheNilledState()
    {
        var (listener, sink, _) = Begin();

        listener.EndElement(new ElementEndInfo(2, true, true, SchemaFixture.Simple("integer")));
        listener.EndElement(new ElementEndInfo(1, true, false, SchemaFixture.Simple("integer")));

        await Assert.That(sink.Elements[0].Ordinal).IsEqualTo(2);
        await Assert.That(sink.Elements[0].Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(sink.Elements[0].IsNilled).IsTrue();
        await Assert.That(sink.Elements[1].IsNilled).IsFalse();
        await Assert.That(sink.Elements[0].AttributeIndex).IsEqualTo(-1);
    }

    [Test]
    public async Task EndElement_Invalid_IsNeverNilled()
    {
        var (listener, sink, _) = Begin();

        listener.EndElement(new ElementEndInfo(1, false, true, SchemaFixture.Simple("integer")));

        await Assert.That(sink.Elements[0].Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(sink.Elements[0].IsNilled).IsFalse();
    }

    private sealed class RecordingSink : IAnnotationSink
    {
        public List<AnnotationRecord> Elements { get; } = [];

        public List<AnnotationRecord> Attributes { get; } = [];

        public void Element(in AnnotationRecord record) => Elements.Add(record);

        public void Attribute(in AnnotationRecord record) => Attributes.Add(record);
    }
}
