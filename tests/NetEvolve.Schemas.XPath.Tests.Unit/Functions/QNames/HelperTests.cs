namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.QNames;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The guards of the helpers of the QName and URI group: every public or internal boundary refuses a null or a missing value.</summary>
public class HelperTests
{
    [Test]
    public async Task Registration_ANullBuilder_IsRefused() =>
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task UriEscaping_ANullText_IsRefused()
    {
        await Assert.That(() => UriEscaping.EncodeForUri(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => UriEscaping.IriToUri(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task UriEscapingFunction_ANullEscape_IsRefused() =>
        await Assert.That(() => new UriEscapingFunction("encode-for-uri", null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task PrefixFromQNameFunction_ANullName_IsRefused() =>
        await Assert.That(() => PrefixFromQNameFunction.NCNameItem(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task InScopeNamespaces_AMissingElement_IsRefused()
    {
        await Assert
            .That(() => InScopeNamespaces.TryResolve(default(XPathNode), "p".AsSpan(), out _))
            .Throws<ArgumentNullException>();
        await Assert.That(() => InScopeNamespaces.Prefixes(default(XPathNode))).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments('a', false)]
    [Arguments('~', false)]
    [Arguments('%', false)]
    [Arguments('#', false)]
    [Arguments(' ', true)]
    [Arguments('\u0000', true)]
    [Arguments('\u007F', true)]
    [Arguments('\u00E9', true)]
    [Arguments('<', true)]
    [Arguments('>', true)]
    [Arguments('"', true)]
    [Arguments('{', true)]
    [Arguments('}', true)]
    [Arguments('|', true)]
    [Arguments('\\', true)]
    [Arguments('^', true)]
    [Arguments('`', true)]
    public async Task UriEscaping_IsForbiddenInUri_NamesTheCharactersOfIriToUri(char c, bool forbidden) =>
        await Assert.That(UriEscaping.IsForbiddenInUri(c)).IsEqualTo(forbidden);
}
