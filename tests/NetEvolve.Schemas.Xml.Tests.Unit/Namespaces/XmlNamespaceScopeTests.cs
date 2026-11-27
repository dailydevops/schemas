namespace NetEvolve.Schemas.Xml.Tests.Unit.Namespaces;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Namespaces;
using NetEvolve.Schemas.Xml.Text.NameTable;

public class XmlNamespaceScopeTests
{
    private const string XmlUri = "http://www.w3.org/XML/1998/namespace";
    private const string XmlnsUri = "http://www.w3.org/2000/xmlns/";

    // Scripted reader events: a start tag is its raw element name plus raw (name, value) attribute pairs.
    private sealed class Script
    {
        internal Script(int maxNames = 1000)
        {
            Table = new SpanInterningNameTable(maxNames);
            Scope = new XmlNamespaceScope(Table);
        }

        internal SpanInterningNameTable Table { get; }

        internal XmlNamespaceScope Scope { get; }

        /// <summary>Runs one start tag; returns the first error and fills the resolved names as {uri}local.</summary>
        internal XmlErrorCode? Start(string element, out string resolved, params string[] attributes)
        {
            resolved = string.Empty;
            Scope.PushElement();
            for (var i = 0; i < attributes.Length; i += 2)
            {
                if (XmlNamespaceScope.IsDeclaration(attributes[i]))
                {
                    var error = Scope.Declare(attributes[i], attributes[i + 1], out _);
                    if (error is not null)
                    {
                        return error;
                    }
                }
            }

            var e = Scope.ResolveElement(element, out var name);
            if (e is not null)
            {
                return e;
            }

            resolved = Format(name);
            for (var i = 0; i < attributes.Length; i += 2)
            {
                if (XmlNamespaceScope.IsDeclaration(attributes[i]))
                {
                    continue;
                }

                e = Scope.ResolveAttribute(attributes[i], out name);
                if (e is not null)
                {
                    return e;
                }

                resolved += " " + Format(name);
            }

            return null;
        }

        internal string Format(XmlName name) =>
            "{" + Table.GetString(name.NamespaceUri) + "}" + Table.GetString(name.Local);
    }

