namespace NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Substitution;

public class SubstitutionGroupTests
{
    private const XsdDerivationMethod Ext = XsdDerivationMethod.Extension;
    private const XsdDerivationMethod Res = XsdDerivationMethod.Restriction;

    private static readonly FakeType Base = new("base");
    private static readonly FakeType ByExt = new("byExt", Base, Ext);
    private static readonly FakeType ByRes = new("byRes", Base, Res);
    private static readonly FakeType Deep = new("deep", ByExt, Ext);

    private static (SubstitutionGroups Groups, IReadOnlyList<ValidationIssue> Issues) Build(
        params FakeElement[] elements
    )
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var groups = SubstitutionGroups.Build(elements, new ChainDerivationCheck(), new XsdIssueReporter(collector));
        return (groups, collector.ToResult().Issues);
    }

    private static string Names(IReadOnlyList<ISubstitutionElement> list)
    {
        var names = new string[list.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = list[i].Name.LocalName;
        }

        return string.Join(",", names);
    }

    [Test]
    public async Task Members_MemberOfMember_IsMemberOfHead_InStableOrder()
    {
        var head = new FakeElement("head", Base);
        var mid = new FakeElement("mid", ByExt, head);
        var leaf = new FakeElement("leaf", Deep, mid);
        var other = new FakeElement("other", ByRes, head);

        var (groups, issues) = Build(leaf, other, mid, head);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,leaf,other,mid");
        await Assert.That(Names(groups.GetMembers(mid))).IsEqualTo("mid,leaf");
        await Assert.That(Names(groups.GetMembers(leaf))).IsEqualTo("leaf");
    }

    [Test]
    public async Task Cycle_IsLocatedSchemaError_AndNoMembers()
    {
        var a = new FakeElement("a", Base, line: 3);
        var b = new FakeElement("b", Base, a, line: 4);
        a.SubstitutionGroup = b;

        var (groups, issues) = Build(a, b);

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(issues[0].Location).IsEqualTo(a.Location);
        await Assert.That(issues[1].Location).IsEqualTo(b.Location);
        await Assert.That(Names(groups.GetMembers(a))).IsEqualTo("a");
    }

    [Test]
    public async Task Block_Substitution_RemovesAllMembers()
    {
        var head = new FakeElement("head", Base) { Block = XsdDerivationMethod.Substitution };
        var member = new FakeElement("member", ByExt, head);

        var (groups, issues) = Build(head, member);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
    }

    [Test]
    [Arguments(1, "byRes")]
    [Arguments(2, "byExt")]
    public async Task Block_ElementDerivationMethod_RemovesMembersDerivedThatWay(int blockValue, string survivor)
    {
        var block = (XsdDerivationMethod)blockValue;
        var head = new FakeElement("head", Base) { Block = block };
        var viaExt = new FakeElement("byExt", ByExt, head);
        var viaRes = new FakeElement("byRes", ByRes, head);

        var (groups, issues) = Build(head, viaExt, viaRes);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head," + survivor);
    }

    [Test]
    public async Task Block_OnTheHeadType_RemovesMembersDerivedThatWay()
    {
        var blocking = new FakeType("blocking") { Block = Res };
        var byExt = new FakeType("e", blocking, Ext);
        var byRes = new FakeType("r", blocking, Res);
        var head = new FakeElement("head", blocking);
        var e = new FakeElement("e", byExt, head);
        var r = new FakeElement("r", byRes, head);

        var (groups, _) = Build(head, e, r);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,e");
    }

    [Test]
    public async Task Final_MatchingTheDerivation_IsLocatedSchemaError_AndNotAMember()
    {
        var head = new FakeElement("head", Base) { Final = Ext };
        var bad = new FakeElement("bad", ByExt, head, line: 9);
        var good = new FakeElement("good", ByRes, head);

        var (groups, issues) = Build(head, bad, good);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(issues[0].Location).IsEqualTo(bad.Location);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,good");
    }

    [Test]
    public async Task TypeNotDerivedFromHeadType_IsLocatedSchemaError()
    {
        var head = new FakeElement("head", Base);
        var stray = new FakeElement("stray", new FakeType("unrelated"), head, line: 5);

        var (groups, issues) = Build(head, stray);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location).IsEqualTo(stray.Location);
        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head");
    }

    [Test]
    public async Task AbstractHead_IsNotAnInstanceElement_WhileMembersAre()
    {
        var head = new FakeElement("head", Base) { IsAbstract = true };
        var member = new FakeElement("member", ByExt, head);

        var (groups, _) = Build(head, member);

        await Assert.That(Names(groups.GetMembers(head))).IsEqualTo("head,member");
        await Assert.That(Names(groups.GetInstanceElements(head))).IsEqualTo("member");
    }

    [Test]
    public async Task LeadsIntoCycle_IsNotAMember_AndOnlyTheCycleIsReported()
    {
        var a = new FakeElement("a", Base);
        var b = new FakeElement("b", Base, a);
        a.SubstitutionGroup = b;
        var tail = new FakeElement("tail", Base, a);

        var (groups, issues) = Build(a, b, tail);

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(Names(groups.GetMembers(a))).IsEqualTo("a");
    }

    [Test]
    public async Task Guards_NullArguments_Throw()
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var check = new ChainDerivationCheck();
        var groups = SubstitutionGroups.Build([], check, reporter);

        await Assert.That(() => SubstitutionGroups.Build(null!, check, reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => SubstitutionGroups.Build([], null!, reporter)).Throws<ArgumentNullException>();
        await Assert.That(() => SubstitutionGroups.Build([], check, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => groups.GetMembers(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => groups.GetInstanceElements(null!)).Throws<ArgumentNullException>();
    }
}
