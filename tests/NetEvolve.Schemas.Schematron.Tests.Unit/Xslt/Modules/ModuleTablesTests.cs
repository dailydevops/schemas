namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Modules;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Modules.ModuleHarness;

/// <summary>
/// The declarations of the linked modules merged into the function, global, and key tables with their import precedence (XSLT 2.0
/// sections 3.10.2, 9.5, 10.3, and 16.3): a function of a loaded module is callable, a higher precedence wins, the same precedence
/// is a static error that names both modules, and a declaration that no table takes is an explicit error with the module URI.
/// </summary>
public class ModuleTablesTests
{
    private const string LibraryModule = "file:///m/lib.xsl";

    private static ModuleHarness Linked(string principal, params (string Uri, string Xml)[] modules)
    {
        var harness = new ModuleHarness();
        foreach (var (uri, xml) in modules)
        {
            _ = harness.Files.Add(uri, xml);
        }

        _ = harness.Link(principal);
        return harness;
    }

    [Test]
    public async Task Tables_FunctionOfAModuleLoadedThroughTheResolver_IsCallable()
    {
        var harness = Linked(Sheet(Import("lib.xsl")), (LibraryModule, Sheet(Fn("double", "$a * 2"))));

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(harness.Tables.Functions.Count).IsEqualTo(1);
        await Assert.That(harness.Call("double", 21)).IsEqualTo(42);
    }

