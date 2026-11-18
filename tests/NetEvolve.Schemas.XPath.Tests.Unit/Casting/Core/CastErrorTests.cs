namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static CastFixture;

/// <summary>The dynamic errors of <c>cast as</c> and the guarantee that <c>castable as</c> returns false for every one of them.</summary>
public class CastErrorTests
{
    private static string Digits(int count) => new('9', count);

    private static (string, string)[] InvalidLexicalForms() =>
        [
            ("'abc' cast as xs:integer", "FORG0001"),
            ("'1.5' cast as xs:integer", "FORG0001"),
            ("'1e3' cast as xs:integer", "FORG0001"),
            ("'' cast as xs:integer", "FORG0001"),
            ("'1 2' cast as xs:integer", "FORG0001"),
            ("'abc' cast as xs:decimal", "FORG0001"),
            ("'' cast as xs:decimal", "FORG0001"),
            ("'1,5' cast as xs:decimal", "FORG0001"),
            ("'1.5.2' cast as xs:decimal", "FORG0001"),
            ("'1e3' cast as xs:decimal", "FORG0001"),
            ("'abc' cast as xs:double", "FORG0001"),
            ("'+INF' cast as xs:double", "FORG0001"),
            ("'inf' cast as xs:double", "FORG0001"),
            ("'1e' cast as xs:double", "FORG0001"),
            ("'abc' cast as xs:float", "FORG0001"),
            ("'nan' cast as xs:float", "FORG0001"),
            ("'yes' cast as xs:boolean", "FORG0001"),
            ("'TRUE' cast as xs:boolean", "FORG0001"),
            ("'' cast as xs:boolean", "FORG0001"),
            ("xs:untypedAtomic('x') cast as xs:float", "FORG0001"),
            ("xs:untypedAtomic('x') cast as xs:decimal", "FORG0001"),
        ];

    // FOCA0002: NaN and the infinities have no value as a decimal or an integer.
    private static (string, string)[] NotANumberCases() =>
        [
            ("xs:double('NaN') cast as xs:decimal", "FOCA0002"),
            ("xs:double('INF') cast as xs:decimal", "FOCA0002"),
            ("xs:double('-INF') cast as xs:decimal", "FOCA0002"),
            ("xs:float('NaN') cast as xs:decimal", "FOCA0002"),
            ("xs:float('INF') cast as xs:decimal", "FOCA0002"),
            ("xs:double('NaN') cast as xs:integer", "FOCA0002"),
            ("xs:double('INF') cast as xs:integer", "FOCA0002"),
            ("xs:double('-INF') cast as xs:integer", "FOCA0002"),
            ("xs:float('NaN') cast as xs:integer", "FOCA0002"),
            ("xs:float('-INF') cast as xs:integer", "FOCA0002"),
        ];

    // FOCA0003: the integer does not fit 64 bits; FOCA0001: the lexical form of a decimal exceeds the digit cap of the datatype layer.
    private static (string, string)[] ImplementationLimits() =>
        [
            ("'9223372036854775808' cast as xs:integer", "FOCA0003"),
            ("'-9223372036854775809' cast as xs:integer", "FOCA0003"),
            ("xs:untypedAtomic('9223372036854775808') cast as xs:integer", "FOCA0003"),
            ("xs:decimal('9223372036854775808') cast as xs:integer", "FOCA0003"),
            ("xs:decimal('-9223372036854775809') cast as xs:integer", "FOCA0003"),
            ("xs:decimal('1000000000000000000000') cast as xs:integer", "FOCA0003"),
            ("xs:double('1e30') cast as xs:integer", "FOCA0003"),
            ("xs:double('9.223372036854775808E18') cast as xs:integer", "FOCA0003"),
            ("xs:double('-1e30') cast as xs:integer", "FOCA0003"),
            ("xs:float('1e20') cast as xs:integer", "FOCA0003"),
            ("'" + Digits(5000) + "' cast as xs:integer", "FOCA0003"),
            ("'" + Digits(5000) + "' cast as xs:decimal", "FOCA0001"),
            ("'0." + Digits(5000) + "' cast as xs:decimal", "FOCA0001"),
            ("'" + Digits(5000) + "' cast as xs:double", "FOCA0001"),
            ("'" + Digits(5000) + "' cast as xs:float", "FOCA0001"),
        ];

