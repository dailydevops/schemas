namespace NetEvolve.Schemas.Schematron.Tests.Unit.Expansion.AbstractPatterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class AbstractPatternExpanderTests
{
    private static DocumentLocation At(int line) => new("rules.sch", line, 1);

    private static SchematronParameter Param(string name, string? value, int line) => new(At(line), name, value);

    private static SchematronLet Let(string name, string value, int line) => new(At(line), name, null, value);

    private static SchematronMessage Message(params SchematronMessagePart[] parts) => new(At(1), parts);

    private static SchematronText Text(string text) => new(At(1), text);

    private static SchematronValueOf ValueOf(string select) => new(At(1), select);

    private static SchematronAssert Assert1(
        string test,
        string? role = null,
        string? subject = null,
        params SchematronMessagePart[] parts
    ) => new(At(30), test, "a1", ["f"], role, null, subject, [], [], Message(parts));

    private static SchematronRule Rule(
        string context,
        SchematronLet[]? lets = null,
        string? subject = null,
        string? role = null,
        params SchematronNode[] items
    ) => new(At(20), "r1", context, "each", ["f"], role, subject, false, null, lets ?? [], items, null);

    private static SchematronPattern Abstract(
        string id,
        SchematronParameter[]? declared = null,
        SchematronLet[]? lets = null,
        params SchematronRule[] rules
    ) => new(At(10), id, true, null, null, null, null, [], declared ?? [], lets ?? [], rules, null);

    private static SchematronPattern Instance(string id, string isA, int line, params SchematronParameter[] supplied) =>
        new(At(line), id, false, isA, null, null, null, [], supplied, [], [], null);

    private static SchematronPattern Concrete(string id, int line) =>
        new(At(line), id, false, null, null, null, null, [], [], [], [], null);

    private static (bool Ok, IReadOnlyList<SchematronPatternBase> Patterns, FakeProblemSink Problems) Run(
        params SchematronPatternBase[] patterns
    )
    {
        var problems = new FakeProblemSink();
        var ok = AbstractPatternExpander.TryExpand(patterns, problems, out var expanded);
        return (ok, expanded, problems);
    }

    private static SchematronAssertion FirstAssertion(SchematronPatternBase pattern) =>
        (SchematronAssertion)pattern.Rules[0].Items[0];

    // The abstract pattern with one rule, one assertion, and the given substitution sites.
    private static SchematronPattern OneAssertion(
        string context,
        string test,
        SchematronParameter[]? declared = null,
        SchematronLet[]? ruleLets = null
    ) => Abstract("abs", declared, null, Rule(context, ruleLets, null, null, Assert1(test)));

    [Test]
    public async Task Substitution_LongestNameFirst_ReplacesAAndAbCorrectly()
    {
        var abs = OneAssertion("$ab", "$a = $ab and $ab != $a");
        var instance = Instance("inst", "abs", 40, Param("a", "'1'", 41), Param("ab", "'2'", 42));

        var (ok, patterns, problems) = Run(abs, instance);

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(patterns[0].Rules[0].Context).IsEqualTo("'2'");
        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("'1' = '2' and '2' != '1'");
    }

    [Test]
    public async Task Substitution_DeclarationOrderDoesNotMatter_LongestNameStillWins()
    {
        var abs = OneAssertion("x", "$ab = $a", [Param("ab", null, 11), Param("a", null, 12)]);
        var instance = Instance("inst", "abs", 40, Param("a", "1", 41), Param("ab", "2", 42));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("2 = 1");
    }

    [Test]
    public async Task Substitution_ReplacedValueIsNotSubstitutedAgain()
    {
        var abs = OneAssertion("x", "$ab");
        var instance = Instance("inst", "abs", 40, Param("a", "1", 41), Param("ab", "$a", 42));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("$a");
    }

    [Test]
    public async Task Substitution_IsTextual_ReplacesAPrefixOfALongerName()
    {
        var abs = OneAssertion("x", "$abc");
        var instance = Instance("inst", "abs", 40, Param("a", "1", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("1bc");
    }

    [Test]
    public async Task Substitution_UnknownVariableReference_IsLeftAlone()
    {
        var abs = OneAssertion("x", "$other + $a + $");
        var instance = Instance("inst", "abs", 40, Param("a", "1", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("$other + 1 + $");
    }

    [Test]
    public async Task Substitution_InContextTestAndBothKindsOfLet()
    {
        var abs = new SchematronPattern(
            At(10),
            "abs",
            true,
            null,
            null,
            null,
            null,
            [],
            [],
            [Let("pv", "$p", 11)],
            [Rule("$p/x", [Let("rv", "count($p)", 21)], null, null, Assert1("$p = 1"))],
            null
        );
        var instance = Instance("inst", "abs", 40, Param("p", "item", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(patterns[0].Lets[0].Value).IsEqualTo("item");
        await Assert.That(patterns[0].Rules[0].Context).IsEqualTo("item/x");
        await Assert.That(patterns[0].Rules[0].Lets[0].Value).IsEqualTo("count(item)");
        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("item = 1");
    }

    [Test]
    public async Task Substitution_InReportTest_Applies()
    {
        var report = new SchematronReport(At(30), "$p", null, [], null, null, null, [], [], Message(Text("t")));
        var abs = Abstract("abs", null, null, Rule("x", null, null, null, report));
        var instance = Instance("inst", "abs", 40, Param("p", "y", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("y");
    }

    [Test]
    public async Task Substitution_InValueOf_AppliesAlsoWhenNestedInInlineElements()
    {
        var message = new SchematronMessagePart[]
        {
            ValueOf("$p"),
            new SchematronEmphasis(At(1), [new SchematronSpan(At(1), null, [ValueOf("$p/@id")])]),
            new SchematronDirection(At(1), null, [ValueOf("string($p)")]),
        };
        var abs = Abstract("abs", null, null, Rule("x", null, null, null, Assert1("true()", null, null, message)));
        var instance = Instance("inst", "abs", 40, Param("p", "y", 41));

        var (_, patterns, _) = Run(abs, instance);

        var parts = FirstAssertion(patterns[0]).Message.Parts;
        await Assert.That(((SchematronValueOf)parts[0]).Select).IsEqualTo("y");
        var span = (SchematronSpan)((SchematronEmphasis)parts[1]).Parts[0];
        await Assert.That(((SchematronValueOf)span.Parts[0]).Select).IsEqualTo("y/@id");
        await Assert.That(((SchematronValueOf)((SchematronDirection)parts[2]).Parts[0]).Select).IsEqualTo("string(y)");
    }

    [Test]
    public async Task Substitution_InNamePath_AppliesAlsoWhenNestedAndKeepsAnAbsentPath()
    {
        var message = new SchematronMessagePart[]
        {
            new SchematronName(At(1), "$p"),
            new SchematronEmphasis(At(1), [new SchematronName(At(1), "$p/@id")]),
            new SchematronName(At(1), null),
        };
        var abs = Abstract("abs", null, null, Rule("x", null, null, null, Assert1("true()", null, null, message)));
        var instance = Instance("inst", "abs", 40, Param("p", "y", 41));

        var (_, patterns, _) = Run(abs, instance);

        var parts = FirstAssertion(patterns[0]).Message.Parts;
        await Assert.That(((SchematronName)parts[0]).Path).IsEqualTo("y");
        await Assert.That(((SchematronName)((SchematronEmphasis)parts[1]).Parts[0]).Path).IsEqualTo("y/@id");
        await Assert.That(((SchematronName)parts[2]).Path).IsNull();
    }

    // Every attribute and text the substitution must not touch holds $p; the instance supplies p.
    private static SchematronPatternBase HostilePattern()
    {
        var message = new SchematronMessagePart[] { Text("$p"), new SchematronName(At(1), "$p") };
        var assertion = new SchematronAssert(
            At(30),
            "true()",
            "$p",
            ["$p"],
            "$p",
            "$p",
            "$p",
            ["$p"],
            ["$p"],
            Message(message)
        );
        var paragraph = new SchematronParagraph(At(31), [Text("$p")], "$p", null, null);
        var rule = new SchematronRule(
            At(20),
            "$p",
            "x",
            "$p",
            ["$p"],
            "$p",
            "$p",
            false,
            null,
            [],
            [assertion, paragraph],
            null
        );
        var patternParagraph = new SchematronParagraph(At(11), [Text("$p")], null, null, null);
        var abs = new SchematronPattern(
            At(10),
            "abs",
            true,
            null,
            "$p",
            "$p",
            null,
            [patternParagraph],
            [],
            [],
            [rule],
            null
        );

        var (_, patterns, _) = Run(abs, Instance("inst", "abs", 40, Param("p", "y", 41)));
        return patterns[0];
    }

    [Test]
    public async Task Substitution_NowhereElse_RuleAttributesStayUnchanged()
    {
        var rule = HostilePattern().Rules[0];

        await Assert.That(rule.Id).IsEqualTo("$p");
        await Assert.That(rule.VisitEach).IsEqualTo("$p");
        await Assert.That(rule.Flags[0]).IsEqualTo("$p");
        await Assert.That(rule.Role).IsEqualTo("$p");
        await Assert.That(rule.Subject).IsEqualTo("$p");
    }

    [Test]
    public async Task Substitution_NowhereElse_AssertionAttributesAndMessageTextStayUnchanged()
    {
        var a = FirstAssertion(HostilePattern());

        await Assert.That(a.Id).IsEqualTo("$p");
        await Assert.That(a.Flags[0]).IsEqualTo("$p");
        await Assert.That(a.Role).IsEqualTo("$p");
        await Assert.That(a.Severity).IsEqualTo("$p");
        await Assert.That(a.Subject).IsEqualTo("$p");
        await Assert.That(a.DiagnosticIds[0]).IsEqualTo("$p");
        await Assert.That(a.PropertyIds[0]).IsEqualTo("$p");
        await Assert.That(((SchematronText)a.Message.Parts[0]).Text).IsEqualTo("$p");
        await Assert.That(((SchematronName)a.Message.Parts[1]).Path).IsEqualTo("$p");
    }

    [Test]
    public async Task Substitution_NowhereElse_ParagraphsPatternRoleAndDocumentsStayUnchanged()
    {
        var pattern = HostilePattern();
        var rule = pattern.Rules[0];

        await Assert.That(((SchematronText)((SchematronParagraph)rule.Items[1]).Parts[0]).Text).IsEqualTo("$p");
        await Assert.That(((SchematronText)pattern.Paragraphs[0].Parts[0]).Text).IsEqualTo("$p");
        await Assert.That(pattern.Documents).IsEqualTo("$p");
        await Assert.That(pattern.Role).IsEqualTo("$p");
    }

    [Test]
    public async Task Parameter_Default_IsUsedWhenTheInstanceSuppliesNone()
    {
        var abs = OneAssertion("x", "$v", [Param("v", "'a'", 11)]);
        var instance = Instance("inst", "abs", 40);

        var (ok, patterns, _) = Run(abs, instance);

        await Assert.That(ok).IsTrue();
        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("'a'");
    }

    [Test]
    public async Task Parameter_SuppliedValue_OverridesTheDefault()
    {
        var abs = OneAssertion("x", "$v = 'b'", [Param("v", "'a'", 11)]);
        var instance = Instance("inst", "abs", 40, Param("v", "'b'", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("'b' = 'b'");
    }

    [Test]
    public async Task Parameter_AbstractDeclaresNone_AnySuppliedNameIsAccepted()
    {
        var abs = OneAssertion("x", "$anything");
        var instance = Instance("inst", "abs", 40, Param("anything", "7", 41));

        var (ok, patterns, problems) = Run(abs, instance);

        await Assert.That(ok).IsTrue();
        await Assert.That(problems.Problems.Count).IsEqualTo(0);
        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("7");
    }

    [Test]
    public async Task Parameter_DeclaredWithoutDefaultAndNotSupplied_ReportsParamMissingAtTheInstance()
    {
        var abs = OneAssertion("x", "$v", [Param("v", null, 11)]);
        var instance = Instance("inst", "abs", 40);

        var (ok, patterns, problems) = Run(abs, instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ParamMissing);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(40));
        await Assert.That(problems.Problems[0].Message).Contains("'v'");
        await Assert.That(patterns[0]).IsSameReferenceAs(abs);
    }

    [Test]
    public async Task Parameter_SuppliedWithoutValueAndNoDefault_ReportsParamMissingAtTheParam()
    {
        var abs = OneAssertion("x", "$v", [Param("v", null, 11)]);
        var instance = Instance("inst", "abs", 40, Param("v", null, 41));

        var (ok, _, problems) = Run(abs, instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ParamMissing);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(41));
    }

    [Test]
    public async Task Parameter_SuppliedWithoutValueWhenAbstractDeclaresNone_ReportsParamMissingAtTheParam()
    {
        var abs = OneAssertion("x", "$v");
        var instance = Instance("inst", "abs", 40, Param("v", null, 41));

        var (ok, _, problems) = Run(abs, instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ParamMissing);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(41));
    }

    [Test]
    public async Task Parameter_SuppliedButNotDeclared_ReportsParamUnknownAtTheParam()
    {
        var abs = OneAssertion("x", "$v", [Param("v", "1", 11)]);
        var instance = Instance("inst", "abs", 40, Param("v", "2", 41), Param("typo", "3", 42));

        var (ok, patterns, problems) = Run(abs, instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ParamUnknown);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(42));
        await Assert.That(problems.Problems[0].Message).Contains("'typo'");
        await Assert.That(patterns[1]).IsSameReferenceAs(instance);
    }

    [Test]
    public async Task IsA_UnknownPattern_ReportsIdrefUnresolvedAtTheInstance()
    {
        var instance = Instance("inst", "nowhere", 40);

        var (ok, patterns, problems) = Run(Concrete("c", 1), instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(40));
        await Assert.That(problems.Problems[0].Message).Contains("'nowhere'");
        await Assert.That(patterns[1]).IsSameReferenceAs(instance);
    }

    [Test]
    public async Task IsA_NonAbstractPattern_ReportsAbstractTargetInvalidAtTheInstance()
    {
        var instance = Instance("inst", "c", 40);

        var (ok, _, problems) = Run(Concrete("c", 1), instance);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.AbstractTargetInvalid);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(40));
        await Assert.That(problems.Problems[0].Message).Contains("'c'");
    }

    [Test]
    public async Task IsA_PatternWithoutId_IsNotATarget()
    {
        var anonymous = new SchematronPattern(At(5), null, true, null, null, null, null, [], [], [], [], null);

        var (ok, _, problems) = Run(anonymous, Instance("inst", "abs", 40));

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
    }

    [Test]
    public async Task Errors_AreCollectedForEveryInstance_AndTheInputIsReturnedUnchanged()
    {
        var abs = OneAssertion("x", "$v", [Param("v", null, 11)]);
        var first = Instance("one", "abs", 40);
        var second = Instance("two", "missing", 50);
        var third = Instance("three", "abs", 60, Param("v", "1", 61));

        var (ok, patterns, problems) = Run(abs, first, second, third);

        await Assert.That(ok).IsFalse();
        await Assert.That(problems.Problems.Count).IsEqualTo(2);
        await Assert.That(problems.Problems[0].Location).IsEqualTo(At(40));
        await Assert.That(problems.Problems[1].Location).IsEqualTo(At(50));
        await Assert.That(patterns.Count).IsEqualTo(4);
        await Assert.That(patterns[3]).IsSameReferenceAs(third);
    }

    [Test]
    public async Task AbstractPattern_IsAbsentFromTheExpandedModel()
    {
        var abs = OneAssertion("x", "true()");
        var concrete = Concrete("c", 5);
        var instance = Instance("inst", "abs", 40);

        var (ok, patterns, _) = Run(abs, concrete, instance);

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns.Count).IsEqualTo(2);
        await Assert.That(patterns[0]).IsSameReferenceAs(concrete);
        await Assert.That(patterns[1].Id).IsEqualTo("inst");
        foreach (var pattern in patterns)
        {
            await Assert.That(pattern.IsAbstract).IsFalse();
            await Assert.That(pattern.Id).IsNotEqualTo("abs");
        }
    }

    [Test]
    public async Task AbstractPattern_WithoutInstance_IsStillDropped()
    {
        var (ok, patterns, _) = Run(OneAssertion("x", "true()"), Concrete("c", 5));

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns.Count).IsEqualTo(1);
        await Assert.That(patterns[0].Id).IsEqualTo("c");
    }

    [Test]
    public async Task NothingToExpand_ReturnsTheSamePatterns()
    {
        var first = Concrete("a", 1);
        var second = Concrete("b", 2);

        var (ok, patterns, _) = Run(first, second);

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns.Count).IsEqualTo(2);
        await Assert.That(patterns[0]).IsSameReferenceAs(first);
        await Assert.That(patterns[1]).IsSameReferenceAs(second);
    }

    [Test]
    public async Task Instance_TakesTheIdFromTheInstance_AndLocationFromTheDefinition()
    {
        var abs = OneAssertion("x", "true()");
        var instance = Instance("inst", "abs", 40);

        var (_, patterns, _) = Run(abs, instance);

        var expanded = patterns[0];
        await Assert.That(expanded.Id).IsEqualTo("inst");
        await Assert.That(expanded.IsAbstract).IsFalse();
        await Assert.That(expanded.Location).IsEqualTo(At(10));
        await Assert.That(expanded.ExpandedFrom).IsEqualTo(At(40));
        await Assert.That(expanded.Rules[0].Location).IsEqualTo(At(20));
        await Assert.That(expanded.Rules[0].ExpandedFrom).IsEqualTo(At(40));
        await Assert.That(expanded.Rules[0].Items[0].Location).IsEqualTo(At(30));
    }

    [Test]
    public async Task Instance_ExpandedPatternCarriesTheSuppliedParameters()
    {
        var abs = OneAssertion("x", "$v", [Param("v", "1", 11)]);
        var instance = Instance("inst", "abs", 40, Param("v", "2", 41));

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(patterns[0].Parameters.Count).IsEqualTo(1);
        await Assert.That(patterns[0].Parameters[0].Value).IsEqualTo("2");
    }

    [Test]
    public async Task Instance_OwnRoleAndDocuments_WinOverTheDefinition()
    {
        var abs = new SchematronPattern(At(10), "abs", true, null, "def-docs", "def-role", null, [], [], [], [], null);
        var instance = new SchematronPattern(
            At(40),
            "inst",
            false,
            "abs",
            "own-docs",
            "own-role",
            null,
            [],
            [],
            [],
            [],
            null
        );

        var (_, patterns, _) = Run(abs, instance);

        await Assert.That(patterns[0].Role).IsEqualTo("own-role");
        await Assert.That(patterns[0].Documents).IsEqualTo("own-docs");
    }

    [Test]
    public async Task Instance_WithoutOwnRoleAndDocuments_InheritsThemFromTheDefinition()
    {
        var abs = new SchematronPattern(At(10), "abs", true, null, "def-docs", "def-role", null, [], [], [], [], null);

        var (_, patterns, _) = Run(abs, Instance("inst", "abs", 40));

        await Assert.That(patterns[0].Role).IsEqualTo("def-role");
        await Assert.That(patterns[0].Documents).IsEqualTo("def-docs");
    }

    [Test]
    public async Task Instance_OfAGroup_StaysAGroup()
    {
        var abs = OneAssertion("x", "true()");
        var group = new SchematronGroup(At(40), "inst", false, "abs", null, null, null, [], [], [], [], null);

        var (_, patterns, _) = Run(abs, group);

        await Assert.That(patterns[0]).IsTypeOf<SchematronGroup>();
        await Assert.That(patterns[0].Rules.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TwoInstancesOfOneAbstractPattern_AreSubstitutedIndependently()
    {
        var abs = OneAssertion("$p", "$p = 1");
        var first = Instance("one", "abs", 40, Param("p", "a", 41));
        var second = Instance("two", "abs", 50, Param("p", "b", 51));

        var (ok, patterns, _) = Run(abs, first, second);

        await Assert.That(ok).IsTrue();
        await Assert.That(patterns.Count).IsEqualTo(2);
        await Assert.That(patterns[0].Rules[0].Context).IsEqualTo("a");
        await Assert.That(patterns[1].Rules[0].Context).IsEqualTo("b");
        await Assert.That(FirstAssertion(patterns[0]).Test).IsEqualTo("a = 1");
        await Assert.That(FirstAssertion(patterns[1]).Test).IsEqualTo("b = 1");
        await Assert.That(abs.Rules[0].Context).IsEqualTo("$p");
    }

    [Test]
    public async Task Instance_ExpandedFromIsTheSiteOfEachInstance()
    {
        var abs = OneAssertion("x", "true()");

        var (_, patterns, _) = Run(abs, Instance("one", "abs", 40), Instance("two", "abs", 50));

        await Assert.That(patterns[0].ExpandedFrom).IsEqualTo(At(40));
        await Assert.That(patterns[1].ExpandedFrom).IsEqualTo(At(50));
        await Assert.That(patterns[1].Rules[0].ExpandedFrom).IsEqualTo(At(50));
    }

    [Test]
    public async Task Instance_AbstractRuleInsideTheAbstractPattern_IsCopiedForTheRuleStage()
    {
        var abstractRule = new SchematronRule(At(21), "shared", null, null, [], null, null, true, null, [], [], null);
        var abs = Abstract("abs", null, null, abstractRule);

        var (_, patterns, _) = Run(abs, Instance("inst", "abs", 40));

        await Assert.That(patterns[0].Rules.Count).IsEqualTo(1);
        await Assert.That(patterns[0].Rules[0].IsAbstract).IsTrue();
    }

    [Test]
    public async Task TryExpand_NullArguments_Throw()
    {
        var problems = new FakeProblemSink();

        await Assert
            .That(() => AbstractPatternExpander.TryExpand(null!, problems, out _))
            .Throws<ArgumentNullException>();
        await Assert.That(() => AbstractPatternExpander.TryExpand([], null!, out _)).Throws<ArgumentNullException>();
    }
}
