namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class SubstitutionThroughTheSeamTests
{
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;

    private readonly TypeFixture _f = new();

    private SubstitutionGroups Build(params TypeElement[] elements) =>
        SubstitutionGroups.Build(elements, TypeFixture.Checker(), _f.Reporter);

    private static string Names(IReadOnlyList<ISubstitutionElement> members)
    {
        var names = new string[members.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = members[i].Name.LocalName;
        }

        return string.Join(",", names);
    }

    [Test]
    public async Task Members_WhoseTypesAreDerivedFromTheHeadType_AreSubstitutable()
    {
        var a = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var byExtension = _f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var byRestriction = _f.Complex("c", Res, a, TypeFixture.Sequence(TypeFixture.Element("x")));
        var head = new TypeElement("head", a);
        var first = new TypeElement("first", byExtension, head);
        var second = new TypeElement("second", byRestriction, head);

        var groups = Build(head, first, second);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,first,second");
        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }

    [Test]
    [Arguments(0, "head,first,second")]
    [Arguments(1, "head,second")]
    [Arguments(2, "head,first")]
    [Arguments(3, "head")]
    public async Task HeadBlock_ExcludesTheMembersWhoseTypeIsDerivedByABlockedMethod(int block, string expected)
    {
        var a = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var byExtension = _f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var byRestriction = _f.Complex("c", Res, a, TypeFixture.Sequence(TypeFixture.Element("x")));
        var head = new TypeElement("head", a) { Block = (XsdDerivationMethod)block };
        var first = new TypeElement("first", byExtension, head);
        var second = new TypeElement("second", byRestriction, head);

        var groups = Build(head, first, second);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo(expected);
    }

    [Test]
    public async Task TypeBlock_ExcludesTheMembersWhoseTypeIsDerivedByABlockedMethod()
    {
        var a = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")), block: Res);
        var byExtension = _f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var byRestriction = _f.Complex("c", Res, a, TypeFixture.Sequence(TypeFixture.Element("x")));
        var head = new TypeElement("head", a);

        var groups = Build(
            head,
            new TypeElement("first", byExtension, head),
            new TypeElement("second", byRestriction, head)
        );

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,first");
    }

    [Test]
    public async Task Member_WhoseTypeIsNotDerivedFromTheHeadType_IsALocatedSchemaError()
    {
        var head = new TypeElement("head", _f.Root("a"));
        var stranger = new TypeElement("stranger", _f.Root("other"), head);

        var groups = Build(head, stranger);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
        await Assert.That(_f.Issues).Count().IsEqualTo(1);
        await Assert.That(FakeRules.Find(_f.Issues[0].Code).Id).IsEqualTo("cos-equiv-derived-ok-rec");
    }

    [Test]
    public async Task HeadFinal_RejectsAMemberWhoseTypeIsDerivedByAFinalMethod()
    {
        var a = _f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var byExtension = _f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var head = new TypeElement("head", a) { Final = Ext };

        var groups = Build(head, new TypeElement("first", byExtension, head));

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
        await Assert.That(_f.Issues).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Member_OfAHeadWithTheUrType_IsAlwaysDerived()
    {
        var head = new TypeElement("head", TypeFixture.Registered("anyType"));

        var groups = Build(
            head,
            new TypeElement("first", _f.Root("a"), head),
            new TypeElement("simple", TypeFixture.Xs("int"), head)
        );

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,first,simple");
        await Assert.That(_f.Issues).Count().IsEqualTo(0);
    }
}
