namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.References;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.References;

public class XmlReferenceResolverTests
{
    private static (XmlErrorCode? Error, string Resolved, int Offset) Resolve(string text)
    {
        var buffer = new char[text.Length];
        var error = XmlReferenceResolver.TryResolve(text, buffer, out var resolved, out var offset);
        return (error, resolved.ToString(), offset);
    }

    [Test]
    [Arguments("&#65;", "A")]
    [Arguments("&#x41;", "A")]
    [Arguments("&#x4a;", "J")]
    [Arguments("&#x4A;", "J")]
    [Arguments("&#233;", "é")]
    [Arguments("&#xE9;", "é")]
    [Arguments("&#9;", "\t")]
    [Arguments("&#x10000;", "𐀀")]
    [Arguments("&#x1F600;", "😀")]
    [Arguments("&#128512;", "😀")]
    [Arguments("&#x10FFFF;", "􏿿")]
    [Arguments("&#x0000041;", "A")]
    [Arguments("a&#x41;b&#66;c", "aAbBc")]
    public async Task TryResolve_CharacterReference_ReturnsCharacters(string text, string expected)
    {
        ArgumentNullException.ThrowIfNull(text);
        var (error, resolved, _) = Resolve(text);

        await Assert.That(error).IsNull();
        await Assert.That(resolved).IsEqualTo(expected);
    }

    [Test]
    [Arguments("&lt;", "<")]
    [Arguments("&gt;", ">")]
    [Arguments("&amp;", "&")]
    [Arguments("&apos;", "'")]
    [Arguments("&quot;", "\"")]
    public async Task TryResolve_PredefinedEntity_ReturnsReplacementCharacter(string text, string expected)
    {
        ArgumentNullException.ThrowIfNull(text);
        var (error, resolved, _) = Resolve(text);

        await Assert.That(error).IsNull();
        await Assert.That(resolved).IsEqualTo(expected);
    }

    [Test]
    public async Task TryResolve_MixedReferences_ResolvesAllInOrder()
    {
        var (error, resolved, _) = Resolve("1 &lt; 2 &amp;&amp; 3 &gt; 2 &#x26; &quot;x&apos;");

        await Assert.That(error).IsNull();
        await Assert.That(resolved).IsEqualTo("1 < 2 && 3 > 2 & \"x'");
    }

    [Test]
    [Arguments("&#0;")]
    [Arguments("&#x0;")]
    [Arguments("&#xFFFE;")]
    [Arguments("&#xFFFF;")]
    [Arguments("&#65534;")]
    [Arguments("&#xD800;")]
    [Arguments("&#xDFFF;")]
    [Arguments("&#55296;")]
    [Arguments("&#x110000;")]
    [Arguments("&#1114112;")]
    [Arguments("&#x1;")]
    [Arguments("&#xFFFFFFFFFFFFFFFFFFFF;")]
    [Arguments("&#99999999999999999999;")]
    public async Task TryResolve_IllegalCharacterReference_ReportsLegalCharacter(string text)
    {
        var (error, _, offset) = Resolve("ab" + text);

        await Assert.That(error).IsEqualTo(XmlErrorCodes.XML0009);
        await Assert.That(offset).IsEqualTo(2);
    }

    [Test]
    [Arguments("&#65")]
    [Arguments("&#x41")]
    [Arguments("&amp")]
    [Arguments("&amp x;")]
    [Arguments("&#65 ;")]
    [Arguments("&#6x5;")]
    [Arguments("&#xG;")]
    [Arguments("&#X41;")]
    [Arguments("&#;")]
    [Arguments("&#x;")]
    [Arguments("&;")]
    [Arguments("&")]
    [Arguments("& ")]
    [Arguments("&1a;")]
    [Arguments("&#")]
    public async Task TryResolve_MalformedReference_ReportsReferenceSyntax(string text)
    {
        var (error, _, offset) = Resolve("ab" + text);

        await Assert.That(error).IsEqualTo(XmlErrorCodes.XML0016);
        await Assert.That(offset).IsEqualTo(2);
    }

    [Test]
    [Arguments("&nbsp;")]
    [Arguments("&AMP;")]
    [Arguments("&lt2;")]
    [Arguments("&foo;")]
    public async Task TryResolve_UnknownEntity_ReportsEntityDeclared(string text)
    {
        var (error, _, offset) = Resolve("ab" + text);

        await Assert.That(error).IsEqualTo(XmlErrorCodes.XML0008);
        await Assert.That(offset).IsEqualTo(2);
    }

    [Test]
    public async Task TryResolve_ErrorAfterValidReference_ReportsOffsetOfFailingReference()
    {
        var (error, _, offset) = Resolve("&lt;&nbsp;");

        await Assert.That(error).IsEqualTo(XmlErrorCodes.XML0008);
        await Assert.That(offset).IsEqualTo(4);
    }

    [Test]
    public async Task TryParseReference_Reference_ReturnsLengthAndCodePoint()
    {
        var error = XmlReferenceResolver.TryParseReference("&#x1F600;tail", out var length, out var codePoint);

        await Assert.That(error).IsNull();
        await Assert.That(length).IsEqualTo(9);
        await Assert.That(codePoint).IsEqualTo(0x1F600);
    }

    [Test]
    public async Task TryResolve_NoAmpersand_ReturnsInputSliceWithoutCopy()
    {
        var (error, sameSlice) = ResolveWithoutBuffer("plain text without references");

        await Assert.That(error).IsNull();
        await Assert.That(sameSlice).IsTrue();
    }

    private static (XmlErrorCode? Error, bool SameSlice) ResolveWithoutBuffer(string text)
    {
        var error = XmlReferenceResolver.TryResolve(text, Span<char>.Empty, out var resolved, out _);
        var same = resolved.Length == text.Length && resolved.Overlaps(text.AsSpan(), out var offset) && offset == 0;
        return (error, same);
    }

    [Test]
    public async Task TryResolve_NoAmpersand_AllocatesZeroBytes()
    {
        var text = new string('x', 256);
        var buffer = new char[text.Length];

        AllocationAssert.NoAllocation(() => _ = XmlReferenceResolver.TryResolve(text, buffer, out _, out _));

        await Task.CompletedTask;
    }

    [Test]
    public async Task TryResolve_WithReferences_AllocatesZeroBytesWithCallerBuffer()
    {
        var text = "a &lt; b &#x41; c &amp; d";
        var buffer = new char[text.Length];

        AllocationAssert.NoAllocation(() => _ = XmlReferenceResolver.TryResolve(text, buffer, out _, out _));

        await Task.CompletedTask;
    }
}
