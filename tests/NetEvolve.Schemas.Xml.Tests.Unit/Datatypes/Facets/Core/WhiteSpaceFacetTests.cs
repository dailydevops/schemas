namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

public class WhiteSpaceFacetTests
{
    [Test]
    [Arguments(WhitespaceMode.Preserve, "a\tb  c ", true)]
    [Arguments(WhitespaceMode.Replace, "a b  c ", true)]
    [Arguments(WhitespaceMode.Replace, "a\tb", false)]
    [Arguments(WhitespaceMode.Replace, "a\nb", false)]
    [Arguments(WhitespaceMode.Collapse, "a b c", true)]
    [Arguments(WhitespaceMode.Collapse, "a  b", false)]
    [Arguments(WhitespaceMode.Collapse, " a", false)]
    [Arguments(WhitespaceMode.Collapse, "a ", false)]
    [Arguments(WhitespaceMode.Collapse, "", true)]
    public async Task IsSatisfiedBy_ChecksTheNormalForm(WhitespaceMode mode, string lexical, bool expected)
    {
        var facet = new WhiteSpaceFacet(mode);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.WhiteSpace);
        await Assert.That(facet.Mode).IsEqualTo(mode);
        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }
}
