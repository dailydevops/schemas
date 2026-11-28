namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static QNameFixture;

/// <summary>
/// The casts to and from <c>xs:QName</c>: the prefix of a lexical QName is resolved against the namespaces of the static context, and the language
/// version decides which operands may be cast (XPath 2.0 only a string literal; XPath 3.0 and 3.1 any string or untyped value).
/// </summary>
public class QNameCastTests
{
    // A string literal is cast with the namespaces of the static context; the string value shows the prefix as it was written.
    [Test]
    [Arguments("'local' cast as xs:QName", "local")]
    [Arguments("'p:local' cast as xs:QName", "p:local")]
    [Arguments("' p:local ' cast as xs:QName", "p:local")]
    [Arguments("'xs:integer' cast as xs:QName", "xs:integer")]
    [Arguments("'xml:space' cast as xs:QName", "xml:space")]
    [Arguments("('p:local') cast as xs:QName", "p:local")]
    [Arguments("('p:local' cast as xs:QName) cast as xs:QName", "p:local")]
    public async Task FromStringLiteral_ProducesTheValue(string expression, string text)
    {
        var item = Item(expression, V20);

        await Assert.That(item.Kind).IsEqualTo(XdmItemKind.QName);
        await Assert.That(Text(expression, V20)).IsEqualTo(text);
    }

    [Test]
    [Arguments("'p:local' cast as xs:QName", "p:local")]
    [Arguments("('q:local' cast as xs:QName) cast as xs:untypedAtomic", "q:local")]
    [Arguments("xs:QName('local') cast as xs:untypedAtomic", "local")]
    public async Task ToUntypedAtomic_GivesTheLexicalForm(string expression, string text)
    {
        var item = Item("(" + expression + ") cast as xs:untypedAtomic", V20);

        await Assert.That(item.Kind).IsEqualTo(XdmItemKind.UntypedAtomic);
        _ = item.TryGetUntypedAtomic(out var value);
        await Assert.That(value).IsEqualTo(text);
    }

    // The value is the expanded name: namespace and local name, and the prefix only for the string value.
    [Test]
    public async Task Value_IsTheExpandedName_AndKeepsThePrefix()
    {
        _ = Item("'p:local' cast as xs:QName", V20).TryGetQName(out var name);

        await Assert.That(name).IsNotNull();
        await Assert.That(name!.Name.NamespaceName).IsEqualTo(P);
        await Assert.That(name.Name.LocalName).IsEqualTo("local");
        await Assert.That(name.Prefix).IsEqualTo("p");
        await Assert.That(name.ToString()).IsEqualTo("p:local");
    }

    [Test]
    public async Task UnprefixedName_IsInNoNamespace_WithoutADefaultElementNamespace()
    {
        _ = Item("'local' cast as xs:QName", V20).TryGetQName(out var name);

        await Assert.That(name!.Name.NamespaceName).IsNull();
        await Assert.That(name.Prefix).IsNull();
    }

    // Decision: an unprefixed lexical QName takes the default element namespace of the static context, as the lexical mapping of xs:QName
    // takes the default namespace in scope.
    [Test]
    public async Task UnprefixedName_TakesTheDefaultElementNamespace()
    {
        var statics = V20.ToBuilder().WithDefaultElementNamespace("urn:default").Build();

        _ = Item("'local' cast as xs:QName", statics).TryGetQName(out var name);

        await Assert.That(name!.Name.NamespaceName).IsEqualTo("urn:default");
        await Assert.That(Text("'local' cast as xs:QName", statics)).IsEqualTo("local");
        await Assert.That(Item("'p:local' cast as xs:QName", statics).TryGetQName(out var prefixed)).IsTrue();
        await Assert.That(prefixed!.Name.NamespaceName).IsEqualTo(P);
    }

    [Test]
    public async Task TwoPrefixesForOneNamespace_GiveEqualValues_WithDifferentStringValues()
    {
        var first = Item("'p:local' cast as xs:QName", V20);
        var second = Item("'q:local' cast as xs:QName", V20);
        var other = Item("'p:other' cast as xs:QName", V20);

        await Assert.That(first).IsEqualTo(second);
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
        await Assert.That(first).IsNotEqualTo(other);
        await Assert.That(Text("'p:local' cast as xs:QName", V20)).IsEqualTo("p:local");
        await Assert.That(Text("'q:local' cast as xs:QName", V20)).IsEqualTo("q:local");
    }

