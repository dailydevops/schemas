namespace NetEvolve.Schemas.Schematron.Tests.Unit.Expansion.AbstractRules;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Expansion.AbstractRules;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class AbstractRuleExpanderTests
{
    private static DocumentLocation At(int line) => new("rules.sch", line, 1);

    private static SchematronAssert Assert1(string id, int line) =>
        new(At(line), "true()", id, [], null, null, null, [], [], FakeModel.Message(id));

    private static SchematronLet Let(string name, int line) => new(At(line), name, null, "1");

    private static SchematronExtends Extends(string? rule, int line, string? href = null) => new(At(line), rule, href);

    private static SchematronRule Rule(
        string? id,
        int line,
        bool isAbstract = false,
        SchematronLet[]? lets = null,
        params SchematronNode[] items
    ) => new(At(line), id, isAbstract ? null : "ctx", null, [], null, null, isAbstract, null, lets ?? [], items, null);

    private static SchematronPattern Pattern(int line, params SchematronRule[] rules) =>
        new(At(line), null, false, null, null, null, null, [], [], [], rules, null);

    private static SchematronRules Container(params SchematronRule[] rules) => new(At(1), "shared", null, [], rules);

    private static (bool Ok, IReadOnlyList<SchematronPatternBase> Patterns, FakeProblemSink Problems) Run(
        SchematronRules[] containers,
        params SchematronPatternBase[] patterns
    )
    {
        var problems = new FakeProblemSink();
        var ok = AbstractRuleExpander.TryExpand(containers, patterns, problems, out var expanded);
        return (ok, expanded, problems);
    }

    private static string[] Ids(SchematronRule rule)
    {
        var ids = new List<string>();
        foreach (var item in rule.Items)
        {
            ids.Add(((SchematronAssertion)item).Id!);
        }
        return ids.ToArray();
    }

    [Test]
    public async Task Extends_AbstractRuleById_CopiesAssertionsAtExtendsPosition()
    {
        var abstractRule = Rule("shared", 2, true, null, Assert1("a1", 3), Assert1("a2", 4));
        var concrete = Rule("c", 5, false, null, Assert1("own1", 6), Extends("shared", 7), Assert1("own2", 8));

        var (ok, patterns, problems) = Run([], Pattern(1, abstractRule, concrete));

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(Ids(patterns[0].Rules[0])).IsEquivalentTo(["own1", "a1", "a2", "own2"]);
    }

    [Test]
    public async Task Extends_AbstractRuleLets_AreCopiedBeforeOwnLets()
    {
        var abstractRule = Rule("shared", 2, true, [Let("inherited", 3)], Assert1("a1", 4));
        var concrete = Rule("c", 5, false, [Let("own", 6)], Extends("shared", 7));

        var (_, patterns, _) = Run([], Pattern(1, abstractRule, concrete));

        var lets = patterns[0].Rules[0].Lets;
        await Assert.That(lets.Count).IsEqualTo(2);
        await Assert.That(lets[0].Name).IsEqualTo("inherited");
        await Assert.That(lets[1].Name).IsEqualTo("own");
    }

    [Test]
    public async Task Extends_CopiedAssertion_KeepsDefinitionLocation()
    {
        var abstractRule = Rule("shared", 2, true, null, Assert1("a1", 3));
        var concrete = Rule("c", 5, false, null, Extends("shared", 7));

        var (_, patterns, _) = Run([], Pattern(1, abstractRule, concrete));

        await Assert.That(patterns[0].Rules[0].Items[0].Location).IsEqualTo(At(3));
    }

    [Test]
    public async Task Extends_AbstractRuleInRulesContainer_IsFoundFromAnyPattern()
    {
        var container = Container(Rule("shared", 2, true, null, Assert1("a1", 3)));
        var first = Pattern(10, Rule("c1", 11, false, null, Extends("shared", 12)));
        var second = Pattern(20, Rule("c2", 21, false, null, Extends("shared", 22)));

        var (ok, patterns, _) = Run([container], first, second);

        await Assert.That(ok).IsTrue();
        await Assert.That(Ids(patterns[0].Rules[0])).IsEquivalentTo(["a1"]);
        await Assert.That(Ids(patterns[1].Rules[0])).IsEquivalentTo(["a1"]);
    }

    [Test]
    public async Task Extends_ChainOverTwoAbstractRules_ExpandsFully()
    {
        var container = Container(
            Rule("outer", 2, true, null, Assert1("o1", 3), Extends("inner", 4)),
            Rule("inner", 5, true, [Let("deep", 6)], Assert1("i1", 7))
        );
        var concrete = Rule("c", 10, false, null, Extends("outer", 11));

        var (ok, patterns, _) = Run([container], Pattern(9, concrete));

        await Assert.That(ok).IsTrue();
        await Assert.That(Ids(patterns[0].Rules[0])).IsEquivalentTo(["o1", "i1"]);
        await Assert.That(patterns[0].Rules[0].Lets[0].Name).IsEqualTo("deep");
    }

    [Test]
    public async Task Extends_SameAbstractRuleTwice_CopiesBothTimes()
    {
        var abstractRule = Rule("shared", 2, true, null, Assert1("a1", 3));
        var concrete = Rule("c", 5, false, null, Extends("shared", 6), Extends("shared", 7));

        var (ok, patterns, _) = Run([], Pattern(1, abstractRule, concrete));

        await Assert.That(ok).IsTrue();
        await Assert.That(Ids(patterns[0].Rules[0])).IsEquivalentTo(["a1", "a1"]);
    }

    [Test]
    public async Task Extends_Cycle_ReportsAbstractTargetInvalidAtExtendsLocation()
    {
        var container = Container(Rule("a", 2, true, null, Extends("b", 3)), Rule("b", 4, true, null, Extends("a", 5)));
        var concrete = Rule("c", 10, false, null, Extends("a", 11));

        var (ok, _, problems) = Run([container], Pattern(9, concrete));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AbstractTargetInvalid);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(5));
    }

    [Test]
    public async Task Extends_RuleExtendingItself_ReportsCycle()
    {
        var container = Container(Rule("self", 2, true, null, Extends("self", 3)));
        var concrete = Rule("c", 10, false, null, Extends("self", 11));

        var (ok, _, problems) = Run([container], Pattern(9, concrete));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AbstractTargetInvalid);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(3));
    }

    [Test]
    public async Task Extends_UnknownId_ReportsIdrefUnresolvedAtExtendsLocation()
    {
        var concrete = Rule("c", 10, false, null, Extends("missing", 11));

        var (ok, _, problems) = Run([], Pattern(9, concrete));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(11));
        await Assert.That(problems.Problems[0].Message).Contains("missing");
    }

    [Test]
    public async Task Extends_ConcreteTarget_ReportsAbstractTargetInvalid()
    {
        var target = Rule("target", 2, false, null, Assert1("a1", 3));
        var concrete = Rule("c", 10, false, null, Extends("target", 11));

        var (ok, _, problems) = Run([], Pattern(1, target, concrete));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AbstractTargetInvalid);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(11));
    }

    [Test]
    public async Task Extends_ProblemsInTwoRules_AreAllReported()
    {
        var one = Rule("one", 10, false, null, Extends("nope1", 11));
        var two = Rule("two", 20, false, null, Extends("nope2", 21));

        var (ok, _, problems) = Run([], Pattern(9, one, two));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AbstractRule_NeverFiresOnItsOwn_IsRemovedFromTheExpandedPattern()
    {
        var abstractRule = Rule("shared", 2, true, null, Assert1("a1", 3));
        var concrete = Rule("c", 5, false, null, Assert1("own", 6));

        var (ok, patterns, _) = Run([], Pattern(1, abstractRule, concrete));

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns[0].Rules.Count).IsEqualTo(1);
        await Assert.That(patterns[0].Rules[0].IsAbstract).IsFalse();
        await Assert.That(patterns[0].Rules[0].Id).IsEqualTo("c");
    }

    [Test]
    public async Task Extends_HrefOnly_IsLeftToTheInclusionStage()
    {
        var concrete = Rule("c", 5, false, null, Extends(null, 6, "other.sch"));

        var (ok, patterns, _) = Run([], Pattern(1, concrete));

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns[0].Rules[0].Items.Count).IsEqualTo(1);
        await Assert.That(patterns[0].Rules[0].Items[0]).IsTypeOf<SchematronExtends>();
    }

    [Test]
    public async Task Expand_Group_ExpandsItsRulesToo()
    {
        var container = Container(Rule("shared", 2, true, null, Assert1("a1", 3)));
        var group = new SchematronGroup(
            At(9),
            "g",
            false,
            null,
            null,
            null,
            null,
            [],
            [],
            [],
            [Rule("c", 10, false, null, Extends("shared", 11))],
            null
        );

        var (ok, patterns, _) = Run([container], group);

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns[0]).IsTypeOf<SchematronGroup>();
        await Assert.That(patterns[0].Id).IsEqualTo("g");
        await Assert.That(Ids(patterns[0].Rules[0])).IsEquivalentTo(["a1"]);
    }

    [Test]
    public async Task Expand_PatternWithoutExtends_IsReturnedUnchanged()
    {
        var pattern = Pattern(1, Rule("c", 2, false, null, Assert1("a", 3)));

        var (ok, patterns, _) = Run([], pattern);

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns[0]).IsSameReferenceAs(pattern);
    }

    [Test]
    public async Task TryExpand_NullArguments_Throw()
    {
        var problems = new FakeProblemSink();

        await Assert
            .That(() => AbstractRuleExpander.TryExpand(null!, [], problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AbstractRuleExpander.TryExpand([], null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert.That(() => AbstractRuleExpander.TryExpand([], [], null!, out _)).Throws<ArgumentNullException>();
    }
}
