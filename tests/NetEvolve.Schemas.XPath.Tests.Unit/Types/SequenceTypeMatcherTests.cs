namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TypeFixture;

public class SequenceTypeMatcherTests
{
    private static XdmItem Item(string kind) =>
        kind switch
        {
            "boolean" => XdmItem.FromBoolean(true),
            "integer" => XdmItem.FromInteger(1),
            "double" => XdmItem.FromDouble(1.5),
            "float" => XdmItem.FromFloat(1.5f),
            "decimal" => XdmItem.FromDecimal(new XsdDecimal(15, 1)),
            "string" => XdmItem.FromString("s"),
            "untypedAtomic" => XdmItem.FromUntypedAtomic("u"),
            "anyURI" => XdmItem.FromAnyUri("urn:x"),
            "dateTime" => XdmItem.FromDateTime(
                XsdDateTime.Parse("2024-05-06T07:08:09Z", XsdDateTimeKind.DateTime, XsdVersion.V11).Value
            ),
            "date" => XdmItem.FromDateTime(XsdDateTime.Parse("2024-05-06", XsdDateTimeKind.Date, XsdVersion.V11).Value),
            "time" => XdmItem.FromDateTime(XsdDateTime.Parse("07:08:09", XsdDateTimeKind.Time, XsdVersion.V11).Value),
            "duration" => XdmItem.FromDuration(XsdDuration.Parse("P1Y2M3DT4H5M6S").Value),
            "yearMonthDuration" => XdmItem.FromDuration(
                XsdDuration.Parse("P1Y2M", DurationKind.YearMonth).Value,
                DurationKind.YearMonth
            ),
            "dayTimeDuration" => XdmItem.FromDuration(
                XsdDuration.Parse("P3DT4H", DurationKind.DayTime).Value,
                DurationKind.DayTime
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static bool Matches(string type, params XdmItem[] items) => Matcher(type).Matches(Seq(items));

    [Test]
    [Arguments("boolean", "xs:boolean", true)]
    [Arguments("boolean", "xs:string", false)]
    [Arguments("integer", "xs:integer", true)]
    [Arguments("integer", "xs:decimal", true)]
    [Arguments("integer", "xs:double", false)]
    [Arguments("integer", "xs:string", false)]
    [Arguments("decimal", "xs:decimal", true)]
    [Arguments("decimal", "xs:integer", false)]
    [Arguments("double", "xs:double", true)]
    [Arguments("double", "xs:float", false)]
    [Arguments("double", "xs:decimal", false)]
    [Arguments("float", "xs:float", true)]
    [Arguments("float", "xs:double", false)]
    [Arguments("string", "xs:string", true)]
    [Arguments("string", "xs:normalizedString", false)]
    [Arguments("string", "xs:untypedAtomic", false)]
    [Arguments("untypedAtomic", "xs:untypedAtomic", true)]
    [Arguments("untypedAtomic", "xs:string", false)]
    [Arguments("anyURI", "xs:anyURI", true)]
    [Arguments("anyURI", "xs:string", false)]
    [Arguments("dateTime", "xs:dateTime", true)]
    [Arguments("dateTime", "xs:date", false)]
    [Arguments("date", "xs:date", true)]
    [Arguments("date", "xs:dateTime", false)]
    [Arguments("time", "xs:time", true)]
    [Arguments("time", "xs:dateTime", false)]
    [Arguments("duration", "xs:duration", true)]
    [Arguments("duration", "xs:dayTimeDuration", false)]
    [Arguments("yearMonthDuration", "xs:yearMonthDuration", true)]
    [Arguments("yearMonthDuration", "xs:duration", true)]
    [Arguments("yearMonthDuration", "xs:dayTimeDuration", false)]
    [Arguments("dayTimeDuration", "xs:dayTimeDuration", true)]
    [Arguments("dayTimeDuration", "xs:duration", true)]
    [Arguments("dayTimeDuration", "xs:yearMonthDuration", false)]
    public async Task AtomicItem_AgainstAtomicType_MatchesByDerivation(string kind, string type, bool expected) =>
        await Assert.That(Matches(type, Item(kind))).IsEqualTo(expected);

    [Test]
    [Arguments("boolean")]
    [Arguments("integer")]
    [Arguments("double")]
    [Arguments("float")]
    [Arguments("decimal")]
    [Arguments("string")]
    [Arguments("untypedAtomic")]
    [Arguments("anyURI")]
    [Arguments("dateTime")]
    [Arguments("date")]
    [Arguments("time")]
    [Arguments("duration")]
    [Arguments("yearMonthDuration")]
    [Arguments("dayTimeDuration")]
    public async Task EveryAtomicItem_MatchesAnyAtomicTypeAndItemAndNoNodeKind(string kind)
    {
        var item = Item(kind);

        await Assert.That(Matches("xs:anyAtomicType", item)).IsTrue();
        await Assert.That(Matches("item()", item)).IsTrue();
        await Assert.That(Matches("node()", item)).IsFalse();
        await Assert.That(Matches("element()", item)).IsFalse();
        await Assert.That(Matches("attribute()", item)).IsFalse();
        await Assert.That(Matches("text()", item)).IsFalse();
        await Assert.That(Matches("document-node()", item)).IsFalse();
    }

    [Test]
    public async Task NotationAndQName_AreAcceptedAsItemTypes()
    {
        await Assert.That(Matches("xs:QName", Item("string"))).IsFalse();
        await Assert.That(Matches("xs:NOTATION", Item("string"))).IsFalse();
        await Assert.That(Matches("xs:hexBinary*")).IsTrue();
    }

    [Test]
    [Arguments("a", "node()", true)]
    [Arguments("a", "item()", true)]
    [Arguments("a", "element()", true)]
    [Arguments("a", "element(*)", true)]
    [Arguments("a", "element(a)", true)]
    [Arguments("a", "element(b)", false)]
    [Arguments("a", "element(p:a)", false)]
    [Arguments("a", "attribute()", false)]
    [Arguments("a", "text()", false)]
    [Arguments("a", "xs:untypedAtomic", false)]
    [Arguments("a", "xs:string", false)]
    [Arguments("e", "element(p:e)", true)]
    [Arguments("e", "element(e)", false)]
    [Arguments("e", "element(*)", true)]
    [Arguments("id", "attribute()", true)]
    [Arguments("id", "attribute(*)", true)]
    [Arguments("id", "attribute(id)", true)]
    [Arguments("id", "attribute(lang)", false)]
    [Arguments("id", "element()", false)]
    [Arguments("id", "xs:untypedAtomic", false)]
    [Arguments("id", "node()", true)]
    [Arguments("lang", "attribute(p:lang)", true)]
    [Arguments("lang", "attribute(lang)", false)]
    [Arguments("t1", "text()", true)]
    [Arguments("t1", "node()", true)]
    [Arguments("t1", "comment()", false)]
    [Arguments("t1", "xs:untypedAtomic", false)]
    [Arguments("c", "comment()", true)]
    [Arguments("c", "text()", false)]
    [Arguments("pi", "processing-instruction()", true)]
    [Arguments("pi", "processing-instruction(pi)", true)]
    [Arguments("pi", "processing-instruction('pi')", true)]
    [Arguments("pi", "processing-instruction(other)", false)]
    [Arguments("pi", "comment()", false)]
    [Arguments("doc", "document-node()", true)]
    [Arguments("doc", "document-node(element(a))", true)]
    [Arguments("doc", "document-node(element(*))", true)]
    [Arguments("doc", "document-node(element(b))", false)]
    [Arguments("doc", "element()", false)]
    [Arguments("a", "document-node()", false)]
    public async Task NodeItem_AgainstKindTest_MatchesTheKindAndName(string node, string type, bool expected) =>
        await Assert.That(Matches(type, Node(node))).IsEqualTo(expected);

    // The type-annotation slot: an untyped element carries xs:untyped, an untyped attribute xs:untypedAtomic.
    [Test]
    [Arguments("a", "element(*, xs:untyped)", true)]
    [Arguments("a", "element(a, xs:untyped)", true)]
    [Arguments("a", "element(b, xs:untyped)", false)]
    [Arguments("a", "element(*, xs:anyType)", true)]
    [Arguments("a", "element(*, xs:untypedAtomic)", false)]
    [Arguments("a", "element(*, xs:anySimpleType)", false)]
    [Arguments("a", "element(*, xs:string)", false)]
    [Arguments("a", "element(*, xs:integer)", false)]
    [Arguments("id", "attribute(*, xs:untypedAtomic)", true)]
    [Arguments("id", "attribute(id, xs:untypedAtomic)", true)]
    [Arguments("id", "attribute(lang, xs:untypedAtomic)", false)]
    [Arguments("id", "attribute(*, xs:anyAtomicType)", true)]
    [Arguments("id", "attribute(*, xs:anySimpleType)", true)]
    [Arguments("id", "attribute(*, xs:anyType)", true)]
    [Arguments("id", "attribute(*, xs:untyped)", false)]
    [Arguments("id", "attribute(*, xs:string)", false)]
    [Arguments("id", "attribute(*, xs:integer)", false)]
    public async Task UntypedNode_AgainstTypedTest_MatchesOnlyItsUntypedAnnotation(
        string node,
        string type,
        bool expected
    ) => await Assert.That(Matches(type, Node(node))).IsEqualTo(expected);

    [Test]
    public async Task UntypedElement_MatchesNoBuiltInAtomicTypeExceptThroughAnyType()
    {
        var element = Node("a");
        foreach (var name in new[] { "string", "boolean", "decimal", "integer", "double", "dateTime", "untypedAtomic" })
        {
            await Assert.That(Matches($"element(*, xs:{name})", element)).IsFalse();
        }
    }

    [Test]
    [Arguments("a", "element(*, xs:integer)", true)]
    [Arguments("a", "element(a, xs:integer)", true)]
    [Arguments("a", "element(b, xs:integer)", false)]
    [Arguments("a", "element(*, xs:decimal)", true)]
    [Arguments("a", "element(*, xs:anyAtomicType)", true)]
    [Arguments("a", "element(*, xs:anyType)", true)]
    [Arguments("a", "element(*, xs:string)", false)]
    [Arguments("a", "element(*, xs:untyped)", false)]
    [Arguments("a", "element(*, xs:untypedAtomic)", false)]
    [Arguments("a", "element(*, xs:integer?)", true)]
    public async Task AnnotatedElement_MatchesItsAnnotationAndItsBaseTypes(string node, string type, bool expected)
    {
        var annotated = AxisFixture.Node("a");
        var source = new FakeAnnotations(annotated, XdmTypes.Get("integer"));

        var result = Matcher(type).Matches(Seq(Node(node)), source);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("id", "attribute(*, xs:integer)", true)]
    [Arguments("id", "attribute(id, xs:int)", false)]
    [Arguments("id", "attribute(*, xs:decimal)", true)]
    [Arguments("id", "attribute(*, xs:untypedAtomic)", false)]
    [Arguments("id", "attribute(*, xs:string)", false)]
    [Arguments("lang", "attribute(*, xs:integer)", false)]
    [Arguments("lang", "attribute(*, xs:untypedAtomic)", true)]
    public async Task AnnotatedAttribute_MatchesItsAnnotationAndItsBaseTypes(string node, string type, bool expected)
    {
        var source = new FakeAnnotations(AxisFixture.Node("id"), XdmTypes.Get("integer"));

        var result = Matcher(type).Matches(Seq(Node(node)), source);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task AnnotatedWithADerivedType_MatchesTheBaseTypeButNotTheReverse()
    {
        var source = new FakeAnnotations(AxisFixture.Node("a"), XdmTypes.Get("byte"));

        await Assert.That(Matcher("element(*, xs:short)").Matches(Seq(Node("a")), source)).IsTrue();
        await Assert.That(Matcher("element(*, xs:byte)").Matches(Seq(Node("a")), source)).IsTrue();
        await Assert.That(Matcher("element(*, xs:long)").Matches(Seq(Node("a")), source)).IsTrue();
        await Assert.That(Matcher("element(*, xs:unsignedByte)").Matches(Seq(Node("a")), source)).IsFalse();
    }

    [Test]
    public async Task ElementTypeTest_ReadsTheAnnotationSlotOfTheNode()
    {
        var source = new FakeAnnotations(AxisFixture.Node("a"), XdmTypes.Get("integer"));

        _ = Matcher("element(*, xs:integer)").Matches(Seq(Node("a")), source);
        _ = Matcher("attribute(*, xs:integer)").Matches(Seq(Node("id")), source);

        await Assert.That(source.Reads).IsEqualTo(2);
    }

    [Test]
    public async Task KindTestWithoutType_DoesNotConsultTheAnnotationSlot()
    {
        var source = new FakeAnnotations(AxisFixture.Node("a"), XdmTypes.Get("integer"));

        _ = Matcher("element(a)").Matches(Seq(Node("a")), source);
        _ = Matcher("node()").Matches(Seq(Node("a")), source);

        await Assert.That(source.Reads).IsEqualTo(0);
    }

    [Test]
    public async Task DocumentNodeWithElementTest_ChecksTheTypeOfTheDocumentElement()
    {
        var source = new FakeAnnotations(AxisFixture.Node("a"), XdmTypes.Get("integer"));
        var document = Seq(Node("doc"));

        await Assert.That(Matcher("document-node(element(a, xs:integer))").Matches(document, source)).IsTrue();
        await Assert.That(Matcher("document-node(element(*, xs:string))").Matches(document, source)).IsFalse();
        await Assert.That(Matcher("document-node(element(a, xs:untyped))").Matches(document)).IsTrue();
        await Assert.That(Matcher("document-node(element(a, xs:integer))").Matches(document)).IsFalse();
    }

    [Test]
    public async Task Matches_WithoutASource_UsesTheUntypedDefault() =>
        await Assert.That(Matcher("element(*, xs:untyped)").Matches(Seq(Node("a")))).IsTrue();

    [Test]
    public async Task Matches_NullSource_Throws() =>
        await Assert
            .That(() => Matcher("element(*, xs:untyped)").Matches(Seq(Node("a")), null!))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task MixedSequence_MatchesOnlyWhenEveryItemMatches()
    {
        var mixed = Seq(Node("a"), XdmItem.FromInteger(1), Node("c"));

        await Assert.That(Matcher("item()*").Matches(mixed)).IsTrue();
        await Assert.That(Matcher("node()*").Matches(mixed)).IsFalse();
        await Assert.That(Matcher("xs:integer*").Matches(mixed)).IsFalse();
        await Assert.That(Matcher("element()+").Matches(Seq(Node("a"), Node("b1")))).IsTrue();
        await Assert.That(Matcher("element()+").Matches(Seq(Node("a"), Node("c")))).IsFalse();
    }

    [Test]
    public async Task LazySequence_IsMatchedLikeAMaterializedOne()
    {
        var lazy = XdmSequence.FromLazy([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]);

        await Assert.That(Matcher("xs:integer+").Matches(lazy)).IsTrue();
        await Assert.That(Matcher("xs:integer").Matches(lazy)).IsFalse();
    }

    [Test]
    public async Task Create_NullArguments_Throw()
    {
        await Assert.That(() => SequenceTypeMatcher.Create(null!, Statics)).Throws<ArgumentNullException>();
        await Assert.That(() => SequenceTypeMatcher.Create(ParseType("item()"), null!)).Throws<ArgumentNullException>();
    }
}