    // XPath 2.0: only a string literal is cast to xs:QName; any other operand is XPTY0004, and castable is false.
    [Test]
    [Arguments("xs:string('local')")]
    [Arguments("xs:string('p:local')")]
    [Arguments("xs:untypedAtomic('local')")]
    [Arguments("xs:untypedAtomic('p:local')")]
    [Arguments("xs:untypedAtomic('an arbitrary string')")]
    [Arguments("xs:string(xs:untypedAtomic('local'))")]
    public async Task Xpath20_NotAStringLiteral_IsNotAllowed(string operand)
    {
        await Assert.That(Code(operand + " cast as xs:QName", V20)).IsEqualTo("XPTY0004");
        await Assert.That(Code(operand + " castable as xs:QName", V20)).IsEqualTo(string.Empty);
        await Assert.That(Castable(operand, "QName", V20)).IsFalse();
    }

    // XPath 3.0 and 3.1: any string or untyped value, against the statically known namespaces.
    [Test]
    [Arguments("xs:string('local')", "local")]
    [Arguments("xs:string('p:local')", "p:local")]
    [Arguments("xs:untypedAtomic('local')", "local")]
    [Arguments("xs:untypedAtomic(' p:local ')", "p:local")]
    [Arguments("'p:local'", "p:local")]
    public async Task Xpath30And31_AnyStringOrUntypedOperand_IsCast(string operand, string text)
    {
        foreach (var statics in (XPathStaticContext[])[V30, V31])
        {
            var expression = operand + " cast as xs:QName";

            await Assert.That(Item(expression, statics).Kind).IsEqualTo(XdmItemKind.QName);
            await Assert.That(Text(expression, statics)).IsEqualTo(text);
            await Assert.That(Castable(operand, "QName", statics)).IsTrue();
        }
    }

    [Test]
    [Arguments("xs:untypedAtomic('1abc') cast as xs:QName", "FORG0001")]
    [Arguments("xs:untypedAtomic('zz:local') cast as xs:QName", "FONS0004")]
    [Arguments("xs:integer('1') cast as xs:QName", "XPTY0004")]
    public async Task Xpath30_ErrorsOfTheLexicalForm(string expression, string code) =>
        await Assert.That(Code(expression, V30)).IsEqualTo(code);

    // FORG0001: the lexical form is not a QName. FONS0004: the prefix is not bound.
    private static (string, string)[] Failures() =>
        [
            ("'1abc' cast as xs:QName", "FORG0001"),
            ("'a:b:c' cast as xs:QName", "FORG0001"),
            ("'' cast as xs:QName", "FORG0001"),
            ("'  ' cast as xs:QName", "FORG0001"),
            ("'p:' cast as xs:QName", "FORG0001"),
            ("':local' cast as xs:QName", "FORG0001"),
            ("'a b' cast as xs:QName", "FORG0001"),
            ("'zz:local' cast as xs:QName", "FONS0004"),
            ("'zz:p:local' cast as xs:QName", "FORG0001"),
            ("xs:integer('1') cast as xs:QName", "XPTY0004"),
            ("xs:hexBinary('0F') cast as xs:QName", "XPTY0004"),
            ("xs:anyURI('http://a') cast as xs:QName", "XPTY0004"),
            ("xs:date('2004-10-13') cast as xs:QName", "XPTY0004"),
            ("() cast as xs:QName", "XPTY0004"),
            ("xs:QName('local') cast as xs:integer", "XPTY0004"),
            ("xs:QName('local') cast as xs:anyURI", "XPTY0004"),
            ("xs:QName('local') cast as xs:dateTime", "XPTY0004"),
            ("xs:QName('local') cast as xs:hexBinary", "XPTY0004"),
            ("xs:QName('local') cast as xs:boolean", "XPTY0004"),
        ];

