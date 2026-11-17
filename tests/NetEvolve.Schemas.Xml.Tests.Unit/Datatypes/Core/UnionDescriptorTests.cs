namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class UnionDescriptorTests
{
    private static AtomicDescriptor<int> Digits() => new("digits", XsdVersion.V11, new FakeDigitsMapping());

    private static AtomicDescriptor<int> Small() => new("small", Digits(), null, [new FakeMaxInclusiveFacet(9)]);

    private static UnionDescriptor Union(params IDatatypeDescriptor[] members) => new("u", members);

    [Test]
    public async Task Parse_ReturnsTheFirstAcceptingMemberInDeclarationOrder()
    {
        var narrowFirst = Union(Small(), Digits());
        var wideFirst = Union(Digits(), Small());

        await Assert.That(narrowFirst.Parse("5").Value.MemberIndex).IsEqualTo(0);
        await Assert.That(narrowFirst.Parse("50").Value.MemberIndex).IsEqualTo(1);
        await Assert.That(wideFirst.Parse("5").Value.MemberIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_KeepsTheRangeOfTheWholeLexicalForm()
    {
        var result = Union(Digits()).Parse(" 5 ");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value.Range).IsEqualTo(new TextRange(0, 3));
    }

    [Test]
    public async Task Parse_NoMemberAccepts_ReportsNoUnionMember()
    {
        var union = Union(Small(), Digits());

        var result = union.Parse("x");
        var validation = union.Validate("x");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.NoUnionMember);
        await Assert.That(result.ErrorOffset).IsEqualTo(0);
        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Error).IsEqualTo(DatatypeError.NoUnionMember);
        await Assert.That(union.TryGetMemberIndex("x", out var index)).IsFalse();
        await Assert.That(index).IsEqualTo(-1);
    }

    [Test]
    public async Task Validate_AcceptedByAnyMember_IsValid() =>
        await Assert.That(Union(Small(), Digits()).Validate("50").IsValid).IsTrue();

    [Test]
    public async Task Shape_ReflectsTheMembers()
    {
        var union = Union(Small(), Digits());

        await Assert.That(union.Variety).IsEqualTo(DatatypeVariety.Union);
        await Assert.That(union.Members.Count).IsEqualTo(2);
        await Assert.That(union.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(union.IsOrdered).IsTrue();
        await Assert.That(union.Facets.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Shape_UnorderedMember_MakesTheUnionUnordered()
    {
        _ = BuiltInDatatypes.For(XsdVersion.V11).TryGet("anySimpleType", out var text);

        await Assert.That(Union(Digits(), text!).IsOrdered).IsFalse();
    }

    [Test]
    public async Task Constructor_RejectsNullAndEmptyMembers()
    {
        await Assert.That(() => new UnionDescriptor("u", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new UnionDescriptor("u", [])).Throws<ArgumentOutOfRangeException>();
    }
}
