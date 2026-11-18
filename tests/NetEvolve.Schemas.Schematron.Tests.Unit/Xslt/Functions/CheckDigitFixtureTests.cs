namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// The check functions of real rule sets (the GLN check and a modulus 11 check) written as XSLT fixtures: control flow in the
/// stylesheet text (recursion, an accumulator parameter, a local variable, arity overloads, a conditional result), only primitive
/// operations in the mini query language. Limitation: they run over <see cref="MiniQuery"/>, not over XPath, until XP-19 (#297).
/// </summary>
public class CheckDigitFixtureTests
{
    // GS1 GLN: thirteen digits, the first twelve weighted 1 and 3 from the left, the check digit is (10 - sum mod 10) mod 10.
    private static readonly XmlElem[] GlnFunctions =
    [
        Function("f:gln-sum", "xs:integer", 10, Param("s", "xs:string", 11), Sequence("f:gln-sum($s, 1)", 12)),
        Function(
            "f:gln-sum",
            "xs:integer",
            20,
            Param("s", "xs:string", 21),
            Param("i", "xs:integer", 22),
            Sequence(
                "if ($i > 12) then 0 else xs:integer(substring($s, $i, 1)) * (if ($i mod 2 = 1) then 1 else 3) + f:gln-sum($s, $i + 1)",
                23
            )
        ),
        Function(
            "f:is-gln",
            "xs:boolean",
            30,
            Param("gln", "xs:string", 31),
            Variable("digits", "string-length($gln) = 13 and translate($gln, '0123456789', '') = ''", null, 32),
            Sequence(
                "if ($digits) then (10 - f:gln-sum($gln) mod 10) mod 10 = xs:integer(substring($gln, 13, 1)) else false()",
                33
            )
        ),
    ];

    // A modulus 11 check digit: the payload weighted 2 to 7 from the right, the last digit is (11 - sum mod 11) mod 11; 10 is invalid.
    private static readonly XmlElem[] Mod11Functions =
    [
        Function(
            "f:mod11-sum",
            "xs:integer",
            10,
            Param("s", "xs:string", 11),
            Param("pos", "xs:integer", 12),
            Param("w", "xs:integer", 13),
            Sequence(
                "if ($pos < 1) then 0 else xs:integer(substring($s, $pos, 1)) * $w + f:mod11-sum($s, $pos - 1, if ($w = 7) then 2 else $w + 1)",
                14
            )
        ),
        Function(
            "f:mod11-valid",
            "xs:boolean",
            20,
            Param("s", "xs:string", 21),
            Variable("n", "string-length($s)", "xs:integer", 22),
            Variable("numeric", "$n >= 2 and translate($s, '0123456789', '') = ''", null, 23),
            Sequence(
                "if ($numeric) then (11 - f:mod11-sum($s, $n - 1, 2) mod 11) mod 11 = xs:integer(substring($s, $n, 1)) else false()",
                24
            )
        ),
    ];

    private static async Task<bool> Check(FunctionFixture fixture, string name, string value)
    {
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, name), 1, out var function);

        var status = caller.TryCall(function, [Str(value)], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetBoolean(out var valid);
        return valid;
    }

    [Test]
    [Arguments("4012345678901", true)]
    [Arguments("0000000000000", true)]
    [Arguments("4000000000006", true)]
    [Arguments("4012345678902", false)]
    [Arguments("4012345678900", false)]
    [Arguments("401234567890", false)]
    [Arguments("40123456789012", false)]
    [Arguments("40123456789O1", false)]
    [Arguments("", false)]
    public async Task Gln_ValidAndInvalidNumbers_ReturnTheExpectedResults(string number, bool expected)
    {
        var fixture = Create();
        _ = fixture.CompileAll(GlnFunctions);

        var valid = await Check(fixture, "is-gln", number);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(valid).IsEqualTo(expected);
    }

    [Test]
    [Arguments("123455", true)]
    [Arguments("00", true)]
    [Arguments("123454", false)]
    [Arguments("12345A", false)]
    [Arguments("5", false)]
    [Arguments("", false)]
    [Arguments("100", false)]
    public async Task Mod11_ValidAndInvalidNumbers_ReturnTheExpectedResults(string number, bool expected)
    {
        var fixture = Create();
        _ = fixture.CompileAll(Mod11Functions);

        var valid = await Check(fixture, "mod11-valid", number);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(valid).IsEqualTo(expected);
    }

    [Test]
    public async Task Gln_ArityOverloads_OfTheSameFunctionAreBothCallable()
    {
        var fixture = Create();
        _ = fixture.CompileAll(GlnFunctions);
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "gln-sum"), 1, out var one);
        _ = fixture.Table.TryGet(new XPathQName(Functions, "gln-sum"), 2, out var two);

        _ = caller.TryCall(one, [Str("4012345678901")], out var whole);
        _ = caller.TryCall(two, [Str("4012345678901"), Int(12)], out var tail);

        await Assert.That(MiniQuery.Integer(whole)).IsEqualTo(89);
        await Assert.That(MiniQuery.Integer(tail)).IsEqualTo(0);
    }

    [Test]
    public async Task Gln_AsTypedParameter_RejectsANumber()
    {
        var fixture = Create();
        _ = fixture.CompileAll(GlnFunctions);
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "is-gln"), 1, out var function);

        var status = caller.TryCall(function, [Int(4012345678901)], out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(caller.GetProblem().Constraint).IsEqualTo("XPTY0004");
    }
}
