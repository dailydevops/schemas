namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Atomization;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class AtomizationTests
{
    private static readonly FakeNodeSource Fake = new();

    private static XdmItem Text => XdmItem.FromNode(new XPathNode(Fake.Text));

    private static XdmItem Attribute => XdmItem.FromNode(new XPathNode(Fake.Attribute));

    private static XdmSequence Seq(params XdmItem[] items) => XdmSequence.Create(items);

    [Test]
    public async Task Atomize_Empty_IsEmpty() =>
        await Assert.That(XPathAtomization.Atomize(XdmSequence.Empty).IsEmpty).IsTrue();

    [Test]
    public async Task Atomize_MixedSequence_YieldsAtomicValuesAndNodeTypedValues()
    {
        var result = XPathAtomization.Atomize(Seq(XdmItem.FromInteger(7), Attribute, XdmItem.FromString("s"), Text));

        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(result[0]).IsEqualTo(XdmItem.FromInteger(7));
        await Assert.That(result[1]).IsEqualTo(XdmItem.FromUntypedAtomic("1"));
        await Assert.That(result[2]).IsEqualTo(XdmItem.FromString("s"));
        await Assert.That(result[3].Kind).IsEqualTo(XdmItemKind.UntypedAtomic);
        await Assert.That(result[3].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Atomize_AtomicOnlySequence_ReturnsItUnchanged()
    {
        var input = Seq(XdmItem.FromBoolean(true), XdmItem.FromDouble(1.5));

        await Assert.That(XPathAtomization.Atomize(input)).IsEqualTo(input);
    }

    [Test]
    public async Task AnnotatedNodeAtomizesToTypedValue()
    {
        var node = new XPathNode(Fake.Attribute);
        var source = new FakeAnnotations(node, XdmItem.FromInteger(42));

        var result = XPathAtomization.Atomize(XdmSequence.Create(XdmItem.FromNode(node)), source);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo(XdmItem.FromInteger(42));
        await Assert.That(result[0].Kind).IsNotEqualTo(XdmItemKind.UntypedAtomic);
    }

    [Test]
    public async Task Atomize_NullSource_Throws() =>
        await Assert.That(() => XPathAtomization.Atomize(XdmSequence.Empty, null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task EmptySequenceIsFalse() =>
        await Assert.That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Empty)).IsFalse();

    [Test]
    public async Task NodeFirstIsTrueEvenWithOtherItems()
    {
        await Assert.That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(Text))).IsTrue();
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(Seq(Text, XdmItem.FromInteger(0), XdmItem.FromString(""))))
            .IsTrue();
    }

    [Test]
    [Arguments(true, true)]
    [Arguments(false, false)]
    public async Task BooleanIsItsValue(bool value, bool expected) =>
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(XdmItem.FromBoolean(value))))
            .IsEqualTo(expected);

    [Test]
    [Arguments("", false)]
    [Arguments("0", true)]
    [Arguments("false", true)]
    [Arguments(" ", true)]
    public async Task StringFamiliesAreFalseOnlyWhenEmpty(string value, bool expected)
    {
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(XdmItem.FromString(value))))
            .IsEqualTo(expected);
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(XdmItem.FromUntypedAtomic(value))))
            .IsEqualTo(expected);
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(XdmItem.FromAnyUri(value))))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task AnyUriEmptyIsFalse() =>
        await Assert
            .That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(XdmItem.FromAnyUri(string.Empty))))
            .IsFalse();

    [Test]
    public async Task NaNAndZeroAreFalse()
    {
        XdmItem[] falsy =
        [
            XdmItem.FromDouble(double.NaN),
            XdmItem.FromDouble(0.0),
            XdmItem.FromDouble(-0.0),
            XdmItem.FromFloat(float.NaN),
            XdmItem.FromFloat(0f),
            XdmItem.FromInteger(0),
            XdmItem.FromDecimal(XsdDecimal.Zero),
        ];
        XdmItem[] truthy =
        [
            XdmItem.FromDouble(double.PositiveInfinity),
            XdmItem.FromDouble(-1.5),
            XdmItem.FromFloat(0.5f),
            XdmItem.FromInteger(-3),
            XdmItem.FromDecimal(new XsdDecimal(1, 2)),
        ];

        foreach (var item in falsy)
        {
            await Assert.That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(item))).IsFalse();
        }

        foreach (var item in truthy)
        {
            await Assert.That(XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(item))).IsTrue();
        }
    }

    [Test]
    public async Task TwoAtomicValuesThrowForg0006()
    {
        var failure = Assert.Throws<XPathException>(() =>
            XPathAtomization.EffectiveBooleanValue(Seq(XdmItem.FromInteger(1), XdmItem.FromInteger(2)))
        );

        await Assert.That(failure.Diagnostic.QName).IsEqualTo(XPathErrorCodes.FORG0006);
    }

    [Test]
    public async Task DateThrowsForg0006()
    {
        var date = XdmItem.FromDateTime(XsdDateTime.Parse("2024-05-06", XsdDateTimeKind.Date, XsdVersion.V11).Value);

        var failure = Assert.Throws<XPathException>(() =>
            XPathAtomization.EffectiveBooleanValue(XdmSequence.Create(date))
        );

        await Assert.That(failure.Diagnostic.QName).IsEqualTo(XPathErrorCodes.FORG0006);
    }

    [Test]
    public async Task SingletonEbvAndAtomizeAllocateZero()
    {
        var truthy = XdmSequence.Create(XdmItem.FromInteger(5));
        var text = XdmSequence.Create(XdmItem.FromString("x"));
        var ebv = false;
        XdmSequence atomized = default;

        AllocationAssert.NoAllocation(() =>
        {
            ebv = XPathAtomization.EffectiveBooleanValue(truthy) & XPathAtomization.EffectiveBooleanValue(text);
            atomized = XPathAtomization.Atomize(text);
        });

        await Assert.That(ebv).IsTrue();
        await Assert.That(atomized[0]).IsEqualTo(XdmItem.FromString("x"));
    }

    private sealed class FakeAnnotations(XPathNode annotated, XdmItem value) : IXdmTypeAnnotationSource
    {
        public XdmTypeId GetTypeAnnotation(in XPathNode node) => node.TypeAnnotation;

        public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
        {
            typedValue = value;
            return node.IsSameNode(annotated);
        }

        public bool TryGetTypedValues(in XPathNode node, out XdmSequence values)
        {
            values = XdmSequence.Empty;
            return false;
        }

        public bool HasTypedValue(in XPathNode node) => true;

        public bool IsNilled(in XPathNode node) => false;
    }
}
