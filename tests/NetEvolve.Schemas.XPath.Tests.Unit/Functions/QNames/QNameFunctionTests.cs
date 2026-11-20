namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:resolve-QName</c>, <c>fn:QName</c>, and the three accessors <c>fn:prefix-from-QName</c>, <c>fn:local-name-from-QName</c>, and
/// <c>fn:namespace-uri-from-QName</c> (Functions and Operators 1.0, section 11): a QName value is shown as
/// <c>QName:prefix:local{namespace}</c>, and a QName without a namespace has the empty braces.
/// </summary>
public class QNameFunctionTests
{
    [Test]
    [Arguments("string:p:a", "node:r", "QName:p:a{urn:p}")]
    [Arguments("string:q:a", "node:r", "QName:q:a{urn:q}")]
    [Arguments("string:b", "node:r", "QName:b{urn:def}")]
    [Arguments("string:p:b", "node:a", "QName:p:b{urn:p2}")]
    [Arguments("string:x:l", "node:b", "QName:x:l{urn:x}")]
    [Arguments("string:p:l", "node:b", "QName:p:l{urn:p2}")]
    [Arguments("string:xml:space", "node:r", "QName:xml:space{http://www.w3.org/XML/1998/namespace}")]
    [Arguments("string:xml:space", "node:c", "QName:xml:space{http://www.w3.org/XML/1998/namespace}")]
    [Arguments("string:xml:lang", "node:o", "QName:xml:lang{http://www.w3.org/XML/1998/namespace}")]
    [Arguments("string:b", "node:o", "QName:b{}")]
    [Arguments("string: p:a ", "node:r", "QName:p:a{urn:p}")]
    [Arguments("anyURI:p:a", "node:r", "QName:p:a{urn:p}")]
    [Arguments("untypedAtomic:p:a", "node:r", "QName:p:a{urn:p}")]
    public async Task ResolveQName_ResolvesThePrefixAgainstTheInScopeNamespacesOfTheElement(
        string qname,
        string element,
        string expected
    ) => await Assert.That(QNamesHarness.Run("resolve-QName", qname, element)).IsEqualTo(expected);

    [Test]
    [Arguments("string:b", "node:a", "QName:b{}")]
    [Arguments("string:b", "node:b", "QName:b{}")]
    public async Task ResolveQName_AnUnprefixedNameTakesTheDefaultNamespaceOnlyWhenOneIsInScope(
        string qname,
        string element,
        string expected
    ) => await Assert.That(QNamesHarness.Run("resolve-QName", qname, element)).IsEqualTo(expected);

    [Test]
    [Arguments("string:x:l", "node:a")]
    [Arguments("string:zz:l", "node:r")]
    [Arguments("string:zz:l", "node:o")]
    public async Task ResolveQName_AnUnboundPrefix_IsFONS0004(string qname, string element) =>
        await Assert.That(QNamesHarness.Run("resolve-QName", qname, element)).IsEqualTo("error:FONS0004");

    [Test]
    [Arguments("string:1a")]
    [Arguments("string:a:b:c")]
    [Arguments("string:")]
    [Arguments("string:   ")]
    [Arguments("string::a")]
    [Arguments("string:a:")]
    [Arguments("string:@a")]
    [Arguments("string:a b")]
    [Arguments("string:p:1a")]
    public async Task ResolveQName_AnInvalidLexicalQName_IsFOCA0002(string qname) =>
        await Assert.That(QNamesHarness.Run("resolve-QName", qname, "node:r")).IsEqualTo("error:FOCA0002");

    [Test]
    public async Task ResolveQName_TheEmptySequence_GivesTheEmptySequence() =>
        await Assert.That(QNamesHarness.Run("resolve-QName", "()", "node:r")).IsEqualTo(string.Empty);