    [Test]
    public async Task UnprefixedElementWithoutDeclarationHasNoNamespace()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out var r)).IsNull();
        await Assert.That(r).IsEqualTo("{}a");
    }

    [Test]
    public async Task DefaultDeclarationAppliesToElementButNotToUnprefixedAttribute()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out var r, "xmlns", "urn:d", "x", "1")).IsNull();
        await Assert.That(r).IsEqualTo("{urn:d}a {}x");
    }

    [Test]
    public async Task PrefixedNamesResolveForElementAndAttribute()
    {
        var s = new Script();

        await Assert.That(s.Start("p:a", out var r, "xmlns:p", "urn:p", "p:x", "1")).IsNull();
        await Assert.That(r).IsEqualTo("{urn:p}a {urn:p}x");
    }

    [Test]
    public async Task NestedElementsInheritAndOverrideDeclarations()
    {
        var s = new Script();
        s.Start("p:a", out _, "xmlns:p", "urn:1", "xmlns", "urn:d");

        await Assert.That(s.Start("p:b", out var inner, "xmlns:p", "urn:2")).IsNull();
        await Assert.That(inner).IsEqualTo("{urn:2}b");
        await Assert.That(s.Start("c", out var deeper)).IsNull();
        await Assert.That(deeper).IsEqualTo("{urn:d}c");
        s.Scope.PopElement();
        s.Scope.PopElement();

        await Assert.That(s.Start("p:d", out var after)).IsNull();
        await Assert.That(after).IsEqualTo("{urn:1}d");
    }

    [Test]
    public async Task OverrideEndsWithItsElement()
    {
        var s = new Script();
        s.Start("a", out _, "xmlns:p", "urn:1");
        s.Start("b", out _, "xmlns:p", "urn:2");
        s.Scope.PopElement();

        await Assert.That(s.Start("p:c", out var r)).IsNull();
        await Assert.That(r).IsEqualTo("{urn:1}c");
        await Assert.That(s.Scope.Depth).IsEqualTo(2);
    }

    [Test]
    public async Task EmptyDefaultDeclarationUndeclaresTheDefaultNamespace()
    {
        var s = new Script();
        s.Start("a", out _, "xmlns", "urn:d");

        await Assert.That(s.Start("b", out var r, "xmlns", string.Empty)).IsNull();
        await Assert.That(r).IsEqualTo("{}b");
        await Assert.That(s.Start("c", out var inner)).IsNull();
        await Assert.That(inner).IsEqualTo("{}c");
    }

    [Test]
    public async Task XmlPrefixIsBoundImplicitly()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out var r, "xml:lang", "en")).IsNull();
        await Assert.That(r).IsEqualTo("{}a {" + XmlUri + "}lang");
    }

    [Test]
    public async Task XmlPrefixMayBeDeclaredToItsOwnNamespace()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out _, "xmlns:xml", XmlUri)).IsNull();
    }

    [Test]
    public async Task UndeclaredElementPrefixRaisesXml3001()
    {
        var s = new Script();

        await Assert.That(s.Start("p:a", out _)).IsEqualTo(XmlErrorCodes.XML3001);
    }

    [Test]
    public async Task UndeclaredAttributePrefixRaisesXml3001()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out _, "p:x", "1")).IsEqualTo(XmlErrorCodes.XML3001);
    }

    [Test]
    public async Task DeclarationOutOfScopeRaisesXml3001()
    {
        var s = new Script();
        s.Start("a", out _, "xmlns:p", "urn:p");
        s.Start("b", out _);
        s.Scope.PopElement();
        s.Scope.PopElement();

        await Assert.That(s.Start("p:c", out _)).IsEqualTo(XmlErrorCodes.XML3001);
    }

    [Test]
    [Arguments("xmlns:xml", "urn:other")]
    [Arguments("xmlns:xml", XmlnsUri)]
    [Arguments("xmlns:p", XmlUri)]
    [Arguments("xmlns", XmlUri)]
    [Arguments("xmlns:xmlns", "urn:x")]
    [Arguments("xmlns:xmlns", XmlnsUri)]
    [Arguments("xmlns:p", XmlnsUri)]
    [Arguments("xmlns", XmlnsUri)]
    public async Task ReservedPrefixOrNamespaceNameMisuseRaisesXml3002(string name, string value)
    {
        var s = new Script();

        await Assert.That(s.Start("a", out _, name, value)).IsEqualTo(XmlErrorCodes.XML3002);
    }

    [Test]
    public async Task ElementWithXmlnsPrefixRaisesXml3002()
    {
        var s = new Script();

        await Assert.That(s.Start("xmlns:a", out _)).IsEqualTo(XmlErrorCodes.XML3002);
    }

    [Test]
    public async Task TwoAttributesWithDifferentPrefixesAndTheSameExpandedNameRaiseXml3003()
    {
        var s = new Script();

        await Assert
            .That(s.Start("a", out _, "xmlns:p", "urn:u", "xmlns:q", "urn:u", "p:x", "1", "q:x", "2"))
            .IsEqualTo(XmlErrorCodes.XML3003);
    }

    [Test]
    public async Task AttributesWithTheSameLocalNameInDifferentNamespacesAreUnique()
    {
        var s = new Script();

        await Assert
            .That(s.Start("a", out _, "xmlns:p", "urn:1", "xmlns:q", "urn:2", "p:x", "1", "q:x", "2", "x", "3"))
            .IsNull();
    }

    [Test]
    public async Task UniquenessIsPerElement()
    {
        var s = new Script();
        s.Start("a", out _, "x", "1");

        await Assert.That(s.Start("b", out _, "x", "1")).IsNull();
    }

    [Test]
    [Arguments("a:b:c")]
    [Arguments(":a")]
    [Arguments("a:")]
    [Arguments(":")]
    [Arguments("1a")]
    [Arguments("a b")]
    [Arguments("")]
    public async Task InvalidElementQNameRaisesXml3004(string name)
    {
        var s = new Script();

        await Assert.That(s.Start(name, out _, "xmlns:a", "urn:a")).IsEqualTo(XmlErrorCodes.XML3004);
    }

    [Test]
    [Arguments("a:b:c")]
    [Arguments(":a")]
    [Arguments("a:")]
    [Arguments("1a")]
    public async Task InvalidAttributeQNameRaisesXml3004(string name)
    {
        var s = new Script();

        await Assert.That(s.Start("r", out _, "xmlns:a", "urn:a", name, "1")).IsEqualTo(XmlErrorCodes.XML3004);
    }

    [Test]
    [Arguments("xmlns:a:b")]
    [Arguments("xmlns:1a")]
    public async Task InvalidDeclaredPrefixRaisesXml3004(string name)
    {
        var s = new Script();

        await Assert.That(s.Start("r", out _, name, "urn:a")).IsEqualTo(XmlErrorCodes.XML3004);
    }

    [Test]
    public async Task EmptyPrefixedDeclarationRaisesXml3005()
    {
        var s = new Script();

        await Assert.That(s.Start("a", out _, "xmlns:p", string.Empty)).IsEqualTo(XmlErrorCodes.XML3005);
    }

    [Test]
    public async Task IsDeclarationRecognizesOnlyXmlnsNames()
    {
        await Assert.That(XmlNamespaceScope.IsDeclaration("xmlns")).IsTrue();
        await Assert.That(XmlNamespaceScope.IsDeclaration("xmlns:p")).IsTrue();
        await Assert.That(XmlNamespaceScope.IsDeclaration("xmlnsx")).IsFalse();
        await Assert.That(XmlNamespaceScope.IsDeclaration("p:xmlns")).IsFalse();
        await Assert.That(XmlNamespaceScope.IsDeclaration("a")).IsFalse();
    }

    [Test]
    public async Task DeclareReturnsTheInternedBinding()
    {
        var s = new Script();
        s.Scope.PushElement();

        await Assert.That(s.Scope.Declare("xmlns:p", "urn:p", out var b)).IsNull();
        await Assert.That(s.Table.GetString(b.Prefix)).IsEqualTo("p");
        await Assert.That(s.Table.GetString(b.Uri)).IsEqualTo("urn:p");
    }

    [Test]
    public async Task InScopeEnumerationListsVisibleBindingsOnce()
    {
        var s = new Script();
        s.Start("a", out _, "xmlns:p", "urn:1", "xmlns:q", "urn:q", "xmlns", "urn:d");
        s.Start("b", out _, "xmlns:p", "urn:2");

        var bindings = new XmlNamespaceBinding[s.Scope.InScopeCount];
        var copied = s.Scope.CopyInScope(bindings);
        var seen = string.Empty;
        foreach (var b in bindings)
        {
            seen += s.Table.GetString(b.Prefix) + "=" + s.Table.GetString(b.Uri) + ";";
        }

        await Assert.That(copied).IsEqualTo(4);

        await Assert.That(s.Scope.InScopeCount).IsEqualTo(4);
        await Assert.That(seen).Contains("p=urn:2;").And.Contains("q=urn:q;").And.Contains("=urn:d;");
        await Assert.That(seen).Contains("xml=" + XmlUri + ";").And.DoesNotContain("urn:1");
    }

    [Test]
    public async Task InScopeEnumerationOmitsAnUndeclaredDefaultNamespace()
    {
        var s = new Script();
        s.Start("a", out _, "xmlns", "urn:d");
        s.Start("b", out _, "xmlns", string.Empty);

        await Assert.That(s.Scope.InScopeCount).IsEqualTo(1);
    }

    [Test]
    public async Task CopyInScopeIntoAShortDestinationThrows()
    {
        var s = new Script();

        await Assert
            .That(() => s.Scope.CopyInScope(Span<XmlNamespaceBinding>.Empty))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FullNameTableRaisesXml1111()
    {
        var s = new Script(maxNames: 5);

        await Assert.That(s.Start("a", out _, "xmlns:p", "urn:p")).IsEqualTo(XmlErrorCodes.XML1111);
    }

    [Test]
    public async Task FrozenNameTableRaisesXml1116()
    {
        var s = new Script();
        s.Table.Freeze();

        await Assert.That(s.Start("a", out _)).IsEqualTo(XmlErrorCodes.XML1116);
    }

    [Test]
    public async Task PopWithoutPushThrows()
    {
        var s = new Script();

        await Assert.That(() => s.Scope.PopElement()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TrustedNamesAgreeWithValidatedNamesForEveryTokenizerName()
    {
        string[] alphabet = ["a", "1", ":", ".", "𐀀"];
        var checkedNames = 0;
        var names = new System.Collections.Generic.List<string>();
        void Grow(string prefix, int depth)
        {
            if (
                prefix.Length > 0
                && NetEvolve.Schemas.Xml.Text.CharClasses.XmlCharClass.NameLength(prefix) == prefix.Length
            )
            {
                names.Add(prefix);
            }

            if (depth == 0)
            {
                return;
            }

            foreach (var token in alphabet)
            {
                Grow(prefix + token, depth - 1);
            }
        }

        Grow(string.Empty, 4);

        foreach (var name in names)
        {
            foreach (var isAttribute in new[] { false, true })
            {
                var plain = new Script();
                var trusted = new Script();
                plain.Scope.PushElement();
                trusted.Scope.PushElement();
                plain.Scope.Declare("xmlns:a", "urn:a", out _);
                trusted.Scope.Declare("xmlns:a", "urn:a", out _);
                var expected = isAttribute
                    ? plain.Scope.ResolveAttribute(name, out var e)
                    : plain.Scope.ResolveElement(name, out e);
                var actual = isAttribute
                    ? trusted.Scope.ResolveAttribute(name, out var a, isName: true)
                    : trusted.Scope.ResolveElement(name, out a, isName: true);
                await Assert.That(actual).IsEqualTo(expected);
                await Assert.That(a.Local).IsEqualTo(e.Local);
                checkedNames++;
            }

            var declared = "xmlns:" + name;
            var plainDeclare = new Script().Scope.Declare(declared, "urn:x", out _);
            var trustedDeclare = new Script().Scope.Declare(declared, "urn:x", out _, isName: true);
            await Assert.That(trustedDeclare).IsEqualTo(plainDeclare);
        }

        await Assert.That(checkedNames).IsGreaterThan(100);
    }
}
