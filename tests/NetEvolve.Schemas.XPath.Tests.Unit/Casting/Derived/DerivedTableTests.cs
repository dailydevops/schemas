namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static DerivedFixture;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core.CastFixture;

/// <summary>The derived types in the casting table: each is a target with a constructor function, and the table follows the primitive ancestors (Functions and Operators, 17.2 to 17.5).</summary>
public class DerivedTableTests
{
    public static IEnumerable<Func<string>> EveryDerivedType()
    {
        foreach (var (name, _, _) in IntegerTypes)
        {
            var captured = name;
            yield return () => captured;
        }

        foreach (var name in StringTypes)
        {
            var captured = name;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(EveryDerivedType))]
    public async Task DerivedType_IsATargetWithAConstructorFunction(string name)
    {
        var table = CastingCatalog.Default.Table;
        var type = XdmTypes.Get(name);

        await Assert.That(table.IsTarget(type)).IsTrue();
        await Assert.That(table.Targets.Contains(type)).IsTrue();
        await Assert.That(Evaluate($"xs:{name}(())").Count).IsEqualTo(0);
        await Assert.That(Code($"'x' castable as xs:{name}")).IsEqualTo(string.Empty);
    }

    [Test]
    [MethodDataSource(nameof(EveryDerivedType))]
    public async Task DerivedType_AsASource_IsAllowedExactlyWhereItsPrimitiveAncestorIs(string name)
    {
        var table = CastingCatalog.Default.Table;
        var type = XdmTypes.Get(name);
        var primitive = Primitive(type);

        // The cell of a derived source follows the primitive ancestor: a derived type is allowed everywhere its ancestor is.
        foreach (var target in table.Targets)
        {
            await Assert
                .That(table.GetCell(type, target) == CastCell.NotAllowed)
                .IsEqualTo(table.GetCell(primitive, Primitive(target)) == CastCell.NotAllowed)
                .Because($"{name} to {XdmTypes.NameOf(target)}");
        }
    }

    // The rule of Functions and Operators 17.2 to 17.5 as one statement over the whole table: a cast from S to T is allowed exactly when the
    // cast from the primitive ancestor of S to the primitive ancestor of T is.
    [Test]
    public async Task EveryPair_IsAllowedExactlyWhenThePairOfPrimitiveAncestorsIs()
    {
        var table = CastingCatalog.Default.Table;
        var mismatches = new StringBuilder();
        foreach (var source in table.Targets)
        {
            foreach (var target in table.Targets)
            {
                var allowed = table.GetCell(source, target) != CastCell.NotAllowed;
                var expected = table.GetCell(Primitive(source), Primitive(target)) != CastCell.NotAllowed;
                if (allowed != expected)
                {
                    _ = mismatches
                        .Append(XdmTypes.NameOf(source))
                        .Append("->")
                        .Append(XdmTypes.NameOf(target))
                        .Append(' ');
                }
            }
        }

        await Assert.That(mismatches.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("'x' cast as xs:NMTOKENS", "XPST0051")]
    [Arguments("'x' cast as xs:ENTITIES", "XPST0051")]
    [Arguments("'x' cast as xs:IDREFS", "XPST0051")]
    [Arguments("xs:NMTOKENS('a')", "XPST0017")]
    [Arguments("xs:IDREFS('a')", "XPST0017")]
    public async Task ListTypes_AreNoAtomicTargets(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    public async Task Result_OfADerivedCast_HasTheDerivedTypeAsItsAnnotation()
    {
        await Assert.That(Evaluate("xs:byte('1')")[0].TypeId).IsEqualTo(XdmTypes.Get("byte"));
        await Assert.That(Evaluate("'1' cast as xs:unsignedLong")[0].TypeId).IsEqualTo(XdmTypes.Get("unsignedLong"));
        await Assert.That(Evaluate("xs:token(' a ')")[0].TypeId).IsEqualTo(XdmTypes.Get("token"));
        await Assert.That(Evaluate("xs:byte('1') cast as xs:integer")[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(Evaluate("xs:token('a') cast as xs:string")[0].TypeId).IsEqualTo(XdmTypeId.XsString);
    }

    [Test]
    public async Task IntegerBeyondTheIntegerItem_KeepsTheKindAndTheDerivedAnnotation()
    {
        var item = Evaluate("xs:unsignedLong('18446744073709551615')")[0];

        await Assert.That(item.TypeId).IsEqualTo(XdmTypes.Get("unsignedLong"));
        await Assert.That(item.TryGetBigInteger(out var value)).IsTrue();
        await Assert.That(value.ToString(CultureInfo.InvariantCulture)).IsEqualTo("18446744073709551615");
    }

    private static XdmTypeId Primitive(XdmTypeId type) =>
        type == XdmTypeId.XsInteger || type == XdmTypeId.XsString ? type
        : XdmTypes.IsDerivedFrom(type, XdmTypeId.XsInteger) ? XdmTypeId.XsInteger
        : XdmTypes.IsDerivedFrom(type, XdmTypeId.XsString) ? XdmTypeId.XsString
        : type;
}
