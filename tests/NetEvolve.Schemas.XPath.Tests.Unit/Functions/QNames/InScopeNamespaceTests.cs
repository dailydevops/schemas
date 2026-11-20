namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Threading.Tasks;

/// <summary>
/// <c>fn:namespace-uri-for-prefix</c> and <c>fn:in-scope-prefixes</c> (Functions and Operators 1.0, 11.2 and 11.3): the namespaces
/// in scope on an element, read through its namespace nodes, with the implicit <c>xml</c> binding, a default namespace that a
/// nested declaration removes, and a prefix that a nested declaration rebinds.
/// </summary>
public class InScopeNamespaceTests
{
    private static string[] Prefixes(string element)
    {
        var shown = QNamesHarness.Run("in-scope-prefixes", "node:" + element);
        if (shown.Length == 0)
        {
            return [];
        }

        var items = shown.Split(',');
        Array.Sort(items, StringComparer.Ordinal);
        return items;
    }

    [Test]
    [Arguments("string:p", "node:r", "anyURI:urn:p")]
    [Arguments("string:q", "node:r", "anyURI:urn:q")]
    [Arguments("string:p", "node:a", "anyURI:urn:p2")]
    [Arguments("string:p", "node:b", "anyURI:urn:p2")]
    [Arguments("string:x", "node:b", "anyURI:urn:x")]
    [Arguments("string:q", "node:b", "anyURI:urn:q")]
    [Arguments("string:xml", "node:r", "anyURI:http://www.w3.org/XML/1998/namespace")]
    [Arguments("string:xml", "node:c", "anyURI:http://www.w3.org/XML/1998/namespace")]
    [Arguments("string:xml", "node:o", "anyURI:http://www.w3.org/XML/1998/namespace")]
    [Arguments("untypedAtomic:p", "node:r", "anyURI:urn:p")]
    public async Task NamespaceUriForPrefix_GivesTheNamespaceTheElementBindsToThePrefix(
        string prefix,
        string element,
        string expected
    ) => await Assert.That(QNamesHarness.Run("namespace-uri-for-prefix", prefix, element)).IsEqualTo(expected);

    [Test]
    [Arguments("string:", "node:r", "anyURI:urn:def")]
    [Arguments("()", "node:r", "anyURI:urn:def")]
    [Arguments("string:", "node:c", "anyURI:urn:def")]
    public async Task NamespaceUriForPrefix_AnEmptyPrefixAsksForTheDefaultNamespace(
        string prefix,
        string element,
        string expected
    ) => await Assert.That(QNamesHarness.Run("namespace-uri-for-prefix", prefix, element)).IsEqualTo(expected);

    [Test]
    [Arguments("string:zz", "node:r")]
    [Arguments("string:x", "node:a")]
    [Arguments("string:", "node:a")]
    [Arguments("string:", "node:b")]
    [Arguments("()", "node:a")]
    [Arguments("string:", "node:o")]
    [Arguments("string:p", "node:o")]
    public async Task NamespaceUriForPrefix_AnUnboundPrefixOrAnUndeclaredDefaultNamespace_GivesTheEmptySequence(
        string prefix,
        string element
    ) => await Assert.That(QNamesHarness.Run("namespace-uri-for-prefix", prefix, element)).IsEqualTo(string.Empty);

    [Test]
    [Arguments("string:p", "node:doc")]
    [Arguments("string:p", "node:r@id")]
    [Arguments("string:p", "node:b/t")]
    [Arguments("string:p", "()")]
    [Arguments("string:p", "string:r")]
    [Arguments("integer:1", "node:r")]
    [Arguments("string:p|string:q", "node:r")]
    public async Task NamespaceUriForPrefix_AnArgumentOfTheWrongTypeOrCardinality_IsXPTY0004(
        string prefix,
        string element
    ) => await Assert.That(QNamesHarness.Run("namespace-uri-for-prefix", prefix, element)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task InScopePrefixes_NestedDeclarations_ListEveryPrefixOfTheEnclosingElementsOnce()
    {
        await Assert.That(Prefixes("r")).IsEquivalentTo(["string:", "string:p", "string:q", "string:xml"]);
        await Assert.That(Prefixes("b")).IsEquivalentTo(["string:p", "string:q", "string:x", "string:xml"]);
    }

    [Test]
    public async Task InScopePrefixes_AnUndeclaredDefaultNamespace_LeavesTheEmptyPrefixOut()
    {
        await Assert.That(Prefixes("a")).IsEquivalentTo(["string:p", "string:q", "string:xml"]);
        await Assert.That(Prefixes("b")).DoesNotContain("string:");
    }

    [Test]
    public async Task InScopePrefixes_TheImplicitXmlPrefix_IsAlwaysInScopeExactlyOnce()
    {
        await Assert.That(Prefixes("o")).IsEquivalentTo(["string:xml"]);
        await Assert.That(Prefixes("q")).IsEquivalentTo(["string:xml"]);

        // An explicit declaration of the xml prefix on c must not list it twice.
        await Assert.That(Prefixes("c")).IsEquivalentTo(["string:", "string:p", "string:q", "string:xml"]);
    }

    [Test]
    [Arguments("node:doc")]
    [Arguments("node:r@id")]
    [Arguments("node:b/t")]
    [Arguments("node:cm")]
    [Arguments("()")]
    [Arguments("string:r")]
    [Arguments("node:r|node:a")]
    public async Task InScopePrefixes_AnArgumentThatIsNotExactlyOneElement_IsXPTY0004(string element) =>
        await Assert.That(QNamesHarness.Run("in-scope-prefixes", element)).IsEqualTo("error:XPTY0004");
}
