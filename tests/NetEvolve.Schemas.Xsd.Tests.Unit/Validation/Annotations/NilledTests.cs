namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The validator does not evaluate <c>xsi:nil</c> yet (XSD-55, #308) and always reports <c>isNilled: false</c>, so these tests
/// drive the hooks of the listener with the end-of-element input that #308 will produce.
/// </summary>
public class NilledTests
{
    private static (AnnotationSource Source, AnnotationListener Listener, XmlTree Tree) Start(string xml)
    {
        var table = new AnnotationTable();
        var listener = new AnnotationListener(table);
        var limits = ValidationLimits.Default;
        listener.BeginRun(
            new ValidationRunContext(
                limits,
                new SpanInterningNameTable(100),
                new XsdIssueReporter(new ValidationIssueCollector(limits))
            )
        );
        var tree = AnnotationFixture.AnnotatedRun.Build(xml);
        return (new AnnotationSource(table, tree, new ScriptedAssessor()), listener, tree);
    }

    private static XdmItem[] TypedValue(XsdAnnotation annotation)
    {
        var items = new XdmItem[annotation.TypedValueCount];
        _ = annotation.CopyTypedValue(items);
        return items;
    }

    [Test]
    public async Task Annotation_Nilled_ReportsNilledTrue_AndAnEmptyTypedValue()
    {
        var (source, listener, tree) = Start("<r/>");
        listener.EndElement(new ElementEndInfo(1, true, true, SchemaFixture.Simple("integer")));

        var found = source.TryGetAnnotation(tree.Document.FirstChild, out var annotation);

        await Assert.That(found).IsTrue();
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.IsNilled).IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Empty);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(0);
        await Assert.That(TypedValue(annotation).Length).IsEqualTo(0);
        await Assert.That(annotation.CopyTypedValue(new XdmItem[1])).IsEqualTo(0);
    }

    [Test]
    public async Task Annotation_Nilled_OnlyWhenValid()
    {
        var (source, listener, tree) = Start("<r/>");
        listener.EndElement(new ElementEndInfo(1, false, true, SchemaFixture.Simple("integer")));

        _ = source.TryGetAnnotation(tree.Document.FirstChild, out var annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(annotation.IsNilled).IsFalse();
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
    }

    [Test]
    public async Task Annotation_Nilled_ElementWithComplexContent_HasAnEmptyTypedValueInsteadOfNone()
    {
        var (source, listener, tree) = Start("<r/>");
        var schema = new SchemaFixture();
        listener.EndElement(
            new ElementEndInfo(1, true, true, schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", null))))
        );

        _ = source.TryGetAnnotation(tree.Document.FirstChild, out var annotation);

        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Empty);
    }

    [Test]
    public async Task TypedValue_OfANilledElement_LeavesTheSlotEmpty()
    {
        var (source, listener, tree) = Start("<r/>");
        listener.EndElement(new ElementEndInfo(1, true, true, SchemaFixture.Simple("integer")));

        var filled = source.TryGetTypedValue(new XPathNode(tree.Document.FirstChild), out _);

        await Assert.That(filled).IsFalse();
    }

    [Test]
    public async Task Annotation_NotNilled_ReportsNilledFalse()
    {
        var (source, listener, tree) = Start("<r/>");
        listener.EndElement(new ElementEndInfo(1, true, false, SchemaFixture.Simple("integer")));

        _ = source.TryGetAnnotation(tree.Document.FirstChild, out var annotation);

        await Assert.That(annotation.IsNilled).IsFalse();
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
    }
}
