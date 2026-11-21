namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;
using NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;

public class SubstitutionGroups11Tests
{
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;

    private static readonly FakeType Base = new("base");
    private static readonly FakeType ByExt = new("byExt", Base, Ext);
    private static readonly FakeType ByRes = new("byRes", Base, Res);
    private static readonly FakeType Deep = new("deep", ByExt, Ext);
    private static readonly FakeType Unrelated = new("unrelated");

    private static (SubstitutionGroups11 Groups, IReadOnlyList<ValidationIssue> Issues) Build(
        params MultiHeadElement[] elements
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var groups = SubstitutionGroups11.Build(elements, new ChainDerivationCheck(), new XsdIssueReporter(collector));
        return (groups, collector.ToResult().Issues);
    }

    private static string Names(IReadOnlyList<IMultiHeadElement> list)
    {
        var names = new string[list.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = list[i].Name.LocalName;
        }

        return string.Join(",", names);
    }

    [Test]
    public async Task Members_AnElementWithTwoHeads_IsAMemberOfBothGroups_WithRealTypes()
    {
        var f = new TypeFixture();
        var a = f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var b = f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var c = f.Complex("c", Ext, b, TypeFixture.Sequence(TypeFixture.Element("z")));
        var first = new MultiHeadElement("first", a);
        var second = new MultiHeadElement("second", b);
        var both = new MultiHeadElement("both", c, 5, first, second);

        var groups = SubstitutionGroups11.Build([first, second, both], TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(first))).IsEqualTo("first,both");
        await Assert.That(Names(groups.GetMembers(second))).IsEqualTo("second,both");
        await Assert.That(Names(groups.GetInstanceElements(first))).IsEqualTo("first,both");
        await Assert.That(Names(groups.GetInstanceElements(second))).IsEqualTo("second,both");
    }

    [Test]
    public async Task Members_SimpleTypes_AreNotBlockedByTheirOwnBlockProperty()
    {
        var f = new TypeFixture();
        var text = TypeFixture.Xs("string");
        var middle = f.Restrict("middle", text);
        var deep = f.Restrict("deep", middle);
        var first = new MultiHeadElement("first", text);
        var second = new MultiHeadElement("second", middle);
        var both = new MultiHeadElement("both", deep, 5, first, second);

        var groups = SubstitutionGroups11.Build([first, second, both], TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(first))).IsEqualTo("first,both");
        await Assert.That(Names(groups.GetMembers(second))).IsEqualTo("second,both");
    }

    [Test]
    public async Task Members_ATypeNotDerivedFromOneHead_IsALocatedSchemaError_WithRealTypes()
    {
        var f = new TypeFixture();
        var a = f.Root("a", TypeFixture.Sequence(TypeFixture.Element("x")));
        var b = f.Complex("b", Ext, a, TypeFixture.Sequence(TypeFixture.Element("y")));
        var other = f.Root("other", TypeFixture.Sequence(TypeFixture.Element("q")));
        var good = new MultiHeadElement("good", a);
        var bad = new MultiHeadElement("bad", other);
        var element = new MultiHeadElement("element", b, 9, good, bad);

        var groups = SubstitutionGroups11.Build([good, bad, element], TypeFixture.Checker(), f.Reporter);

        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(f.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(f.Issues[0].Location).IsEqualTo(element.Location);
        await Assert.That(f.Issues[0].Message).Contains("bad");
        await Assert.That(Names(groups.GetMembers(good))).IsEqualTo("good,element");
        await Assert.That(Names(groups.GetMembers(bad))).IsEqualTo("bad");
    }

    [Test]
    public async Task Members_MemberOfMember_IsMemberOfEveryHeadOfEveryHead_InStableOrder()
    {
        var h1 = new MultiHeadElement("h1", Base);
        var h2 = new MultiHeadElement("h2", Base);
        var mid = new MultiHeadElement("mid", ByExt, 1, h1, h2);
        var leaf = new MultiHeadElement("leaf", Deep, 1, mid);

        var (groups, issues) = Build(leaf, mid, h2, h1);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(h1))).IsEqualTo("h1,leaf,mid");
        await Assert.That(Names(groups.GetMembers(h2))).IsEqualTo("h2,leaf,mid");
        await Assert.That(Names(groups.GetMembers(mid))).IsEqualTo("mid,leaf");
        await Assert.That(Names(groups.GetMembers(leaf))).IsEqualTo("leaf");
    }

    [Test]
    public async Task Members_AHeadReachedOnTwoPaths_ListsTheMemberOnce()
    {
        var head = new MultiHeadElement("head", Base);
        var left = new MultiHeadElement("left", ByExt, 1, head);
        var right = new MultiHeadElement("right", Base, 1, head);
        var bottom = new MultiHeadElement("bottom", Deep, 1, left, right);

        var (groups, issues) = Build(head, left, right, bottom);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,left,right,bottom");
        await Assert.That(Names(groups.GetMembers(left))).IsEqualTo("left,bottom");
        await Assert.That(Names(groups.GetMembers(right))).IsEqualTo("right,bottom");
    }

    [Test]
    public async Task Members_ADuplicateHead_ListsTheMemberOnce()
    {
        var head = new MultiHeadElement("head", Base);
        var member = new MultiHeadElement("member", ByExt, 1, head, head);

        var (groups, issues) = Build(head, member);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,member");
    }

    [Test]
    public async Task Members_ATypeNotDerivedFromOneHead_KeepsTheMembershipOfTheOtherHead()
    {
        var good = new MultiHeadElement("good", Base);
        var bad = new MultiHeadElement("bad", Unrelated, 2);
        var member = new MultiHeadElement("member", ByExt, 3, bad, good);

        var (groups, issues) = Build(good, bad, member);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location).IsEqualTo(member.Location);
        await Assert.That(Names(groups.GetMembers(good))).IsEqualTo("good,member");
        await Assert.That(Names(groups.GetMembers(bad))).IsEqualTo("bad");
    }

    [Test]
    public async Task Members_FinalOfOneHead_IsASchemaErrorForThatHeadOnly()
    {
        var open = new MultiHeadElement("open", Base);
        var closed = new MultiHeadElement("closed", Base) { Final = Ext };
        var member = new MultiHeadElement("member", ByExt, 4, open, closed);

        var (groups, issues) = Build(open, closed, member);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(issues[0].Location).IsEqualTo(member.Location);
        await Assert.That(issues[0].Message).Contains("closed");
        await Assert.That(Names(groups.GetMembers(open))).IsEqualTo("open,member");
        await Assert.That(Names(groups.GetMembers(closed))).IsEqualTo("closed");
    }

    [Test]
    public async Task Members_MemberOfAnInvalidMid_IsStillMemberOfMidAndHead()
    {
        var head = new MultiHeadElement("head", Base) { Final = Ext };
        var mid = new MultiHeadElement("mid", ByExt, 2, head);
        var leaf = new MultiHeadElement("leaf", Deep, 3, mid);

        var (groups, issues) = Build(head, mid, leaf);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location).IsEqualTo(mid.Location);
        await Assert.That(Names(groups.GetMembers(mid))).IsEqualTo("mid,leaf");
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,leaf");
    }

    [Test]
    public async Task Cycle_ThroughTheSecondHead_IsReportedAtEveryElementOnIt_AndNoneIsAMember()
    {
        var head = new MultiHeadElement("head", Base);
        var a = new MultiHeadElement("a", Base, 3, head);
        var b = new MultiHeadElement("b", Base, 4, a);
        a.AddHead(b);

        var (groups, issues) = Build(head, a, b);

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(issues[0].Location).IsEqualTo(a.Location);
        await Assert.That(issues[1].Location).IsEqualTo(b.Location);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
        await Assert.That(Names(groups.GetMembers(a))).IsEqualTo("a");
    }

    [Test]
    public async Task Cycle_AnElementThatNamesItself_IsReported()
    {
        var a = new MultiHeadElement("a", Base, 6);
        a.AddHead(a);

        var (groups, issues) = Build(a);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location).IsEqualTo(a.Location);
        await Assert.That(Names(groups.GetMembers(a))).IsEqualTo("a");
    }

    [Test]
    public async Task Cycle_AnElementThatLeadsIntoACycle_IsNotAMemberAndNotReported()
    {
        var a = new MultiHeadElement("a", Base, 3);
        var b = new MultiHeadElement("b", Base, 4, a);
        a.AddHead(b);
        var leads = new MultiHeadElement("leads", ByExt, 5, a);

        var (groups, issues) = Build(a, b, leads);

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(Names(groups.GetMembers(a))).IsEqualTo("a");
        await Assert.That(Names(groups.GetMembers(b))).IsEqualTo("b");
    }

    [Test]
    public async Task Block_SubstitutionOfOneHead_ExcludesTheMemberFromThatGroupWithoutAnError()
    {
        var open = new MultiHeadElement("open", Base);
        var blocked = new MultiHeadElement("blocked", Base) { Block = XsdDerivationMethod.Substitution };
        var member = new MultiHeadElement("member", ByExt, 1, open, blocked);

        var (groups, issues) = Build(open, blocked, member);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(open))).IsEqualTo("open,member");
        await Assert.That(Names(groups.GetMembers(blocked))).IsEqualTo("blocked");
    }

    [Test]
    [Arguments(1, "head,byRes")]
    [Arguments(2, "head,byExt")]
    [Arguments(3, "head")]
    public async Task Block_OfAHeadElement_ExcludesTheMembersWhoseTypeIsDerivedByABlockedMethod(
        int block,
        string expected
    )
    {
        var head = new MultiHeadElement("head", Base) { Block = (XsdDerivationMethod)block };
        var other = new MultiHeadElement("other", Base);
        var viaExt = new MultiHeadElement("byExt", ByExt, 1, head, other);
        var viaRes = new MultiHeadElement("byRes", ByRes, 1, head, other);

        var (groups, issues) = Build(head, other, viaExt, viaRes);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo(expected);
        await Assert.That(Names(groups.GetMembers(other))).IsEqualTo("other,byExt,byRes");
    }

    [Test]
    public async Task Block_OfTheHeadType_ExcludesTheMembersWhoseTypeIsDerivedByABlockedMethod()
    {
        var blockingBase = new FakeType("blockingBase") { Block = Ext };
        var derived = new FakeType("derived", blockingBase, Ext);
        var head = new MultiHeadElement("head", blockingBase);
        var member = new MultiHeadElement("member", derived, 1, head);

        var (groups, issues) = Build(head, member);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
    }

    [Test]
    public async Task InstanceElements_LeaveOutAbstractMembersAndAbstractHeads()
    {
        var head = new MultiHeadElement("head", Base) { IsAbstract = true };
        var other = new MultiHeadElement("other", Base);
        var concrete = new MultiHeadElement("concrete", ByExt, 1, head, other);
        var hidden = new MultiHeadElement("hidden", ByRes, 1, head, other) { IsAbstract = true };

        var (groups, _) = Build(head, other, concrete, hidden);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,concrete,hidden");
        await Assert.That(Names(groups.GetInstanceElements(head))).IsEqualTo("concrete");
        await Assert.That(Names(groups.GetInstanceElements(other))).IsEqualTo("other,concrete");
    }

    [Test]
    public async Task Members_ADeclarationWithoutHeads_IsItsOwnGroup()
    {
        var alone = new MultiHeadElement("alone", Base);

        var (groups, issues) = Build(alone);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(alone))).IsEqualTo("alone");
        await Assert.That(Names(groups.GetInstanceElements(alone))).IsEqualTo("alone");
    }

    [Test]
    public async Task Build_NullArguments_Throw()
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var check = new ChainDerivationCheck();

        await Assert.That(() => SubstitutionGroups11.Build(null!, check, reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => SubstitutionGroups11.Build([], null!, reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => SubstitutionGroups11.Build([], check, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Members_NullHead_Throws()
    {
        var (groups, _) = Build();

        await Assert.That(() => groups.GetMembers(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => groups.GetInstanceElements(null!)).Throws<ArgumentNullException>();
    }
}
