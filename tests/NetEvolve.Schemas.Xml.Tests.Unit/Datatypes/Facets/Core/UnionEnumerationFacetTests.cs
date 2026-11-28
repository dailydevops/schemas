namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

public class UnionEnumerationFacetTests
{
    private static IDatatypeDescriptor Type(string name) => BuiltInDatatypes.For(XsdVersion.V11).Get(name);

    private static UnionDescriptor Union(params string[] members) => new("union", [.. members.Select(Type)]);

    [Test]
    [Arguments("1", true)]
    [Arguments("+01", true)]
    [Arguments("2", true)]
    [Arguments("3", false)]
    [Arguments("1.0", false)]
    [Arguments("2.0", false)]
    [Arguments("x", false)]
    [Arguments("", false)]
    public async Task Integer_ThenDecimal_ComparesInTheValueSpaceOfTheAcceptingMember(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("integer", "decimal"), ["1", "2"]);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.Enumeration);
        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1", true)]
    [Arguments("1.0", true)]
    [Arguments("1.50", false)]
    [Arguments("2", false)]
    public async Task Decimal_ThenInteger_AcceptsEveryValueOfTheFirstMember(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("decimal", "integer"), ["1"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("float", "1.5", "1.50", true)]
    [Arguments("float", "1.5", "2", false)]
    [Arguments("double", "1.5", "1.50E0", true)]
    [Arguments("double", "1.5", "2", false)]
    [Arguments("duration", "P1D", "PT24H", true)]
    [Arguments("duration", "P1D", "PT25H", false)]
    [Arguments("duration", "P1D", "P1D", true)]
    [Arguments("dateTime", "2001-01-01T00:00:00Z", "2001-01-01T00:00:00Z", true)]
    [Arguments("dateTime", "2001-01-01T00:00:00Z", "2001-01-01T00:00:01Z", false)]
    [Arguments("boolean", "true", "1", true)]
    [Arguments("boolean", "true", "false", false)]
    public async Task OrderedMembers_CompareInTheirValueSpace(
        string member,
        string listed,
        string lexical,
        bool expected
    )
    {
        var facet = new UnionEnumerationFacet(Union(member, "string"), [listed]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("true", true)]
    [Arguments("1", true)]
    [Arguments("TRUE", false)]
    [Arguments("0", false)]
    [Arguments("other", true)]
    [Arguments("tru", false)]
    public async Task Boolean_ThenString_ADifferentAcceptingMemberIsNotEqual(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("boolean", "string"), ["true", "1", "other"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("abc", true)]
    [Arguments("abd", false)]
    [Arguments(" abc", false)]
    [Arguments("12", false)]
    public async Task String_ComparesOrdinally(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("string", "integer"), ["abc"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("a b", true)]
    [Arguments("  a   b ", true)]
    [Arguments("a  c", false)]
    public async Task Token_CollapsesWhitespaceBeforeItComparesOrdinally(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("token", "integer"), [" a  b "]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("0A", true)]
    [Arguments(" 0A ", true)]
    [Arguments("0a", false)]
    public async Task OtherMembers_CompareTheCollapsedLexicalForm(string lexical, bool expected)
    {
        var facet = new UnionEnumerationFacet(Union("hexBinary", "integer"), ["0A"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1 2", true)]
    [Arguments(" 1   2 ", true)]
    [Arguments("2 1", false)]
    [Arguments("01 2", false)]
    [Arguments("7", false)]
    public async Task ListMember_ComparesTheCollapsedLexicalForm(string lexical, bool expected)
    {
        var list = new ListDescriptor("integers", Type("integer"));
        var facet = new UnionEnumerationFacet(new UnionDescriptor("union", [list, Type("boolean")]), ["1 2"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1", true)]
    [Arguments("2", false)]
    [Arguments("true", false)]
    public async Task UnionMember_ComparesTheLexicalForm(string lexical, bool expected)
    {
        var inner = Union("integer", "string");
        var facet = new UnionEnumerationFacet(new UnionDescriptor("union", [inner, Type("boolean")]), ["1"]);

        await Assert.That(facet.IsSatisfiedBy(lexical.AsSpan())).IsEqualTo(expected);
    }

    [Test]
    public async Task ListedValue_OfNoMember_IsLeftOut()
    {
        var facet = new UnionEnumerationFacet(Union("integer", "boolean"), ["x", "5"]);

        await Assert.That(facet.IsSatisfiedBy("5".AsSpan())).IsTrue();
        await Assert.That(facet.IsSatisfiedBy("x".AsSpan())).IsFalse();
    }

    [Test]
    public async Task EmptyEnumeration_AcceptsNothing()
    {
        var facet = new UnionEnumerationFacet(Union("integer", "string"), []);

        await Assert.That(facet.IsSatisfiedBy("1".AsSpan())).IsFalse();
        await Assert.That(facet.IsSatisfiedBy("a".AsSpan())).IsFalse();
    }

    [Test]
    public async Task Constructor_RejectsNull()
    {
        await Assert.That(() => new UnionEnumerationFacet(null!, ["1"])).Throws<ArgumentNullException>();
        await Assert.That(() => new UnionEnumerationFacet(Union("integer"), null!)).Throws<ArgumentNullException>();
    }
}