    [Test]
    [Arguments("string:a", "node:doc")]
    [Arguments("string:a", "node:r@id")]
    [Arguments("string:a", "node:b/t")]
    [Arguments("string:a", "node:cm")]
    [Arguments("string:a", "()")]
    [Arguments("string:a", "string:b")]
    [Arguments("string:a", "node:r|node:a")]
    [Arguments("integer:1", "node:r")]
    [Arguments("string:a|string:b", "node:r")]
    public async Task ResolveQName_AnArgumentOfTheWrongTypeOrCardinality_IsXPTY0004(string qname, string element) =>
        await Assert.That(QNamesHarness.Run("resolve-QName", qname, element)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("string:urn:x", "string:x:l", "QName:x:l{urn:x}")]
    [Arguments("string:urn:x", "string:l", "QName:l{urn:x}")]
    [Arguments("string:", "string:l", "QName:l{}")]
    [Arguments("()", "string:l", "QName:l{}")]
    [Arguments("anyURI:urn:x", "string:l", "QName:l{urn:x}")]
    [Arguments("string:urn:x", "untypedAtomic:l", "QName:l{urn:x}")]
    [Arguments("string:urn:x", "string: x:l ", "QName:x:l{urn:x}")]
    public async Task QName_BuildsTheValueFromTheNamespaceAndTheLexicalName(
        string namespaceText,
        string lexical,
        string expected
    ) => await Assert.That(QNamesHarness.Run("QName", namespaceText, lexical)).IsEqualTo(expected);

    [Test]
    [Arguments("string:", "string:p:l")]
    [Arguments("()", "string:p:l")]
    [Arguments("string:urn:x", "string:1a")]
    [Arguments("string:urn:x", "string:a:b:c")]
    [Arguments("string:urn:x", "string:@a")]
    [Arguments("string:urn:x", "string::a")]
    [Arguments("string:urn:x", "string:a:")]
    [Arguments("string:urn:x", "string:<a>")]
    [Arguments("string:urn:x", "string:")]
    [Arguments("string:urn:x", "string: ")]
    public async Task QName_AnInvalidLexicalNameOrAPrefixWithoutANamespace_IsFOCA0002(
        string namespaceText,
        string lexical
    ) => await Assert.That(QNamesHarness.Run("QName", namespaceText, lexical)).IsEqualTo("error:FOCA0002");

    [Test]
    [Arguments("integer:1", "string:a")]
    [Arguments("string:urn:x", "integer:1")]
    [Arguments("string:urn:x", "()")]
    [Arguments("()", "()")]
    [Arguments("string:a|string:b", "string:c")]
    public async Task QName_AnArgumentOfTheWrongTypeOrCardinality_IsXPTY0004(string namespaceText, string lexical) =>
        await Assert.That(QNamesHarness.Run("QName", namespaceText, lexical)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("qname:p,urn:p,a", "NCName:p")]
    [Arguments("qname:,urn:def,a", "")]
    [Arguments("qname:,,a", "")]
    [Arguments("()", "")]
    public async Task PrefixFromQName_GivesThePrefixAsNCNameOrTheEmptySequence(string qname, string expected) =>
        await Assert.That(QNamesHarness.Run("prefix-from-QName", qname)).IsEqualTo(expected);

    [Test]
    [Arguments("qname:p,urn:p,a", "NCName:a")]
    [Arguments("qname:,,b", "NCName:b")]
    [Arguments("()", "")]
    public async Task LocalNameFromQName_GivesTheLocalNameAsNCNameOrTheEmptySequence(string qname, string expected) =>
        await Assert.That(QNamesHarness.Run("local-name-from-QName", qname)).IsEqualTo(expected);

    [Test]
    [Arguments("qname:p,urn:p,a", "anyURI:urn:p")]
    [Arguments("qname:,,a", "anyURI:")]
    [Arguments("()", "")]
    public async Task NamespaceUriFromQName_GivesTheNamespaceAsAnyUriOrTheEmptySequence(
        string qname,
        string expected
    ) => await Assert.That(QNamesHarness.Run("namespace-uri-from-QName", qname)).IsEqualTo(expected);

    [Test]
    [Arguments("prefix-from-QName")]
    [Arguments("local-name-from-QName")]
    [Arguments("namespace-uri-from-QName")]
    public async Task TheAccessors_AnArgumentThatIsNotAQName_IsXPTY0004(string function)
    {
        await Assert.That(QNamesHarness.Run(function, "string:p:a")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "string:")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "untypedAtomic:a")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "integer:1")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "node:r")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "qname:,,a|qname:,,b")).IsEqualTo("error:XPTY0004");
    }
}