    [Test]
    public async Task Tables_FunctionInTheImportingModule_OverridesTheImportedOneWithTheSameNameAndArity()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), Fn("pick", "$a + 1000")),
            (LibraryModule, Sheet(Fn("pick", "$a + 1"), Fn("only", "$a + 7")))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(harness.Tables.Functions.Count).IsEqualTo(2);
        await Assert.That(harness.Call("pick", 1)).IsEqualTo(1001);
        await Assert.That(harness.Call("only", 1)).IsEqualTo(8);
    }

    [Test]
    public async Task Tables_FunctionOfTheHigherLevelWins_AlsoWhenTheLowerOneIsPlacedAfterIt()
    {
        var harness = Linked(
            Sheet(Include("x1.xsl"), Include("x2.xsl")),
            ("file:///m/x1.xsl", Sheet(Fn("g", "$a + 1"))),
            ("file:///m/x2.xsl", Sheet(Import("y.xsl"), Fn("h", "$a + 2"))),
            ("file:///m/y.xsl", Sheet(Fn("g", "$a + 100")))
        );

        var ok = harness.Compile();

        await Assert.That(Describe(harness.Set)).IsEqualTo("f:g@2,f:g@1,f:h@2");
        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Call("g", 0)).IsEqualTo(1);
        await Assert.That(harness.Call("h", 0)).IsEqualTo(2);
    }

    [Test]
    public async Task Tables_TheOverrideAttribute_DoesNotChangeTheImportPrecedence()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), Fn("pick", "$a + 1000", "override=\"no\"")),
            (LibraryModule, Sheet(Fn("pick", "$a + 1")))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Call("pick", 1)).IsEqualTo(1001);
    }

    [Test]
    public async Task Tables_SameNameAndArityAtTheSamePrecedence_IsTheStaticErrorXtse0770NamingBothModules()
    {
        var harness = Linked(Sheet(Include("lib.xsl"), Fn("dup", "$a")), (LibraryModule, Sheet(Fn("dup", "$a + 1"))));

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3200/XTSE0770");
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Location.Source).IsEqualTo(Principal);
        await Assert.That(problem.Location.Line).IsEqualTo(3);
        await Assert.That(problem.Message).Contains("f:dup").And.Contains(LibraryModule).And.Contains(Principal);
        await Assert.That(harness.Call("dup", 1)).IsEqualTo(2);
    }

    [Test]
    public async Task Tables_SameFunctionTwiceInThePrincipalModule_NamesTheSameModuleTwice()
    {
        var harness = Linked(Sheet(Fn("dup", "$a"), Fn("dup", "$a + 1")));

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3200/XTSE0770");
        await Assert.That(harness.Problems.Problems[0].Message).Contains(Principal);
    }

    [Test]
    public async Task Tables_PrincipalModuleWithoutSystemId_IsNamedAsThePrincipalModuleInADuplicate()
    {
        var harness = new ModuleHarness();
        _ = harness.Link(Sheet(Fn("dup", "$a"), Fn("dup", "$a + 1")), systemId: null);

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3200/XTSE0770");
        await Assert.That(harness.Problems.Problems[0].Message).Contains("the principal module");
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsNull();
    }

    [Test]
    public async Task Tables_SharedModuleInADiamond_IsCompiledOnceAndRaisesNoDuplicate()
    {
        var harness = Linked(
            Sheet(Import("a.xsl"), Import("b.xsl")),
            ("file:///m/a.xsl", Sheet(Import("c.xsl"))),
            ("file:///m/b.xsl", Sheet(Import("c.xsl"))),
            ("file:///m/c.xsl", Sheet(Fn("shared", "$a + 5")))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Set.Declarations.Count).IsEqualTo(2);
        await Assert.That(harness.Compiled).IsEqualTo(1);
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(harness.Call("shared", 1)).IsEqualTo(6);
    }

    [Test]
    public async Task Tables_SameModuleWithAFunctionIncludedTwiceAtOneLevel_IsTheStaticErrorXtse0770()
    {
        var harness = Linked(
            Sheet(Include("lib.xsl"), Include("lib.xsl")),
            (LibraryModule, Sheet(Fn("double", "$a * 2")))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Set.Declarations.Count).IsEqualTo(2);
        await Assert.That(harness.Compiled).IsEqualTo(1);
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3200/XTSE0770");
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Message).Contains("f:double").And.Contains(LibraryModule);
        await Assert.That(problem.Location.Source).IsEqualTo(LibraryModule);
    }

    [Test]
    public async Task Tables_SharedModuleWithAKeyInADiamond_AddsTheKeyOnce()
    {
        var harness = Linked(
            Sheet(Import("a.xsl"), Import("b.xsl")),
            ("file:///m/a.xsl", Sheet(Import("c.xsl"))),
            ("file:///m/b.xsl", Sheet(Import("c.xsl"))),
            ("file:///m/c.xsl", Sheet("<xsl:key name=\"k\" match=\"item\" use=\"@code\"/>"))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Set.Declarations.Count).IsEqualTo(2);
        await Assert.That(harness.Tables.Keys.TryGet(new XPathQName(string.Empty, "k"), out var declarations)).IsTrue();
        await Assert.That(declarations.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Tables_TemplateInAModule_IsTheUnsupportedConstructErrorWithTheModuleUriAndLocation()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), Fn("ok", "$a")),
            (LibraryModule, Sheet("<xsl:template match=\"/\"/>"))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(1);
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Message).Contains("xsl:template");
        await Assert.That(problem.Location.Source).IsEqualTo(LibraryModule);
        await Assert.That(problem.Location.Line).IsEqualTo(2);
        await Assert.That(problem.Location.Column).IsGreaterThan(0);
        await Assert.That(harness.Tables.Functions.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Tables_ModeAndOutputInAModule_AreUnsupportedAtTheirOwnLines()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl")),
            (LibraryModule, Sheet("<xsl:output method=\"xml\"/>", Fn("ok", "$a"), "<xsl:mode name=\"m\"/>"))
        );

        _ = harness.Compile();

        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(2);
        await Assert.That(harness.Problems.Problems[0].Message).Contains("xsl:output");
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(harness.Problems.Problems[1].Message).Contains("xsl:mode");
        await Assert.That(harness.Problems.Problems[1].Location.Line).IsEqualTo(4);
        await Assert.That(harness.Problems.Problems[1].Location.Source).IsEqualTo(LibraryModule);
    }

    [Test]
    public async Task Tables_ErrorInADeclaration_IsLocatedInTheModuleAndTheOtherDeclarationsStay()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl")),
            (
                LibraryModule,
                Sheet("<xsl:function as=\"xs:integer\"><xsl:sequence select=\"1\"/></xsl:function>", Fn("ok", "$a"))
            )
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3005");
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo(LibraryModule);
        await Assert.That(harness.Problems.Problems[0].Location.Line).IsEqualTo(2);
        await Assert.That(harness.Call("ok", 3)).IsEqualTo(3);
    }

    [Test]
    public async Task Tables_ErrorInAShadowedFunction_IsStillReported()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), Fn("pick", "$a")),
            (LibraryModule, Sheet(Fn("pick", "$undefined")))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo(LibraryModule);
        await Assert.That(harness.Call("pick", 4)).IsEqualTo(4);
    }

    [Test]
    public async Task Tables_RegisteredDeclarationWithoutATable_IsAnExplicitErrorAtItsLocation()
    {
        var name = new XPathQName(Xslt, "fake");
        var harness = new ModuleHarness(extra: builder =>
            builder.Add(new FakeXsltInstruction(name, XsltPosition.Declaration))
        );
        _ = harness.Files.Add(LibraryModule, Sheet("<xsl:fake/>"));
        _ = harness.Link(Sheet(Import("lib.xsl")));

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        var problem = harness.Problems.Problems[0];
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Message).Contains("xsl:fake").And.Contains(LibraryModule);
        await Assert.That(problem.Location.Source).IsEqualTo(LibraryModule);
        await Assert.That(problem.Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Tables_GlobalVariable_HigherPrecedenceWinsAndLowerOneIsIgnored()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), "<xsl:variable name=\"v\" select=\"'p'\"/>"),
            (
                LibraryModule,
                Sheet("<xsl:variable name=\"v\" select=\"'l'\"/>", "<xsl:param name=\"w\" select=\"'l'\"/>")
            )
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Tables.Globals.Count).IsEqualTo(2);
        await Assert.That(harness.Tables.Globals.TryGet(new XPathQName(string.Empty, "v"), out var winner)).IsTrue();
        await Assert.That(winner!.Value).IsEqualTo("'p'");
        await Assert.That(winner.Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Tables_GlobalVariableAtTheSamePrecedence_IsXtse0630NamingBothModules()
    {
        var harness = Linked(
            Sheet(Include("lib.xsl"), "<xsl:variable name=\"v\" select=\"1\"/>"),
            (LibraryModule, Sheet("<xsl:variable name=\"v\" select=\"2\"/>"))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(harness.Problems)).IsEqualTo("NES3200/XTSE0630");
        await Assert.That(harness.Problems.Problems[0].Message).Contains(LibraryModule).And.Contains(Principal);
        await Assert.That(harness.Problems.Problems[0].Location.Source).IsEqualTo(Principal);
    }

    [Test]
    public async Task Tables_KeysOfTheSameName_AreAllKeptWhateverTheirPrecedence()
    {
        var harness = Linked(
            Sheet(Import("lib.xsl"), "<xsl:key name=\"k\" match=\"item\" use=\"@code\"/>"),
            (LibraryModule, Sheet("<xsl:key name=\"k\" match=\"other\" use=\"@code\"/>"))
        );

        var ok = harness.Compile();

        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Tables.Keys.Count).IsEqualTo(1);
        await Assert.That(harness.Tables.Keys.TryGet(new XPathQName(string.Empty, "k"), out var declarations)).IsTrue();
        await Assert.That(declarations.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Tables_DefaultResolverDenial_SurfacesAsTheCodedErrorAndTheTablesStayUsable()
    {
        var harness = new ModuleHarness(DenyAllResourceResolver.Instance);

        var linked = harness.Link(Sheet(Import("lib.xsl"), Fn("own", "$a + 1")));
        var ok = harness.Compile();

        await Assert.That(linked).IsFalse();
        await Assert.That(ok).IsTrue();
        await Assert.That(harness.Problems.Problems[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(harness.Call("own", 1)).IsEqualTo(2);
    }

    [Test]
    public async Task Compiler_RejectsNullArguments()
    {
        var registry = new XsltRegistryBuilder().Build();
        var problems = new FakeProblemSink();
        XsltDeclarationCompiler compile = (_, _, _) => null;

        await Assert
            .That(() => XsltModuleCompiler.TryCompile(null!, registry, compile, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltModuleCompiler.TryCompile(XsltModuleSet.Empty, null!, compile, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltModuleCompiler.TryCompile(XsltModuleSet.Empty, registry, null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltModuleCompiler.TryCompile(XsltModuleSet.Empty, registry, compile, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compiler_DeclarationThatTheCallbackDoesNotCompile_IsLeftOutWithoutAnExtraProblem()
    {
        var harness = Linked(Sheet(Fn("ok", "$a")));
        var registry = new XsltRegistryBuilder();
        Register(registry);

        var ok = XsltModuleCompiler.TryCompile(
            harness.Set,
            registry.Build(),
            (_, _, _) => null,
            harness.Problems,
            out var tables
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(tables.Functions.Count).IsEqualTo(0);
        await Assert.That(harness.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Tables_Rejects_NullArguments()
    {
        var harness = Linked(Sheet(Fn("ok", "$a")));
        _ = harness.Compile();

        await Assert
            .That(() => new XsltModuleTables(null!, harness.Tables.Globals, harness.Tables.Keys))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltModuleTables(harness.Tables.Functions, null!, harness.Tables.Keys))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltModuleTables(harness.Tables.Functions, harness.Tables.Globals, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task LocatedSink_AddsTheModuleOnlyWhereTheProblemHasNoSource()
    {
        var inner = new FakeProblemSink();
        var sink = new XsltModuleProblemSink(inner, LibraryModule);
        var located = new DocumentLocation("file:///other.xsl", 4, 5);

        sink.Add(
            new SchematronProblem(
                SchematronDiagnosticCodes.XsltUnsupported,
                "a",
                new DocumentLocation(null, 2, 3),
                "X1"
            )
        );
        sink.Add(new SchematronProblem(SchematronDiagnosticCodes.XsltUnsupported, "b", located));

        await Assert.That(inner.Problems[0].Location).IsEqualTo(new DocumentLocation(LibraryModule, 2, 3));
        await Assert.That(inner.Problems[0].Constraint).IsEqualTo("X1");
        await Assert.That(inner.Problems[1].Location).IsEqualTo(located);
        await Assert.That(() => new XsltModuleProblemSink(null!, LibraryModule)).Throws<ArgumentNullException>();
    }

    private static void Register(XsltRegistryBuilder builder) =>
        NetEvolve.Schemas.Schematron.Xslt.Functions.Registration.Register(builder);
}