    public static IEnumerable<Func<(string Expression, string Code)>> FailureCases()
    {
        foreach (var entry in Failures())
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(FailureCases))]
    public async Task Cast_Failure_RaisesItsCode(string expression, string code) =>
        await Assert.That(Code(expression, V20)).IsEqualTo(code);

    // castable as never raises FONS0004 or FORG0001; it is false.
    [Test]
    [MethodDataSource(nameof(FailureCases))]
    public async Task Castable_EveryFailingCast_ReturnsFalseAndRaisesNothing(string expression, string code)
    {
        _ = code;
        var castable = CastFixture.AsCastable(expression);

        await Assert.That(Code(castable, V20)).IsEqualTo(string.Empty);
        _ = Item(castable, V20).TryGetBoolean(out var value);
        await Assert.That(value).IsFalse();
    }

    // The constructor function is lowered with the static context of the call, as cast as is.
    [Test]
    [Arguments("xs:QName('local')", "local")]
    [Arguments("xs:QName(' local ')", "local")]
    [Arguments("xs:QName(xs:QName('local'))", "local")]
    public async Task Constructor_UnprefixedName_ProducesTheValue(string expression, string text)
    {
        await Assert.That(CastShow.KindOf(expression)).IsEqualTo(XdmItemKind.QName);
        await Assert.That(CastShow.Show(expression)).IsEqualTo("QName:" + text);
    }

    [Test]
    [Arguments("xs:QName('zz:local')", "FONS0004")]
    [Arguments("xs:QName('1abc')", "FORG0001")]
    [Arguments("xs:QName(1)", "XPTY0004")]
    [Arguments("xs:QName(('a', 'b'))", "XPTY0004")]
    [Arguments("xs:QName()", "XPST0017")]
    public async Task Constructor_Error_RaisesItsCode(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);

    [Test]
    public async Task Constructor_OfTheEmptySequence_IsTheEmptySequence() =>
        await Assert.That(CastFixture.Evaluate("xs:QName(())").Count).IsEqualTo(0);

    [Test]
    [Arguments("xs:QName('p:local')")]
    [Arguments("xs:QName(' p:local ')")]
    [Arguments("xs:QName(('p:local'))")]
    [Arguments("xs:QName('q:local')")]
    public async Task Constructor_PrefixedName_IsResolvedAgainstTheStaticContext(string expression)
    {
        foreach (var statics in (XPathStaticContext[])[V20, V30, V31])
        {
            await Assert.That(Item(expression, statics).TryGetQName(out var name)).IsTrue();
            await Assert.That(name!.Name.NamespaceName).IsEqualTo(P);
            await Assert.That(name.Name.LocalName).IsEqualTo("local");
        }
    }

    [Test]
    public async Task Constructor_UnboundPrefix_IsFons0004InEveryVersion()
    {
        foreach (var statics in (XPathStaticContext[])[V20, V30, V31])
        {
            await Assert.That(Code("xs:QName('zz:local')", statics)).IsEqualTo("FONS0004");
        }
    }

    [Test]
    public async Task Constructor_UnprefixedName_TakesTheDefaultElementNamespace()
    {
        var statics = V20.ToBuilder().WithDefaultElementNamespace("urn:default").Build();

        await Assert.That(Item("xs:QName('local')", statics).TryGetQName(out var name)).IsTrue();
        await Assert.That(name!.Name.NamespaceName).IsEqualTo("urn:default");
    }

    // The constructor function is cast as xs:QName?: XPath 2.0 takes a string literal alone, XPath 3.0 and 3.1 any string or untyped value.
    [Test]
    [Arguments("xs:string('local')")]
    [Arguments("xs:string('p:local')")]
    [Arguments("xs:untypedAtomic('p:local')")]
    [Arguments("xs:untypedAtomic('an arbitrary string')")]
    public async Task Xpath20_Constructor_NotAStringLiteral_IsXpty0004(string operand) =>
        await Assert.That(Code("xs:QName(" + operand + ")", V20)).IsEqualTo("XPTY0004");

    [Test]
    [Arguments("xs:string('p:local')", "p:local")]
    [Arguments("xs:untypedAtomic(' p:local ')", "p:local")]
    public async Task Xpath30And31_Constructor_AnyStringOrUntypedOperand_IsCast(string operand, string text)
    {
        foreach (var statics in (XPathStaticContext[])[V30, V31])
        {
            var expression = "xs:QName(" + operand + ")";

            await Assert.That(Text(expression, statics)).IsEqualTo(text);
        }
    }

    [Test]
    public async Task Constructor_InCompatibilityMode_IsResolvedAgainstTheStaticContext()
    {
        var statics = V20.ToBuilder().WithXPath10Compatibility(true).Build();

        await Assert.That(Item("xs:QName('p:local')", statics).TryGetQName(out var name)).IsTrue();
        await Assert.That(name!.Name.NamespaceName).IsEqualTo(P);
    }

    [Test]
    public async Task Constructor_OfTheEmptySequence_IsTheEmptySequenceInEveryVersion()
    {
        foreach (var statics in (XPathStaticContext[])[V20, V30, V31])
        {
            await Assert.That(Evaluate("xs:QName(())", statics).Count).IsEqualTo(0);
        }
    }
}
