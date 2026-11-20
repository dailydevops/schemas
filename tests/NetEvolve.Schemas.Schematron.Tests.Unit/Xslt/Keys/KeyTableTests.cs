namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys.KeyFixture;

/// <summary>The key declarations of a schema by name: all declarations of one name are kept, in declaration order.</summary>
public class KeyTableTests
{
    [Test]
    public async Task Create_TwoDeclarationsWithOneName_KeepsBothInDeclarationOrder()
    {
        var fixture = new KeyFixture();
        var components = fixture.CompileAll(
            Kx.Key("k", "item", "@code", 10),
            Kx.Key("other", "sub", "@code", 20),
            Kx.Key("k", "other", "@code", 30)
        );

        var table = XsltKeyTable.Create(components);

        await Assert.That(table.Count).IsEqualTo(2);
        await Assert.That(table.TryGet(Key("k"), out var declarations)).IsTrue();
        await Assert.That(declarations.Length).IsEqualTo(2);
        await Assert.That(declarations[0].Location.Line).IsEqualTo(10);
        await Assert.That(declarations[1].Location.Line).IsEqualTo(30);
    }

    [Test]
    public async Task TryGet_AnUndeclaredName_IsFalse()
    {
        var table = XsltKeyTable.Create([]);

        await Assert.That(table.TryGet(Key("k"), out var declarations)).IsFalse();
        await Assert.That(declarations).IsEmpty();
        await Assert.That(table.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_TheSameLocalNameInTwoNamespaces_AreTwoKeys()
    {
        var fixture = new KeyFixture();
        var components = fixture.CompileAll(Kx.Key("k", "item", "@code", 10), Kx.Key("f:k", "item", "@code", 20));

        var table = XsltKeyTable.Create(components);

        await Assert.That(table.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Create_WithNull_Throws() =>
        await Assert.That(() => XsltKeyTable.Create(null!)).Throws<ArgumentNullException>();
}
