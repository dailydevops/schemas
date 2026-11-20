namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Annotations;

using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class AnnotationTests
{
    private const string Xs = AnnotationFixture.Xs;

    private static AnnotationFixture.AnnotatedRun Run(string xml) =>
        AnnotationFixture.Run(AnnotationFixture.Schema(), xml);

    [Test]
    public async Task Annotation_ValidFull_CarriesTypeName_ForAnElementWithSimpleContent()
    {
        var run = Run("<r><i>42</i></r>");

        var annotation = run.Annotation("i");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypeName).IsEqualTo(new XsdTypeName(Xs, "integer", false));
        await Assert.That(annotation.IsNilled).IsFalse();
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(items[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42L);
    }

    [Test]
    public async Task Annotation_ValidFull_CarriesTypeName_ForAnElementWithComplexContent()
    {
        var run = Run("<r><c><n>7</n></c></r>");

        var annotation = run.Annotation("c");
        var root = run.Annotation("r");

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypeName).IsEqualTo(new XsdTypeName(string.Empty, "Named", false));
        await Assert.That(annotation.IsNilled).IsFalse();
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.None);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(0);
        await Assert.That(annotation.CopyTypedValue(new XdmItem[1])).IsEqualTo(0);
        await Assert.That(root.TypeName.IsAnonymous).IsTrue();
        await Assert.That(root.TypeName.LocalName).IsEqualTo(string.Empty);
        await Assert.That(run.Annotation("n").Validity).IsEqualTo(XsdValidity.Valid);
    }

    [Test]
    public async Task Annotation_ValidFull_CarriesTypeName_ForAnAttribute()
    {
        var run = Run("<r><w a=\"5\" s=\"x\"/></r>");
        var element = run.Element("w");

        var number = run.Annotation(run.Attribute(element, "a"));
        var text = run.Annotation(run.Attribute(element, "s"));
        var numbers = run.TypedValue(number);
        var texts = run.TypedValue(text);

        await Assert.That(number.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(number.TypeName).IsEqualTo(new XsdTypeName(Xs, "integer", false));
        await Assert.That(number.IsNilled).IsFalse();
        await Assert.That(number.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(numbers.Length).IsEqualTo(1);
        await Assert.That(numbers[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(5L);
        await Assert.That(text.TypeName.LocalName).IsEqualTo("string");
        await Assert.That(texts[0].TryGetString(out var s)).IsTrue();
        await Assert.That(s).IsEqualTo("x");
    }

    [Test]
    public async Task Annotation_InvalidElement_MapsToAnyType_AndKeepsTheUntypedValue()
    {
        var run = Run("<r><i>abc</i></r>");

        var annotation = run.Annotation("i");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(annotation.TypeName).IsEqualTo(new XsdTypeName(Xs, "anyType", false));
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(run.Annotation("r").Validity).IsEqualTo(XsdValidity.Invalid);
    }

    [Test]
    public async Task Annotation_InvalidAttribute_MapsToAnySimpleType_AndKeepsTheUntypedValue()
    {
        var run = Run("<r><w a=\"zz\"/></r>");

        var annotation = run.Annotation(run.Attribute(run.Element("w"), "a"));
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Invalid);
        await Assert.That(annotation.TypeName).IsEqualTo(new XsdTypeName(Xs, "anySimpleType", false));
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_ListType_YieldsASequenceOfItems()
    {
        var run = Run("<r><l>1 2  3</l></r>");

        var annotation = run.Annotation("l");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(3);
        await Assert.That(items.Length).IsEqualTo(3);
        for (var i = 0; i < 3; i++)
        {
            await Assert.That(items[i].TypeId).IsEqualTo(XdmTypeId.XsInteger);
            await Assert.That(items[i].TryGetInteger(out var value)).IsTrue();
            await Assert.That(value).IsEqualTo(i + 1L);
        }
    }

    [Test]
    public async Task Annotation_EmptyList_IsAtomicWithoutItems()
    {
        var annotation = Run("<r><l/></r>").Annotation("l");

        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(0);
    }

    [Test]
    public async Task Annotation_ListType_CopiesOnlyWhatFitsTheDestination()
    {
        var run = Run("<r><l>1 2 3</l></r>");
        var destination = new XdmItem[2];

        var written = run.Annotation("l").CopyTypedValue(destination);

        await Assert.That(written).IsEqualTo(2);
        await Assert.That(destination[1].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(2L);
    }

    [Test]
    public async Task Annotation_UnionType_YieldsTheValueOfTheMatchingMemberType()
    {
        var run = Run("<r><u>7</u><u>true</u></r>");
        var first = run.Annotation(run.Element("u"));
        var second = run.Annotation(run.Tree.Document.FirstChild.FirstChild.NextSibling);

        var number = run.TypedValue(first);
        var flag = run.TypedValue(second);

        await Assert.That(number[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(number[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(7L);
        await Assert.That(flag[0].TypeId).IsEqualTo(XdmTypeId.Boolean);
        await Assert.That(flag[0].TryGetBoolean(out var truth)).IsTrue();
        await Assert.That(truth).IsTrue();
    }

    [Test]
    public async Task Annotation_MixedContent_HasTheStringValueAsUntypedAtomic()
    {
        var annotation = Run("<r><m>some text</m></r>").Annotation("m");

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Untyped);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(1);
    }

    [Test]
    public async Task Annotation_EmptyContent_HasAnEmptyTypedValue()
    {
        var annotation = Run("<r><e/></r>").Annotation("e");

        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Empty);
        await Assert.That(annotation.TypedValueCount).IsEqualTo(0);
    }

    [Test]
    public async Task Annotation_SimpleContentOfAComplexType_HasTheTypedValueOfItsSimpleType()
    {
        var run = Run("<r><sc>12</sc></r>");

        var annotation = run.Annotation("sc");
        var items = run.TypedValue(annotation);

        await Assert.That(annotation.TypeName.IsAnonymous).IsTrue();
        await Assert.That(annotation.TypedValueKind).IsEqualTo(XsdTypedValueKind.Atomic);
        await Assert.That(items[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(items[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(12L);
    }

    [Test]
    public async Task Annotation_UserType_AtomizesToBuiltInAncestor()
    {
        var run = Run("<r><ri>5</ri><rn>6</rn></r>");

        var anonymous = run.Annotation("ri");
        var named = run.Annotation("rn");
        var anonymousItems = run.TypedValue(anonymous);
        var namedItems = run.TypedValue(named);

        await Assert.That(anonymous.TypeName.IsAnonymous).IsTrue();
        await Assert.That(named.TypeName).IsEqualTo(new XsdTypeName("urn:t", "small", false));
        await Assert.That(anonymousItems[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(namedItems[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(namedItems[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(6L);
    }

    [Test]
    public async Task Annotation_BuiltInListTypes_YieldOneItemPerToken()
    {
        var run = Run("<r><nm>a b</nm><ids>x y z</ids><ents>e1</ents></r>");

        var tokens = run.TypedValue(run.Annotation("nm"));
        var references = run.TypedValue(run.Annotation("ids"));
        var entities = run.TypedValue(run.Annotation("ents"));

        await Assert.That(tokens.Length).IsEqualTo(2);
        await Assert.That(tokens[0].TryGetString(out var first)).IsTrue();
        await Assert.That(first).IsEqualTo("a");
        await Assert.That(references.Length).IsEqualTo(3);
        await Assert.That(entities.Length).IsEqualTo(1);
        await Assert.That(entities[0].TryGetString(out var entity)).IsTrue();
        await Assert.That(entity).IsEqualTo("e1");
    }

    [Test]
    public async Task Annotation_TypeWithoutABuiltInAncestor_KeepsTheValueUntyped()
    {
        var run = Run("<r><weird>q</weird><any>z</any></r>");

        var weird = run.Annotation("weird");
        var any = run.Annotation("any");

        await Assert.That(weird.TypeName).IsEqualTo(new XsdTypeName("urn:t", "weird", false));
        await Assert.That(run.TypedValue(weird)[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(any.TypeName.LocalName).IsEqualTo("anySimpleType");
        await Assert.That(run.TypedValue(any)[0].TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task Annotation_Ordinal_CountsEveryElementStartEvent_IncludingUnassessedOnes()
    {
        var run = AnnotationFixture.Run(
            AnnotationFixture.LaxSchema(),
            "<r><unknown a=\"1\">text<child/></unknown><k>5</k></r>"
        );

        var known = run.Annotation("k");
        var found = run.Source.TryGetAnnotation(run.Element("unknown"), out _);
        var nested = run.Source.TryGetAnnotation(run.Element("child"), out _);
        var unknownAttribute = run.Source.TryGetAnnotation(run.Attribute(run.Element("unknown"), "a"), out _);

        await Assert.That(known.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(known.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(found).IsFalse();
        await Assert.That(nested).IsFalse();
        await Assert.That(unknownAttribute).IsFalse();
    }

    [Test]
    public async Task Annotation_AnAttributeInTheInstanceSchemaNamespace_DoesNotShiftTheIndexOfTheOthers()
    {
        var run = Run(
            "<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><w xsi:nil=\"false\" a=\"5\" s=\"x\"/></r>"
        );
        var element = run.Element("w");
        var control = run.Source.TryGetAnnotation(run.Attribute(element, "nil"), out _);

        var number = run.Annotation(run.Attribute(element, "a"));
        var text = run.Annotation(run.Attribute(element, "s"));

        await Assert.That(control).IsFalse();
        await Assert.That(number.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(text.TypeName.LocalName).IsEqualTo("string");
    }

    [Test]
    public async Task Annotation_AttributesAreKeyedInSourceOrder_AndDefaultedOnesFollowTheSpecifiedOnes()
    {
        var run = Run("<r><w s=\"x\" a=\"5\"/></r>");
        var element = run.Element("w");

        var text = run.Annotation(run.Attribute(element, "s"));
        var number = run.Annotation(run.Attribute(element, "a"));

        await Assert.That(text.TypeName.LocalName).IsEqualTo("string");
        await Assert.That(number.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(run.Table.TryGetAttribute(2, 2, out var defaulted)).IsTrue();
        await Assert.That(defaulted.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(run.Table.AttributeCount).IsEqualTo(3);
    }

    [Test]
    public async Task Annotation_Ordinal_IsIdenticalForTheValidatorAndTheTreeOfTheSameDocument()
    {
        const string Xml = "<r><i>1</i><c><n>2</n></c><i>3</i></r>";
        var run = Run(Xml);

        var second = run.Tree.Document.FirstChild.FirstChild.NextSibling.NextSibling;
        var annotation = run.Annotation(second);
        var items = run.TypedValue(annotation);

        await Assert.That(run.Table.ElementOrdinalLimit).IsEqualTo(5);
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(items[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(3L);
    }
}
