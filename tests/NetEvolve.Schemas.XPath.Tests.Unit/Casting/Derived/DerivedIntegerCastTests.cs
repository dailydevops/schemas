namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using System;
using System.Collections.Generic;
using static DerivedFixture;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core.CastFixture;

/// <summary>
/// Casting to the derived integer types (Functions and Operators, 17.2 to 17.5): the value is cast to <c>xs:integer</c> and then checked
/// against the bounds of the target. For every type the tests assert the lower bound, the upper bound, and the value outside each bound
/// (FORG0001), through a string, a decimal, and the constructor function.
/// </summary>
public class DerivedIntegerCastTests
{
    public static IEnumerable<Func<(string Type, string Value, bool Inside)>> EveryBound() => Bounds();

    [Test]
    [MethodDataSource(nameof(EveryBound))]
    public async Task FromString_ChecksTheBounds(string type, string value, bool inside) =>
        await AssertCast($"'{value}' cast as xs:{type}", type, value, inside);

    [Test]
    [MethodDataSource(nameof(EveryBound))]
    public async Task FromDecimal_ChecksTheBounds(string type, string value, bool inside) =>
        await AssertCast($"xs:decimal('{value}') cast as xs:{type}", type, value, inside);

    [Test]
    [MethodDataSource(nameof(EveryBound))]
    public async Task Constructor_ChecksTheBounds(string type, string value, bool inside) =>
        await AssertCast($"xs:{type}('{value}')", type, value, inside);

    [Test]
    [MethodDataSource(nameof(EveryBound))]
    public async Task Castable_IsTrueInsideTheBoundsAndFalseOutside(string type, string value, bool inside) =>
        await Assert.That(Castable($"'{value}'", type)).IsEqualTo(inside);

    [Test]
    [Arguments("' 12 ' cast as xs:short", "short:12")]
    [Arguments("'+5' cast as xs:byte", "byte:5")]
    [Arguments("'-0' cast as xs:unsignedByte", "unsignedByte:0")]
    [Arguments("'-00' cast as xs:unsignedLong", "unsignedLong:0")]
    [Arguments("'+00' cast as xs:nonPositiveInteger", "nonPositiveInteger:0")]
    [Arguments("'007' cast as xs:int", "int:7")]
    [Arguments("xs:untypedAtomic('42') cast as xs:long", "long:42")]
    [Arguments("xs:double('1e3') cast as xs:short", "short:1000")]
    [Arguments("xs:double('1.9') cast as xs:byte", "byte:1")]
    [Arguments("xs:double('-1.9') cast as xs:byte", "byte:-1")]
    [Arguments("xs:float('10') cast as xs:positiveInteger", "positiveInteger:10")]
    [Arguments("xs:decimal('-0.5') cast as xs:nonNegativeInteger", "nonNegativeInteger:0")]
    [Arguments("xs:decimal('127.99') cast as xs:byte", "byte:127")]
    [Arguments("xs:integer(5) cast as xs:short", "short:5")]
    [Arguments("xs:boolean('true') cast as xs:positiveInteger", "positiveInteger:1")]
    [Arguments("xs:boolean('false') cast as xs:nonNegativeInteger", "nonNegativeInteger:0")]
    [Arguments("xs:double('1e19') cast as xs:unsignedLong", "unsignedLong:10000000000000000000")]
    public async Task FromAnotherSource_ProducesTheValueWithTheDerivedAnnotation(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("'1.5' cast as xs:short")]
    [Arguments("'abc' cast as xs:byte")]
    [Arguments("'' cast as xs:int")]
    [Arguments("'1e3' cast as xs:long")]
    [Arguments("xs:boolean('false') cast as xs:positiveInteger")]
    [Arguments("xs:boolean('true') cast as xs:negativeInteger")]
    [Arguments("xs:double('1e5') cast as xs:short")]
    [Arguments("xs:double('-1e5') cast as xs:short")]
    [Arguments("xs:decimal('128') cast as xs:byte")]
    public async Task FromAnotherSource_OutsideTheTypeOrNotAnInteger_RaisesForg0001(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0001");

    [Test]
    [Arguments("xs:double('NaN') cast as xs:byte", "FOCA0002")]
    [Arguments("xs:double('INF') cast as xs:unsignedLong", "FOCA0002")]
    [Arguments("xs:float('-INF') cast as xs:long", "FOCA0002")]
    [Arguments("xs:date('2000-01-01') cast as xs:byte", "XPTY0004")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00') cast as xs:positiveInteger", "XPTY0004")]
    [Arguments("xs:hexBinary('0A') cast as xs:long", "XPTY0004")]
    [Arguments("(1, 2) cast as xs:byte", "XPTY0004")]
    [Arguments("() cast as xs:byte", "XPTY0004")]
    public async Task OtherFailures_KeepTheCodeOfTheCastToInteger(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    // Decision: a number beyond the 64 bits of the integer item goes to the bounds of the derived type, so it is FORG0001 for a bounded
    // type and a value for an unbounded one; the cast to xs:integer itself still raises FOCA0003 there (#265, D3).
    [Test]
    [Arguments("xs:double('1e30') cast as xs:long", "FORG0001")]
    [Arguments("xs:double('1e30') cast as xs:unsignedLong", "FORG0001")]
    [Arguments("xs:double('-1e30') cast as xs:nonPositiveInteger", "")]
    [Arguments("xs:double('1e30') cast as xs:nonNegativeInteger", "")]
    [Arguments("xs:double('1e30') cast as xs:integer", "FOCA0003")]
    public async Task BeyondTheIntegerItem_GoesToTheBoundsOfTheDerivedType(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    public async Task BeyondTheIntegerItem_UnboundedType_KeepsTheExactValue() =>
        await Assert
            .That(Show("xs:double('1e30') cast as xs:positiveInteger"))
            .IsEqualTo("positiveInteger:1000000000000000019884624838656");

    // The digit cap of the datatype layer is an implementation limit for the derived types as for xs:integer (#265, D5).
    [Test]
    public async Task LexicalFormBeyondTheDigitCap_IsAnImplementationLimit()
    {
        var beyond = new string('1', 5000);

        await Assert.That(Code($"'{beyond}' cast as xs:nonNegativeInteger")).IsEqualTo("FOCA0003");
        await Assert.That(Code($"'{beyond}' cast as xs:integer")).IsEqualTo("FOCA0003");
        await Assert.That(Castable($"'{beyond}'", "nonNegativeInteger")).IsFalse();
    }

    [Test]
    public async Task EveryDerivedIntegerType_HasAConstructorFunctionForTheEmptySequence()
    {
        foreach (var (name, _, _) in IntegerTypes)
        {
            await Assert.That(Evaluate($"xs:{name}(())").Count).IsEqualTo(0).Because(name);
        }
    }

    private static async Task AssertCast(string expression, string type, string value, bool inside)
    {
        if (inside)
        {
            await Assert.That(Show(expression)).IsEqualTo($"{type}:{value}");
        }
        else
        {
            await Assert.That(Code(expression)).IsEqualTo("FORG0001");
        }
    }
}
