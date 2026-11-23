namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The compilation tells which identity constraints belong to which element declaration, global or local.</summary>
public class ConstraintOwnerTests
{
    [Test]
    public async Task IdentityConstraints_GlobalAndLocalElements_OwnTheirOwnConstraints()
    {
        var compilation = CompilationFixture.Compile(
            CompilationFixture.Schema(
                "<xs:element name=\"g\"><xs:complexType><xs:sequence><xs:element name=\"l\" minOccurs=\"0\">"
                    + "<xs:complexType><xs:sequence><xs:element name=\"x\" minOccurs=\"0\"/></xs:sequence><xs:attribute name=\"a\"/></xs:complexType>"
                    + Constraint("kLocal")
                    + "</xs:element></xs:sequence></xs:complexType>"
                    + Constraint("kGlobal")
                    + Constraint("kSecond")
                    + "</xs:element><xs:element name=\"plain\"/>"
            )
        );

        var owned = Owned(compilation);

        await Assert.That(compilation.IsValid).IsTrue();
        await Assert.That(owned["g"]).IsEquivalentTo(["kGlobal", "kSecond"]);
        await Assert.That(owned["l"]).IsEquivalentTo(["kLocal"]);
        await Assert.That(owned.ContainsKey("plain")).IsFalse();
        await Assert.That(owned.ContainsKey("x")).IsFalse();
    }

    [Test]
    public async Task IdentityConstraints_TwoChameleonCopies_EachElementOwnsOnlyTheConstraintsOfItsCopy(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new MapResolver().Add(
            "file:///c.xsd",
            CompositionFixture.Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:sequence><xs:element name=\"x\" minOccurs=\"0\"/></xs:sequence><xs:attribute name=\"a\"/></xs:complexType>"
                    + Constraint("k")
                    + "</xs:element>"
            )
        );
        var compiler = CompilationFixture.Compiler(XsdVersion.V10, resolver);
        var inputs = new List<SchemaInput>
        {
            SchemaInput.FromBytes(Utf8(Root("urn:a")), "file:///a.xsd"),
            SchemaInput.FromBytes(Utf8(Root("urn:b")), "file:///b.xsd"),
        };

        var compilation = compiler.Compile(inputs, cancellationToken);

        await Assert.That(compilation.IsValid).IsTrue();
        var elements = compilation.IdentityConstraints.Keys.ToArray();
        await Assert.That(elements.Length).IsEqualTo(2);
        foreach (var element in elements)
        {
            var constraints = compilation.IdentityConstraints[element];
            await Assert.That(constraints.Count).IsEqualTo(1);
            await Assert.That(constraints[0].Name.NamespaceName).IsEqualTo(element.Name.NamespaceName);
        }

        await Assert
            .That(elements.Select(e => e.Name.NamespaceName).OrderBy(name => name, StringComparer.Ordinal))
            .IsEquivalentTo(["urn:a", "urn:b"]);
    }

    [Test]
    public async Task IdentityConstraints_ASchemaWithAnError_HasNone()
    {
        var compilation = CompilationFixture.Compile(
            CompilationFixture.Schema("<xs:element name=\"e\" type=\"t:missing\">" + Constraint("k") + "</xs:element>")
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.IdentityConstraints).IsEmpty();
    }

    private static string Constraint(string name) =>
        "<xs:key name=\"" + name + "\"><xs:selector xpath=\"x\"/><xs:field xpath=\"@a\"/></xs:key>";

    private static string Root(string targetNamespace) =>
        "<xs:schema xmlns:xs=\""
        + CompilationFixture.Xs
        + "\" targetNamespace=\""
        + targetNamespace
        + "\"><xs:include schemaLocation=\"c.xsd\"/></xs:schema>";

    private static ReadOnlyMemory<byte> Utf8(string text) => new UTF8Encoding(false).GetBytes(text);

    private static Dictionary<string, string[]> Owned(SchemaCompilation compilation)
    {
        var owned = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in compilation.IdentityConstraints)
        {
            owned[pair.Key.Name.LocalName] = [.. pair.Value.Select(c => c.Name.LocalName)];
        }

        return owned;
    }
}
