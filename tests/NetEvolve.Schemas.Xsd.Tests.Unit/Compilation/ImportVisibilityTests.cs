namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Threading;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>A reference to another namespace resolves only when the referencing document imports that namespace directly (src-resolve).</summary>
public class ImportVisibilityTests
{
    private static string Root(string body, string imports) =>
        CompositionFixture
            .Schema(imports + body, Tns)
            .Replace("<xs:schema ", "<xs:schema xmlns:a=\"urn:a\" xmlns:b=\"urn:b\" ", StringComparison.Ordinal);

    private static MapResolver Resolver() =>
        new MapResolver()
            .Add(
                "file:///a.xsd",
                CompositionFixture.Schema(
                    "<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/><xs:complexType name=\"a\"/>",
                    "urn:a"
                )
            )
            .Add("file:///b.xsd", CompositionFixture.Schema("<xs:complexType name=\"b\"/>", "urn:b"));

    private const string UseBoth = "<xs:element name=\"x\" type=\"a:a\"/><xs:element name=\"y\" type=\"b:b\"/>";

    [Test]
    public async Task Compile_ReferenceToNamespaceImportedOnlyByAnotherDocument_IsUnresolved()
    {
        var compilation = Compile(
            Root(UseBoth, "<xs:import namespace=\"urn:a\" schemaLocation=\"a.xsd\"/>"),
            XsdVersion.V10,
            Resolver()
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Compile_ReferenceToDirectlyImportedNamespaces_Resolves()
    {
        var compilation = Compile(
            Root(
                UseBoth,
                "<xs:import namespace=\"urn:a\" schemaLocation=\"a.xsd\"/><xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>"
            ),
            XsdVersion.V10,
            Resolver()
        );

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task Compile_AnonymousTypeReferenceToUnimportedNamespace_IsUnresolved()
    {
        var compilation = Compile(
            Root(
                "<xs:element name=\"y\"><xs:complexType><xs:sequence><xs:element name=\"z\" type=\"b:b\"/></xs:sequence></xs:complexType></xs:element>",
                "<xs:import namespace=\"urn:a\" schemaLocation=\"a.xsd\"/>"
            ),
            XsdVersion.V10,
            Resolver()
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Compile_KeyrefReferToConstraintOfNamespaceThatIsNotImported_IsUnresolved()
    {
        var resolver = new MapResolver().Add(
            "file:///k.xsd",
            CompositionFixture.Schema(
                "<xs:element name=\"k\"><xs:complexType/><xs:keyref name=\"r\" refer=\"c\"><xs:selector xpath=\".\"/><xs:field xpath=\"@f\"/></xs:keyref></xs:element>",
                "urn:k"
            )
        );
        var root = CompositionFixture.Schema(
            "<xs:import namespace=\"urn:k\" schemaLocation=\"k.xsd\"/>"
                + "<xs:element name=\"e\"><xs:complexType/><xs:key name=\"c\"><xs:selector xpath=\".\"/><xs:field xpath=\"@f\"/></xs:key></xs:element>"
        );

        var compilation = Compile(root, XsdVersion.V10, resolver);

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Compile_RootDocumentsOfOneSchemaSet_SeeEachOther(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var first = Root("<xs:element name=\"x\" type=\"a:a\"/>", string.Empty);
        var second = CompositionFixture.Schema("<xs:complexType name=\"a\"/>", "urn:a");

        var compilation = Compiler()
            .Compile(
                [
                    SchemaInput.FromChars(first.AsMemory(), "file:///r1.xsd"),
                    SchemaInput.FromChars(second.AsMemory(), "file:///r2.xsd"),
                ],
                cancellationToken
            );

        await Assert.That(compilation.IsValid).IsTrue();
    }
}
