namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Xslt.Modules;

/// <summary>Stage Compile compiles the XSLT modules that stage Resolve linked and keeps their tables (#832).</summary>
public class XsltModuleCompileTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Root = InclusionFixture.RootId;

    private static readonly BuildSettings Settings = new(
        "#ALL",
        new Dictionary<string, string>(),
        ValidationLimits.Default,
        DenyAllResourceResolver.Instance
    );

    private static string Principal(string body) =>
        $"<schema xmlns=\"{InclusionFixture.Ns}\" queryBinding=\"xslt2\" {Xsl} xmlns:f=\"urn:f\">{body}"
        + "<pattern id=\"p\"><rule context=\"a\"><assert test=\"true()\">ok</assert></rule></pattern></schema>";

    private static string Sheet(string body) =>
        $"<xsl:stylesheet version=\"2.0\" {Xsl} xmlns:f=\"urn:f\">{body}</xsl:stylesheet>";

    private static string Var(string name, string value = "1") => $"<xsl:variable name=\"{name}\" select=\"{value}\"/>";

    private static string Fn(string name, string value) =>
        $"<xsl:function name=\"f:{name}\"><xsl:param name=\"x\"/><xsl:sequence select=\"{value}\"/></xsl:function>";

    private static string Import(string href) => $"<xsl:import href=\"{href}\"/>";

    private static (bool Ok, CompiledSchema? Compiled, FakeProblemSink Problems) Compile(
        string xml,
        FakeResources? files = null
    )
    {
        var problems = new FakeProblemSink();
        var loader = new ModuleLoader(files ?? new FakeResources(), ValidationLimits.Default);
        if (!new ModelResolver().TryResolve(InclusionFixture.Parse(xml), loader, problems, out var resolved))
        {
            throw new InvalidOperationException("The resolve stage must succeed.");
        }

        var ok = new SchematronSchemaCompiler(PipelineBindings.Registry).TryCompile(
            resolved.Schema,
            Settings,
            resolved.Modules,
            problems,
            out var compiled
        );
        return (ok, ok ? compiled : null, problems);
    }

    private static XsltModuleTables Tables(CompiledSchema? compiled) => ((CompiledProgram)compiled!).XsltTables!;

    [Test]
    public async Task Compile_AllKindsOfDeclaration_KeepsTheTablesInTheProgram()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.xsl",
            Sheet(
                Var("v", "2")
                    + "<xsl:key name=\"k\" match=\"a\" use=\"@id\"/>"
                    + "<xsl:decimal-format name=\"eu\" decimal-separator=\",\" grouping-separator=\".\"/>"
                    + Fn("twice", "$x * $v")
            )
        );

        var (ok, compiled, problems) = Compile(Principal(Import("lib.xsl")), files);
        var tables = Tables(compiled);

        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(ok).IsTrue();
        await Assert.That(tables.Functions.Count).IsEqualTo(1);
        await Assert.That(tables.Globals.Count).IsEqualTo(1);
        await Assert.That(tables.Keys.Count).IsEqualTo(1);
        await Assert.That(tables.DecimalFormats.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_GlobalOfThePrincipal_ShadowsTheImportedOneWithoutAConflict()
    {
        var files = new FakeResources().Add("file:///s/lib.xsl", Sheet(Var("v", "1")));

        var (ok, compiled, problems) = Compile(Principal(Import("lib.xsl") + Var("v", "2")), files);

        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(ok).IsTrue();
        await Assert.That(Tables(compiled).Globals.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_TwoFunctionsAtOnePrecedence_ReportTheConflict()
    {
        var files = new FakeResources().Add("file:///s/lib.xsl", Sheet(Fn("same", "1")));

        var (ok, _, problems) = Compile(Principal(Fn("same", "2") + "<xsl:include href=\"lib.xsl\"/>"), files);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Single().Constraint).IsEqualTo("XTSE0770");
    }

    [Test]
    public async Task Compile_ConflictingDecimalFormats_ReportTheAttributeConflict()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.xsl",
            Sheet("<xsl:decimal-format decimal-separator=\",\" grouping-separator=\".\"/>")
        );

        var (ok, _, problems) = Compile(
            Principal(
                "<xsl:decimal-format decimal-separator=\";\" grouping-separator=\".\"/>"
                    + "<xsl:include href=\"lib.xsl\"/>"
            ),
            files
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Single().Constraint).IsEqualTo("XTSE1290");
    }

    [Test]
    public async Task Compile_GlobalWithoutAName_Fails()
    {
        var (ok, compiled, problems) = Compile(Principal("<xsl:variable select=\"1\"/>" + Fn("one", "1")));

        await Assert.That(ok).IsFalse();
        await Assert.That(compiled).IsNull();
        await Assert.That(problems.Problems.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Compile_FunctionThatReadsAnUndeclaredVariable_FailsInTheSecondPass()
    {
        var (ok, _, problems) = Compile(Principal(Fn("bad", "$undeclared")));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Compile_UnregisteredDeclaration_IsUnsupportedAtItsModule()
    {
        var (ok, _, problems) = Compile(Principal("<xsl:template match=\"/\"/>"));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Single().Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problems.Problems.Single().Location.Source).IsEqualTo(Root);
    }

    [Test]
    public async Task Compile_SchemaWithoutDeclarations_KeepsNoTables()
    {
        var (ok, compiled, _) = Compile(Principal(string.Empty));

        await Assert.That(ok).IsTrue();
        await Assert.That(((CompiledProgram)compiled!).XsltTables).IsNull();
    }

    [Test]
    public async Task Compile_WithoutAModuleSet_ReportsTheXsltChildrenAsUnsupported()
    {
        var problems = new FakeProblemSink();
        var schema = InclusionFixture.Parse(Principal(Var("v"))).Schema;

        var ok = new SchematronSchemaCompiler(PipelineBindings.Registry).TryCompile(schema, Settings, problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Single().Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }

    [Test]
    public async Task Compile_Rejects_NullModules()
    {
        var schema = InclusionFixture.Parse(Principal(string.Empty)).Schema;
        var compiler = new SchematronSchemaCompiler(PipelineBindings.Registry);

        await Assert
            .That(() => compiler.TryCompile(schema, Settings, null!, new FakeProblemSink(), out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Declarations_RejectNullArguments()
    {
        var schema = InclusionFixture.Parse(Principal(string.Empty)).Schema;
        var problems = new FakeProblemSink();
        _ = PipelineBindings.Registry.TryResolve(schema, out var binding, out _);

        await Assert
            .That(() => XsltDeclarations.TryCompileGlobals(null!, XsltModuleSet.Empty, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltDeclarations.TryCompileGlobals(schema, null!, binding, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltDeclarations.TryCompileGlobals(schema, XsltModuleSet.Empty, null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XsltDeclarations.TryCompileGlobals(schema, XsltModuleSet.Empty, binding, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task NotImplementedCompiler_ModuleOverload_Throws()
    {
        var schema = InclusionFixture.Parse(Principal(string.Empty)).Schema;

        await Assert
            .That(() =>
                new NotImplementedSchemaCompiler().TryCompile(
                    schema,
                    Settings,
                    XsltModuleSet.Empty,
                    new FakeProblemSink(),
                    out _
                )
            )
            .Throws<SchemaCompilationException>();
    }
}
