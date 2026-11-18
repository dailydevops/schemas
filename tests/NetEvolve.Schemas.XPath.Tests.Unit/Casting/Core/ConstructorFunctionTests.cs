namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static CastFixture;

public class ConstructorFunctionTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static readonly string[] SevenNames =
    [
        "string",
        "untypedAtomic",
        "float",
        "double",
        "decimal",
        "integer",
        "boolean",
    ];

    public static IEnumerable<Func<string>> TypeNames()
    {
        foreach (var name in SevenNames)
        {
            var captured = name;
            yield return () => captured;
        }
    }

    // Staging order: the staging of the constructors reads the casting table when the function catalog freezes, so the casting table is
    // frozen from then on, and a package that stages rows later is refused instead of being missed silently.
    [Test]
    public async Task DefaultCastingCatalog_StagedAfterTheFunctionCatalogFroze_Throws()
    {
        _ = FunctionCatalog.Default.Registry;

        await Assert.That(() => CastingCatalog.Default.Stage(static _ => { })).Throws<InvalidOperationException>();
    }

    [Test]
    [MethodDataSource(nameof(TypeNames))]
    public async Task DefaultCatalog_HoldsTheConstructor_ForEveryVersion(string name)
    {
        var registry = FunctionCatalog.Default.Registry;
        var qname = new XPathQName(Xs, name);

        foreach (var version in (XPathVersion[])[XPathVersion.V20, XPathVersion.V30, XPathVersion.V31])
        {
            await Assert.That(registry.IsDefinedByCatalog(version, qname, 1)).IsTrue();
        }

        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V20, qname, 0)).IsFalse();
        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V20, qname, 2)).IsFalse();
    }

    [Test]
    public async Task Create_GeneratesOneConstructorPerTargetOfTheTable()
    {
        var functions = ConstructorFunctions.Create(CastingCatalog.Default.Table);
        var names = new List<string>();
        foreach (var function in functions)
        {
            names.Add(function.Name.LocalName);

            await Assert.That(function.Name.NamespaceName).IsEqualTo(Xs);
            await Assert.That(function.MinArity).IsEqualTo(1);
            await Assert.That(function.MaxArity).IsEqualTo(1);
        }

        await Assert.That(names).IsEquivalentTo(SevenNames);
    }

    [Test]
    public async Task Create_FollowsTheTable_ARowForANewTargetAddsItsConstructor()
    {
        var catalog = new CastingCatalog();
        catalog.Stage(CastingRegistration.RegisterRows);
        catalog.Stage(static builder =>
            builder.Add(
                new CastRow(
                    XdmTypeId.XsString,
                    XdmTypeId.Date,
                    CastCell.ValueDependent,
                    static (in XdmItem source, out XdmItem result) =>
                    {
                        result = XdmItem.FromString("a date");
                        return CastStatus.Ok;
                    }
                )
            )
        );
        var functions = new FunctionCatalog();
        functions.Stage(builder =>
        {
            foreach (var function in ConstructorFunctions.Create(catalog.Table))
            {
                builder.Add(function);
            }
        });

        var result = Evaluate("xs:date('x')", catalog.Table, functions.Registry);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(Render(result[0])).IsEqualTo("string:a date");
        await Assert.That(Code("xs:date('x')")).IsEqualTo("XPST0017");
    }

    [Test]
    [Arguments("xs:string(())", "count:0")]
    [Arguments("xs:integer(())", "count:0")]
    [Arguments("xs:boolean(())", "count:0")]
    [Arguments("xs:string(5)", "string:5")]
    [Arguments("xs:integer(' 5 ')", "integer:5")]
    [Arguments("xs:integer(xs:untypedAtomic('5'))", "integer:5")]
    [Arguments("xs:decimal(5)", "decimal:5")]
    [Arguments("xs:float(xs:decimal('0.1'))", "float:0.1")]
    [Arguments("xs:double(xs:float('0.1'))", "double:0.10000000149011612")]
    [Arguments("xs:untypedAtomic(12)", "untyped:12")]
    [Arguments("xs:boolean(2)", "boolean:true")]
    public async Task Constructor_AppliesTheCast_AndMapsTheEmptySequenceToItself(string expression, string expected) =>
        await Assert.That(EvaluateRendered(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:integer((1, 2))", "XPTY0004")]
    [Arguments("xs:string(('a', 'b'))", "XPTY0004")]
    [Arguments("xs:integer('x')", "FORG0001")]
    [Arguments("xs:decimal(xs:double('NaN'))", "FOCA0002")]
    [Arguments("xs:integer('9223372036854775808')", "FOCA0003")]
    [Arguments("xs:integer()", "XPST0017")]
    [Arguments("xs:integer(1, 2)", "XPST0017")]
    [Arguments("xs:date('2020-01-01')", "XPST0017")]
    public async Task Constructor_Error_RaisesItsCode(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);
}
