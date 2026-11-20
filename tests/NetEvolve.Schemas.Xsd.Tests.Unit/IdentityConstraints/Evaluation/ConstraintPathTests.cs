namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

public class ConstraintPathTests
{
    private const string Xml = "http://www.w3.org/XML/1998/namespace";

    private static readonly IReadOnlyList<KeyValuePair<string, string>> NoBindings = [];

    private static readonly IReadOnlyList<KeyValuePair<string, string>> P = [new("p", "urn:p")];

    private static ElementName[] Stack(params string[] names)
    {
        var stack = new ElementName[names.Length];
        for (var i = 0; i < stack.Length; i++)
        {
            var colon = names[i].IndexOf(':', StringComparison.Ordinal);
            stack[i] =
                colon < 0
                    ? new ElementName(string.Empty, names[i])
                    : new ElementName(names[i].Substring(0, colon), names[i].Substring(colon + 1));
        }

        return stack;
    }

    [Test]
    [Arguments("a", 1, 2, true)]
    [Arguments("a", 1, 3, false)]
    [Arguments("b", 1, 2, false)]
    [Arguments(".", 1, 1, true)]
    [Arguments(".", 1, 2, false)]
    [Arguments("*", 1, 2, true)]
    [Arguments("a/b", 1, 3, true)]
    [Arguments("a/b", 2, 3, false)]
    [Arguments(".//b", 1, 3, true)]
    [Arguments(".//b", 1, 2, false)]
    [Arguments(".//a/b", 1, 3, true)]
    [Arguments(".//.", 1, 1, true)]
    [Arguments(".//.", 1, 3, true)]
    [Arguments("a|b", 1, 2, true)]
    [Arguments("x|y", 1, 2, false)]
    [Arguments("p:a", 1, 2, false)]
    public async Task MatchesElement_ChecksTheLastOpenElementsAgainstTheSteps(
        string xpath,
        int contextDepth,
        int depth,
        bool expected
    )
    {
        var path = ConstraintPath.Compile(xpath, NoBindings);

        // The stack is r, a, b: a path of the context r reaches a at depth 2 and b at depth 3.
        await Assert.That(path.MatchesElement(Stack("r", "a", "b"), contextDepth, depth)).IsEqualTo(expected);
    }

    [Test]
    public async Task MatchesElement_PrefixedName_NeedsItsBinding()
    {
        var stack = new[] { new ElementName(string.Empty, "r"), new ElementName("urn:p", "a") };

        await Assert.That(ConstraintPath.Compile("p:a", P).MatchesElement(stack, 1, 2)).IsTrue();
        await Assert.That(ConstraintPath.Compile("p:*", P).MatchesElement(stack, 1, 2)).IsTrue();
        await Assert.That(ConstraintPath.Compile("q:*", P).MatchesElement(stack, 1, 2)).IsFalse();
        await Assert.That(ConstraintPath.Compile("p:a", NoBindings).MatchesElement(stack, 1, 2)).IsFalse();
        await Assert.That(ConstraintPath.Compile("a", P).MatchesElement(stack, 1, 2)).IsFalse();
    }

    [Test]
    public async Task MatchesElement_NameTestOfAnyNamespace_IsOutsideTheSubsetAndMatchesNothing()
    {
        var stack = new[] { new ElementName(string.Empty, "r"), new ElementName(string.Empty, "a") };

        await Assert.That(ConstraintPath.Compile("*:a", NoBindings).MatchesElement(stack, 1, 2)).IsFalse();
    }

    [Test]
    [Arguments("a[1]")]
    [Arguments("a[")]
    [Arguments("")]
    [Arguments("count(a)")]
    [Arguments("parent::a")]
    public async Task Compile_ExpressionOutsideTheSubset_MatchesNothing(string xpath)
    {
        var path = ConstraintPath.Compile(xpath, NoBindings);
        var stack = Stack("r", "a");

        await Assert.That(path.MatchesElement(stack, 1, 2)).IsFalse();
        await Assert.That(path.MatchesAttribute(stack, 1, 2, string.Empty, "a")).IsFalse();
    }

    [Test]
    [Arguments("@a", 1, 1, "", "a", true)]
    [Arguments("@a", 1, 1, "", "b", false)]
    [Arguments("@a", 1, 1, "urn:p", "a", false)]
    [Arguments("@*", 1, 1, "urn:p", "anything", true)]
    [Arguments("a/@a", 1, 2, "", "a", true)]
    [Arguments("a/@a", 1, 1, "", "a", false)]
    [Arguments(".//@a", 1, 2, "", "a", true)]
    [Arguments("@p:a", 1, 1, "urn:p", "a", true)]
    [Arguments("@p:*", 1, 1, "urn:p", "z", true)]
    [Arguments("@p:*", 1, 1, "", "z", false)]
    [Arguments("@q:a", 1, 1, "urn:p", "a", false)]
    [Arguments("@xml:lang", 1, 1, Xml, "lang", true)]
    [Arguments("@xml:lang", 1, 1, "", "lang", false)]
    [Arguments("@*:a", 1, 1, "", "a", false)]
    [Arguments("a", 1, 2, "", "a", false)]
    public async Task MatchesAttribute_ChecksTheOwnerPathAndTheAttributeName(
        string xpath,
        int contextDepth,
        int depth,
        string namespaceName,
        string localName,
        bool expected
    )
    {
        var path = ConstraintPath.Compile(xpath, P);
        var stack = Stack("r", "a");

        await Assert
            .That(path.MatchesAttribute(stack, contextDepth, depth, namespaceName, localName))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task Guards_NullArguments_Throw()
    {
        var path = ConstraintPath.Compile("a", NoBindings);
        var stack = Stack("r", "a");

        await Assert.That(() => ConstraintPath.Compile(null!, NoBindings)).Throws<ArgumentNullException>();
        await Assert.That(() => ConstraintPath.Compile("a", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => path.MatchesElement(null!, 1, 2)).Throws<ArgumentNullException>();
        await Assert.That(() => path.MatchesAttribute(null!, 1, 2, "", "a")).Throws<ArgumentNullException>();
        await Assert.That(() => path.MatchesAttribute(stack, 1, 2, null!, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => path.MatchesAttribute(stack, 1, 2, "", null!)).Throws<ArgumentNullException>();
    }
}
