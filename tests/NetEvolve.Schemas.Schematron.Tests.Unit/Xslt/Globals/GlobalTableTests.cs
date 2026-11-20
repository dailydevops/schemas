namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals.GlobalFixture;

/// <summary>
/// The global declarations of a schema by name (XSLT 2.0 section 3.10 and 9.5, design #150 EP-14): the declaration of the highest
/// import precedence wins, two of one name at the same precedence are the static error XTSE0630, a variable and a parameter share
/// the names, and a required parameter that the caller did not supply is a build-time error.
/// </summary>
public class GlobalTableTests
{
    private static Dictionary<string, string> Supplied(params (string Name, string Value)[] values)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in values)
        {
            map[name] = value;
        }

        return map;
    }

    [Test]
    public async Task Build_KeepsTheDeclarationsInDeclarationOrder()
    {
        var fixture = new GlobalFixture();

        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "b"), A("select", "1")),
            X("param", 11, A("name", "a"), A("select", "2")),
            X("variable", 12, A("name", "c"), A("select", "3"))
        );

        await Assert.That(table.Count).IsEqualTo(3);
        await Assert.That(table.Declarations[0].Name).IsEqualTo("b");
        await Assert.That(table.Declarations[1].Name).IsEqualTo("a");
        await Assert.That(table.Declarations[2].Name).IsEqualTo("c");
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryGet_FindsADeclarationByExpandedName()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "g"), A("select", "1")),
            X("variable", 11, A("name", "f:g"), A("select", "2"))
        );

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.TryGet(new XPathQName(string.Empty, "g"), out var plain)).IsTrue();
        await Assert.That(plain!.Value).IsEqualTo("1");
        await Assert.That(table.TryGet(new XPathQName(Functions, "g"), out var qualified)).IsTrue();
        await Assert.That(qualified!.Value).IsEqualTo("2");
        await Assert.That(table.TryGet(new XPathQName(string.Empty, "other"), out var missing)).IsFalse();
        await Assert.That(missing).IsNull();
    }

    [Test]
    public async Task TryAdd_TwoDeclarationsOfOneNameAtTheSamePrecedence_IsXtse0630WithBothLocations()
    {
        var fixture = new GlobalFixture();

        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "g"), A("select", "1")),
            X("variable", 20, A("name", "g"), A("select", "2"))
        );

        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200/XTSE0630");
        var problem = fixture.Problems.Problems[0];
        await Assert.That(problem.Location.Line).IsEqualTo(20);
        await Assert.That(problem.Message).Contains("'g'");
        await Assert.That(problem.Message).Contains("line 10");
        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(table.Declarations[0].Value).IsEqualTo("1");
    }

    [Test]
    public async Task TryAdd_AVariableAndAParameterOfOneNameAtTheSamePrecedence_IsXtse0630()
    {
        var fixture = new GlobalFixture();

        _ = fixture.Table(
            0,
            X("variable", 10, A("name", "g"), A("select", "1")),
            X("param", 20, A("name", "g"), A("select", "2"))
        );

        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200/XTSE0630");
    }

    [Test]
    public async Task TryAdd_TheModuleOfTheOtherDeclarationIsNamedInTheMessage()
    {
        var fixture = new GlobalFixture();
        var builder = new XsltGlobalTableBuilder();
        var first = new XsltGlobalDeclaration(
            new XPathQName(string.Empty, "g"),
            new NetEvolve.Schemas.Schematron.Scopes.GlobalDeclaration(
                "g",
                "1",
                null,
                false,
                false,
                new DocumentLocation("file:///a.xsl", 3, 5)
            )
        );
        var second = new XsltGlobalDeclaration(
            new XPathQName(string.Empty, "g"),
            new NetEvolve.Schemas.Schematron.Scopes.GlobalDeclaration(
                "g",
                "2",
                null,
                false,
                false,
                new DocumentLocation("file:///b.xsl", 7, 9)
            )
        );

        _ = builder.TryAdd(first, 2, out _);
        var added = builder.TryAdd(second, 2, out var problem);

        await Assert.That(added).IsFalse();
        await Assert.That(problem.Message).Contains("file:///a.xsl");
        await Assert.That(problem.Message).Contains("line 3");
        await Assert.That(problem.Location.Source).IsEqualTo("file:///b.xsl");
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TryAdd_AHigherPrecedenceReplacesTheLowerOneInPlace_AndALowerOneIsIgnored()
    {
        var fixture = new GlobalFixture();
        var nodes = Declarations(
            X("variable", 10, A("name", "g"), A("select", "'low'")),
            X("variable", 11, A("name", "h"), A("select", "'other'")),
            X("variable", 12, A("name", "g"), A("select", "'high'")),
            X("variable", 13, A("name", "g"), A("select", "'lower than high'"))
        );
        var builder = new XsltGlobalTableBuilder();
        var precedences = new[] { 1, 1, 3, 2 };
        for (var i = 0; i < nodes.Count; i++)
        {
            var declaration = (XsltGlobalDeclaration)fixture.Compile(nodes[i])!;
            await Assert.That(builder.TryAdd(declaration, precedences[i], out _)).IsTrue();
        }

        var table = builder.Build();

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.Declarations[0].Value).IsEqualTo("'high'");
        await Assert.That(table.Declarations[1].Value).IsEqualTo("'other'");
    }

    [Test]
    public async Task Build_CopiesWhatWasAdded_SoALaterAddDoesNotChangeTheTable()
    {
        var fixture = new GlobalFixture();
        var builder = new XsltGlobalTableBuilder();
        var node = Declarations(
            X("variable", 10, A("name", "g"), A("select", "1")),
            X("variable", 11, A("name", "h"), A("select", "2"))
        );
        _ = builder.TryAdd((XsltGlobalDeclaration)fixture.Compile(node[0])!, 0, out _);
        var table = builder.Build();

        _ = builder.TryAdd((XsltGlobalDeclaration)fixture.Compile(node[1])!, 0, out _);

        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(builder.Build().Count).IsEqualTo(2);
    }

    [Test]
    public async Task CheckParameters_ARequiredParameterWithoutAValue_IsNes3111WithXtde0050AtTheParam()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("param", 10, A("name", "p"), A("required", "yes")));
        var problems = new FakeProblemSink();

        var ok = table.CheckParameters(Supplied(), problems);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(problems)).IsEqualTo("NES3111/XTDE0050");
        await Assert.That(problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(problems.Problems[0].Message).Contains("'p'");
    }

    [Test]
    public async Task CheckParameters_ATypedParameterWithoutADefault_IsRequiredToo()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("param", 10, A("name", "p"), A("as", "xs:string")));
        var problems = new FakeProblemSink();

        await Assert.That(table.CheckParameters(Supplied(), problems)).IsFalse();
        await Assert.That(Codes(problems)).IsEqualTo("NES3111/XTDE0050");
    }

    [Test]
    public async Task CheckParameters_EveryMissingParameterIsReportedOnce()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("param", 10, A("name", "a"), A("required", "yes")),
            X("param", 11, A("name", "b"), A("required", "yes")),
            X("param", 12, A("name", "c"), A("required", "yes"))
        );
        var problems = new FakeProblemSink();

        var ok = table.CheckParameters(Supplied(("b", "x")), problems);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(2);
        await Assert.That(problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(problems.Problems[1].Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task CheckParameters_ARequiredParameterWithAValue_AnOptionalOneWithout_AndVariables_AreFine()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("param", 10, A("name", "p"), A("required", "yes")),
            X("param", 11, A("name", "o"), A("select", "1")),
            X("variable", 12, A("name", "v"), A("select", "1"))
        );
        var problems = new FakeProblemSink();

        var ok = table.CheckParameters(Supplied(("p", "given")), problems);

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CheckParameters_AValueForAVariable_DoesNotSatisfyARequiredParameterOfAnotherName()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("param", 10, A("name", "p"), A("required", "yes")),
            X("variable", 11, A("name", "v"), A("select", "1"))
        );
        var problems = new FakeProblemSink();

        var ok = table.CheckParameters(Supplied(("v", "given")), problems);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Guards_RejectNullArguments()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0);

        await Assert.That(() => new XsltGlobalTableBuilder().TryAdd(null!, 0, out _)).Throws<ArgumentNullException>();
        await Assert.That(() => table.CheckParameters(null!, new FakeProblemSink())).Throws<ArgumentNullException>();
        await Assert.That(() => table.CheckParameters(Supplied(), null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new NetEvolve.Schemas.Schematron.Scopes.GlobalDeclaration(null!, null, null, false, false, default)
            )
            .Throws<ArgumentNullException>();
    }
}
