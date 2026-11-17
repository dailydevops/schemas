namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using static AxisFixture;

public class NodeTestTests
{
    // Name tests: QName, *, prefix:*, *:local; the principal node kind of the axis is respected.
    [Test]
    [Arguments("child::b", "a", "b b")]
    [Arguments("child::p:e", "a", "p:e")]
    [Arguments("child::e", "a", "")]
    [Arguments("child::zzz", "a", "")]
    [Arguments("child::*", "a", "b b p:e")]
    [Arguments("child::*", "doc", "a")]
    [Arguments("child::p:*", "a", "p:e")]
    [Arguments("child::*:b", "a", "b b")]
    [Arguments("child::*:e", "a", "p:e")]
    [Arguments("child::*:zzz", "a", "")]
    [Arguments("child::id", "a", "")]
    [Arguments("descendant::*", "doc", "a b b i p:e")]
    [Arguments("descendant-or-self::*", "a", "a b b i p:e")]
    [Arguments("self::*", "id", "")]
    [Arguments("self::id", "id", "")]
    [Arguments("self::a", "a", "a")]
    [Arguments("attribute::*", "a", "@id @p:lang")]
    [Arguments("attribute::id", "a", "@id")]
    [Arguments("attribute::lang", "a", "")]
    [Arguments("attribute::p:lang", "a", "@p:lang")]
    [Arguments("attribute::p:*", "a", "@p:lang")]
    [Arguments("attribute::*:id", "a", "@id")]
    [Arguments("attribute::*:lang", "a", "@p:lang")]
    [Arguments("attribute::b", "a", "")]
    [Arguments("@id", "a", "@id")]
    public async Task Walk_NameTest_MatchesOnInternedNamesAndPrincipalKind(
        string step,
        string context,
        string expected
    ) => await Assert.That(Eval(step, context)).IsEqualTo(expected);

    [Test]
    public async Task Walk_DefaultElementNamespace_AppliesToElementNamesOnly()
    {
        var statics = Statics.ToBuilder().WithDefaultElementNamespace("urn:p").Build();

        await Assert.That(Eval("child::e", "a", statics)).IsEqualTo("p:e");
        await Assert.That(Eval("child::b", "a", statics)).IsEqualTo("");
        await Assert.That(Eval("attribute::id", "a", statics)).IsEqualTo("@id");
    }

    [Test]
    [Arguments("child::text()", "b2", "'t2' 't3'")]
    [Arguments("child::text()", "a", "")]
    [Arguments("self::text()", "t1", "'t1'")]
    [Arguments("descendant::text()", "a", "'t1' 't2' 'x' 't3'")]
    [Arguments("attribute::text()", "a", "")]
    [Arguments("child::comment()", "a", "#c")]
    [Arguments("self::comment()", "c", "#c")]
    [Arguments("child::comment()", "b1", "")]
    [Arguments("child::processing-instruction()", "a", "?pi")]
    [Arguments("child::processing-instruction(pi)", "a", "?pi")]
    [Arguments("child::processing-instruction('pi')", "a", "?pi")]
    [Arguments("child::processing-instruction(other)", "a", "")]
    [Arguments("child::node()", "a", "b #c ?pi b p:e")]
    [Arguments("attribute::node()", "a", "@id @p:lang")]
    [Arguments("child::element()", "a", "b b p:e")]
    [Arguments("child::element(*)", "a", "b b p:e")]
    [Arguments("child::element(b)", "a", "b b")]
    [Arguments("child::element(p:e)", "a", "p:e")]
    [Arguments("child::element(e)", "a", "")]
    [Arguments("self::element(a)", "a", "a")]
    [Arguments("child::attribute()", "a", "")]
    [Arguments("attribute::attribute()", "a", "@id @p:lang")]
    [Arguments("attribute::attribute(*)", "a", "@id @p:lang")]
    [Arguments("attribute::attribute(id)", "a", "@id")]
    [Arguments("attribute::attribute(p:lang)", "a", "@p:lang")]
    [Arguments("attribute::attribute(lang)", "a", "")]
    [Arguments("self::attribute(id)", "id", "@id")]
    [Arguments("self::element()", "id", "")]
    [Arguments("self::document-node()", "doc", "/")]
    [Arguments("self::document-node()", "a", "")]
    [Arguments("self::document-node(element(a))", "doc", "/")]
    [Arguments("self::document-node(element(*))", "doc", "/")]
    [Arguments("self::document-node(element(b))", "doc", "")]
    [Arguments("self::document-node(element(a))", "a", "")]
    public async Task Walk_KindTest_MatchesTheNodeKind(string step, string context, string expected) =>
        await Assert.That(Eval(step, context)).IsEqualTo(expected);

    [Test]
    [Arguments("child::schema-element(a)")]
    [Arguments("child::schema-attribute(id)")]
    [Arguments("child::element(a, xs:string)")]
    [Arguments("child::attribute(id, xs:string)")]
    [Arguments("self::document-node(schema-element(a))")]
    public async Task TryFromSyntax_TestOutsideTheScope_ReturnsFalse(string step)
    {
        var parsed = Parse(step);

        var supported = XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out _);

        await Assert.That(supported).IsFalse();
    }

    [Test]
    public async Task TryFromSyntax_UnboundPrefix_YieldsXPST0081()
    {
        var parsed = Parse("child::q:x");

        var failure = Assert.Throws<XPathException>(() =>
            XPathNodeTest.TryFromSyntax(parsed.Test, parsed.Axis, Statics, out _)
        );

        await Assert.That(failure.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0081);
    }
}
