namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System;
using System.Linq;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion.InclusionFixture;

/// <summary>Stage Resolve links the XSLT modules of a schema with the loader of the Schematron include (#832).</summary>
public class XsltModuleResolveTests
{
    private const string Xsl = "xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";
    private const string Xslt2 = " queryBinding=\"xslt2\"";

    private static string Sheet(string body) =>
        $"<xsl:stylesheet version=\"2.0\" {Xsl} xmlns:f=\"urn:f\">{body}</xsl:stylesheet>";

    private static string Local(XsltModuleDeclaration declaration) =>
        declaration.Element.Tree!.Names.GetString(declaration.Element.Node.Name.Local);

    [Test]
    public async Task Resolve_ImportInThePrincipal_LinksTheModuleBelowThePrincipal()
    {
        var files = new FakeResources()
            .Add("file:///s/lib.xsl", Sheet("<xsl:variable name=\"lib\" select=\"1\"/>"))
            .Add("file:///s/other.xsl", Sheet("<xsl:key name=\"k\" match=\"a\" use=\"@id\"/>"));

        var outcome = Resolve(
            Schema(
                $"<xsl:import {Xsl} href=\"lib.xsl\"/><xsl:include {Xsl} href=\"other.xsl\"/>" + Pattern("p"),
                Xslt2
            ),
            files
        );

        var declarations = outcome.Modules.Declarations;
        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(declarations.Count).IsEqualTo(2);
        await Assert.That(Local(declarations[0])).IsEqualTo("variable");
        await Assert.That(declarations[0].SystemId).IsEqualTo("file:///s/lib.xsl");
        await Assert.That(Local(declarations[1])).IsEqualTo("key");
        await Assert.That(declarations[1].Precedence).IsGreaterThan(declarations[0].Precedence);
        await Assert.That(files.Requests).IsEquivalentTo(["file:///s/lib.xsl", "file:///s/other.xsl"]);
    }

    [Test]
    public async Task Resolve_XsltChildOfAnIncludedLibrary_IsADeclarationOfThePrincipalModule()
    {
        var files = new FakeResources().Add(
            "file:///s/lib.sch",
            Library($"<xsl:variable {Xsl} name=\"fromLibrary\" select=\"1\"/>")
        );

        var outcome = Resolve(Schema(Include("lib.sch") + Pattern("p"), Xslt2 + Edition2025), files);

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Modules.Declarations.Count).IsEqualTo(1);
        await Assert.That(outcome.Modules.Declarations[0].SystemId).IsEqualTo(RootId);
    }

    [Test]
    public async Task Resolve_PrefixDeclaredOutsideTheDeclaration_IsInScopeAtTheDeclaration()
    {
        var root =
            $"<schema xmlns=\"{Ns}\" queryBinding=\"xslt2\" xmlns:f=\"urn:f\" {Xsl}>"
            + "<xsl:function name=\"f:one\" xmlns:g=\"urn:g\"><xsl:sequence select=\"1\"/></xsl:function></schema>";

        var outcome = Resolve(root, new FakeResources());
        var element = outcome.Modules.Declarations.Single().Element;

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(XsltElementReader.TryResolvePrefix(element, "f", out var f)).IsTrue();
        await Assert.That(f).IsEqualTo("urn:f");
        await Assert.That(XsltElementReader.TryResolvePrefix(element, "g", out var g)).IsTrue();
        await Assert.That(g).IsEqualTo("urn:g");
    }

    [Test]
    public async Task Resolve_NestedElementDeclaringAPrefix_KeepsTheBindingOnTheNestedElement()
    {
        var root =
            $"<schema xmlns=\"{Ns}\" queryBinding=\"xslt2\" {Xsl}>"
            + "<xsl:variable name=\"v\"><e xmlns:n=\"urn:n\" xmlns:xsl2=\"urn:x\"/></xsl:variable></schema>";

        var outcome = Resolve(root, new FakeResources());
        var nested = outcome.Modules.Declarations.Single().Element.Node.FirstChild;

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(nested.FirstNamespace.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Resolve_UnderAnXpathBinding_LinksNothing()
    {
        var outcome = Resolve(
            Schema($"<xsl:import {Xsl} href=\"missing.xsl\"/>" + Pattern("p"), " queryBinding=\"xpath2\""),
            new FakeResources()
        );

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Modules).IsSameReferenceAs(XsltModuleSet.Empty);
    }

    [Test]
    public async Task Resolve_SchemaWithoutXsltChildren_LinksNothing()
    {
        var outcome = Resolve(Schema(Pattern("p"), Xslt2), new FakeResources());

        await Assert.That(outcome.Ok).IsTrue();
        await Assert.That(outcome.Modules.Declarations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_ModuleThatCannotBeLoaded_FailsWithTheLoadProblem()
    {
        var outcome = Resolve(
            Schema($"<xsl:import {Xsl} href=\"missing.xsl\"/>" + Pattern("p"), Xslt2),
            new FakeResources()
        );

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_ModuleThatImportsItself_IsAnImportCycle()
    {
        var files = new FakeResources().Add("file:///s/a.xsl", Sheet($"<xsl:import href=\"a.xsl\"/>"));

        var outcome = Resolve(Schema($"<xsl:import {Xsl} href=\"a.xsl\"/>" + Pattern("p"), Xslt2), files);

        await Assert.That(outcome.Ok).IsFalse();
        await Assert.That(outcome.Problems.Single().Constraint).IsEqualTo("XTSE0210");
    }

    [Test]
    public async Task ResolvedModel_WithoutModules_HasTheEmptySet()
    {
        var parsed = Parse(Schema(Pattern("p")));

        await Assert.That(new ResolvedModel(parsed.Schema).Modules).IsSameReferenceAs(XsltModuleSet.Empty);
        await Assert.That(() => new ResolvedModel(null!)).Throws<ArgumentNullException>();
    }
}