    // XPTY0004: a sequence of more than one item, or none without the optional marker.
    private static (string, string)[] WrongCardinality() =>
        [
            ("() cast as xs:integer", "XPTY0004"),
            ("(1, 2) cast as xs:integer", "XPTY0004"),
            ("('1', '2') cast as xs:string?", "XPTY0004"),
            ("() cast as xs:string", "XPTY0004"),
        ];

    /// <summary>Every dynamic failure of a cast among the seven types and the code it raises; the castable tests derive their expressions from it.</summary>
    public static IEnumerable<Func<(string Expression, string Code)>> FailingCasts()
    {
        foreach (
            var group in (Func<(string, string)[]>[])
                [InvalidLexicalForms, NotANumberCases, ImplementationLimits, WrongCardinality]
        )
        {
            foreach (var entry in group())
            {
                var captured = entry;
                yield return () => captured;
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(FailingCasts))]
    public async Task Cast_Failure_RaisesItsCode(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    [MethodDataSource(nameof(FailingCasts))]
    public async Task Castable_EveryFailingCast_ReturnsFalseAndRaisesNothing(string expression, string code)
    {
        var castable = AsCastable(expression);

        await Assert.That(Code(castable)).IsEqualTo(string.Empty).Because($"{code} must not escape castable");
        await Assert.That(EvaluateRendered(castable)).IsEqualTo("boolean:false");
    }

    [Test]
    [Arguments("'abc' castable as xs:string", true)]
    [Arguments("'12' castable as xs:integer", true)]
    [Arguments("'1.5' castable as xs:decimal", true)]
    [Arguments("' 1.5e3 ' castable as xs:double", true)]
    [Arguments("'true' castable as xs:boolean", true)]
    [Arguments("xs:double('1.5') castable as xs:integer", true)]
    [Arguments("xs:double('9.2233720368547748E18') castable as xs:integer", true)]
    [Arguments("xs:decimal('9223372036854775807') castable as xs:integer", true)]
    [Arguments("() castable as xs:integer?", true)]
    [Arguments("() castable as xs:integer", false)]
    [Arguments("(1, 2) castable as xs:integer?", false)]
    [Arguments("'1' castable as xs:integer?", true)]
    public async Task Castable_ReturnsWhetherTheCastSucceeds(string expression, bool expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected ? "boolean:true" : "boolean:false");

    // The arithmetic operators cast an untyped operand through the same casting table, so a lexical form beyond the digit cap is an
    // implementation limit for them too (#621), not an invalid form; the comparisons and the aggregates have their tests beside them.
    [Test]
    [Arguments("xs:untypedAtomic('", "') + 1", "FOCA0001", 5000)]
    [Arguments("1 + xs:untypedAtomic('", "')", "FOCA0001", 5000)]
    [Arguments("xs:untypedAtomic('", "') + 1", "FORG0001", 0)]
    public async Task UntypedOperandBeyondTheDigitCap_RaisesTheCodeOfTheCastingTable(
        string before,
        string after,
        string code,
        int digits
    ) => await Assert.That(Code(before + (digits == 0 ? "abc" : Digits(digits)) + after)).IsEqualTo(code);

    [Test]
    public async Task Castable_OnTheLimit_Succeeds()
    {
        // The digit cap is 4096; 4000 digits are within it.
        await Assert.That(EvaluateRendered("'" + Digits(4000) + "' castable as xs:decimal")).IsEqualTo("boolean:true");
        await Assert.That(Code("'" + Digits(4000) + "' cast as xs:decimal")).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("'x' cast as xs:long", "XPST0051")]
    [Arguments("'x' cast as xs:int", "XPST0051")]
    [Arguments("'x' cast as xs:token", "XPST0051")]
    [Arguments("'x' cast as xs:anyType", "XPST0051")]
    [Arguments("'x' cast as xs:anySimpleType", "XPST0051")]
    [Arguments("'x' cast as xs:IDREFS", "XPST0051")]
    [Arguments("'x' cast as xs:nothing", "XPST0051")]
    [Arguments("'x' cast as p:integer", "XPST0051")]
    [Arguments("'x' cast as integer", "XPST0051")]
    [Arguments("'x' castable as xs:long", "XPST0051")]
    [Arguments("'x' castable as xs:nothing", "XPST0051")]
    [Arguments("'x' cast as xs:anyAtomicType", "XPST0080")]
    [Arguments("'x' cast as xs:NOTATION", "XPST0080")]
    [Arguments("'x' castable as xs:anyAtomicType", "XPST0080")]
    [Arguments("'x' castable as xs:NOTATION?", "XPST0080")]
    [Arguments("'x' cast as zz:integer", "XPST0081")]
    [Arguments("'x' castable as zz:integer", "XPST0081")]
    public async Task StaticError_IsRaisedWhileCompiling(string expression, string code)
    {
        var compiled = Compile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo(code);
    }

    [Test]
    public async Task StaticError_CarriesTheLocationOfTheTypeName()
    {
        const string Expression = "'x' cast as xs:long";

        _ = Compile(Expression, out var diagnostic);

        await Assert.That(diagnostic.Offset).IsEqualTo(Expression.IndexOf("xs:long", StringComparison.Ordinal));
    }

    [Test]
    public async Task Operand_WithAnOperandError_IsNotSwallowedByCastable()
    {
        // The operand raises XPST0017 while compiling: castable never turns a static error into false.
        var compiled = Compile("p:nothing(1) castable as xs:integer", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0017");
    }

    // Sources outside the seven types that the specification does not allow to cast to a numeric or boolean target.
    public static IEnumerable<Func<XdmItem>> UncastableSources()
    {
        var versions = BuiltInDatatypes.For(XsdVersion.V11);
        var dateTime = (IDatatypeDescriptor<XsdDateTime>)versions.Get("dateTime");
        var date = (IDatatypeDescriptor<XsdDateTime>)versions.Get("date");
        var time = (IDatatypeDescriptor<XsdDateTime>)versions.Get("time");
        var duration = (IDatatypeDescriptor<XsdDuration>)versions.Get("duration");
        var items = new[]
        {
            XdmItem.FromDateTime(dateTime.Parse("2020-01-02T03:04:05Z".AsSpan()).Value),
            XdmItem.FromDateTime(date.Parse("2020-01-02".AsSpan()).Value),
            XdmItem.FromDateTime(time.Parse("03:04:05".AsSpan()).Value),
            XdmItem.FromDuration(duration.Parse("P1Y2M3DT4H".AsSpan()).Value),
            XdmItem.FromAnyUri("urn:x"),
        };

        foreach (var item in items)
        {
            var captured = item;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(UncastableSources))]
    public async Task NotAllowedCell_RaisesXpty0004_AndIsNotCastable(XdmItem source)
    {
        var table = CastingCatalog.Default.Table;
        var location = new SourceSpan(0, 1);
        var targets = new[]
        {
            XdmTypeId.XsFloat,
            XdmTypeId.XsDouble,
            XdmTypeId.XsDecimal,
            XdmTypeId.XsInteger,
            XdmTypeId.Boolean,
        };

        foreach (var target in targets)
        {
            var operand = new ConstantNode(XdmSequence.Create(source));
            var cast = new CastNode(operand, target, allowsEmpty: false, table, location);
            var castable = new CastableNode(operand, target, allowsEmpty: false, table);
            using var context = new XPathEvaluationContext();

            XPathException? failure = null;
            try
            {
                _ = cast.Evaluate(context);
            }
            catch (XPathException exception)
            {
                failure = exception;
            }

            await Assert.That(failure).IsNotNull();
            await Assert.That(failure!.Diagnostic.Code).IsEqualTo("XPTY0004");
            await Assert.That(table.TryCast(source, target, out _)).IsEqualTo(CastStatus.NotAllowed);
            await Assert.That(CastFixture.Render(castable.Evaluate(context)[0])).IsEqualTo("boolean:false");
        }
    }

    // The status is internal, so the public test method takes its number.
    [Test]
    [Arguments((int)CastStatus.InvalidLexical, "FORG0001")]
    [Arguments((int)CastStatus.InvalidValue, "FOCA0002")]
    [Arguments((int)CastStatus.DecimalOverflow, "FOCA0001")]
    [Arguments((int)CastStatus.IntegerOverflow, "FOCA0003")]
    [Arguments((int)CastStatus.NotAllowed, "XPTY0004")]
    public async Task Status_MapsToTheErrorCode(int status, string code) =>
        await Assert.That(((CastStatus)status).ToErrorCode().ToString()).IsEqualTo(code);

    [Test]
    public async Task Status_Ok_HasNoErrorCode() =>
        await Assert.That(() => CastStatus.Ok.ToErrorCode()).Throws<ArgumentOutOfRangeException>();
}
