namespace NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

public class NameConstraintTests
{
    /// <summary>
    /// Parses <c>any</c>, <c>not a b</c>, or <c>set a b</c> (<c>L</c> is the absent namespace), then optional parts
    /// separated by semicolons: <c>q a#x</c> for disallowed names (<c>ns#local</c>), <c>defined</c>, and <c>sibling</c>.
    /// </summary>
    internal static NameConstraint Spec(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parts = text.Split(';');
        var head = parts[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var negation = !string.Equals(head[0], "set", StringComparison.Ordinal);
        var namespaces = new List<string>();
        for (var i = 1; i < head.Length; i++)
        {
            namespaces.Add(Ns(head[i]));
        }

        var names = new List<XPathQName>();
        var defined = false;
        var sibling = false;
        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (string.Equals(part, "defined", StringComparison.Ordinal))
            {
                defined = true;
            }
            else if (string.Equals(part, "sibling", StringComparison.Ordinal))
            {
                sibling = true;
            }
            else
            {
                foreach (var token in part.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var hash = token.IndexOf('#', StringComparison.Ordinal);
                    names.Add(new XPathQName(Ns(token.Substring(0, hash)), token.Substring(hash + 1)));
                }
            }
        }

        return NameConstraint.Create(negation, namespaces.ToArray(), names.ToArray(), defined, sibling);
    }

    private static string Ns(string token) =>
        string.Equals(token, "L", StringComparison.Ordinal) ? string.Empty : "urn:" + token;

    private static XPathQName Name(string ns, string local) => new(Ns(ns), local);

    private sealed class Names(params XPathQName[] names) : IDeclaredNames
    {
        public bool Contains(XPathQName name) => Array.IndexOf(names, name) >= 0;
    }

    [Test]
    [Arguments("any", "a", true)]
    [Arguments("any", "L", true)]
    [Arguments("not a", "a", false)]
    [Arguments("not a", "b", true)]
    [Arguments("not a", "L", true)]
    [Arguments("not a L", "L", false)]
    [Arguments("not a L", "b", true)]
    [Arguments("not a b", "b", false)]
    [Arguments("set a b", "b", true)]
    [Arguments("set a b", "c", false)]
    [Arguments("set a L", "L", true)]
    [Arguments("set a", "L", false)]
    [Arguments("set", "a", false)]
    public async Task AllowsNamespace_FollowsTheVariety(string spec, string ns, bool expected) =>
        await Assert.That(Spec(spec).AllowsNamespace(Ns(ns))).IsEqualTo(expected);

    [Test]
    public async Task NotNamespace_ExcludesTheListedNamespaces_AndOnlyThose()
    {
        var constraint = Spec("not a");

        await Assert.That(constraint.Variety).IsEqualTo(NamespaceConstraintVariety.Not);
        await Assert.That(constraint.Allows(Name("a", "x"))).IsFalse();
        await Assert.That(constraint.Allows(Name("b", "x"))).IsTrue();
        await Assert.That(constraint.Allows(Name("L", "x"))).IsTrue();
    }

    [Test]
    public async Task NotQName_ExcludesTheListedNames_AndOnlyThose()
    {
        var constraint = Spec("any;q a#x b#y");

        await Assert.That(constraint.Allows(Name("a", "x"))).IsFalse();
        await Assert.That(constraint.Allows(Name("b", "y"))).IsFalse();
        await Assert.That(constraint.Allows(Name("a", "y"))).IsTrue();
        await Assert.That(constraint.Allows(Name("b", "x"))).IsTrue();
        await Assert.That(constraint.Allows(Name("L", "x"))).IsTrue();
    }

    [Test]
    public async Task NotQName_InAnExcludedNamespace_ChangesNothingBecauseTheNamespaceExcludesTheName()
    {
        var constraint = Spec("not a;q a#x");

        await Assert.That(constraint.DisallowedNames).IsEmpty();
        await Assert.That(constraint.Allows(Name("a", "x"))).IsFalse();
        await Assert.That(constraint).IsEqualTo(Spec("not a"));
    }

    [Test]
    public async Task Allows_Defined_ExcludesTheDeclaredNamesOnlyWhenTheContextKnowsThem()
    {
        var constraint = Spec("any;defined");
        var declared = new Names(Name("a", "x"));

        await Assert.That(constraint.Allows(Name("a", "x"), declared)).IsFalse();
        await Assert.That(constraint.Allows(Name("a", "y"), declared)).IsTrue();
        await Assert.That(constraint.Allows(Name("a", "x"))).IsTrue();
        await Assert.That(constraint.Allows(Name("a", "x"), null, declared)).IsTrue();
        await Assert.That(Spec("any").Allows(Name("a", "x"), declared)).IsTrue();
    }

    [Test]
    public async Task Allows_DefinedSibling_ExcludesTheSiblingNamesOnlyWhenTheContextKnowsThem()
    {
        var constraint = Spec("any;sibling");
        var siblings = new Names(Name("a", "x"));

        await Assert.That(constraint.Allows(Name("a", "x"), null, siblings)).IsFalse();
        await Assert.That(constraint.Allows(Name("a", "y"), null, siblings)).IsTrue();
        await Assert.That(constraint.Allows(Name("a", "x"), siblings)).IsTrue();
        await Assert.That(constraint.Allows(Name("a", "x"))).IsTrue();
        await Assert.That(Spec("any").Allows(Name("a", "x"), null, siblings)).IsTrue();
    }

    [Test]
    public async Task Create_NormalizesNamespacesAndNames()
    {
        var left = NameConstraint.Create(
            false,
            ["urn:b", "urn:a", "urn:b"],
            [Name("a", "y"), Name("a", "x"), Name("a", "y"), Name("c", "z")]
        );
        var right = Spec("set a b;q a#x a#y");

        await Assert.That(left).IsEqualTo(right);
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left.Namespaces).IsEquivalentTo(["urn:a", "urn:b"]);
        await Assert.That(left.DisallowedNames).Count().IsEqualTo(2);
        await Assert.That(left.DisallowedNames[0].LocalName).IsEqualTo("x");
    }

    [Test]
    public async Task Create_DoesNotKeepTheCallersArrays()
    {
        var namespaces = new[] { "urn:a" };
        var constraint = NameConstraint.Create(false, namespaces, []);

        namespaces[0] = "urn:z";

        await Assert.That(constraint.AllowsNamespace("urn:a")).IsTrue();
    }

    [Test]
    public async Task Variety_NamesTheThreeForms()
    {
        await Assert.That(NameConstraint.Any.Variety).IsEqualTo(NamespaceConstraintVariety.Any);
        await Assert.That(Spec("not").Variety).IsEqualTo(NamespaceConstraintVariety.Any);
        await Assert.That(Spec("any;q a#x").Variety).IsEqualTo(NamespaceConstraintVariety.Any);
        await Assert.That(Spec("not a").Variety).IsEqualTo(NamespaceConstraintVariety.Not);
        await Assert.That(Spec("set a").Variety).IsEqualTo(NamespaceConstraintVariety.Enumeration);
        await Assert.That(Spec("set").Variety).IsEqualTo(NamespaceConstraintVariety.Enumeration);
        await Assert.That(Spec("set").IsEmpty).IsTrue();
        await Assert.That(Spec("set a").IsEmpty).IsFalse();
        await Assert.That(Spec("not a").IsNegation).IsTrue();
        await Assert.That(Spec("set a").IsNegation).IsFalse();
        await Assert.That(Spec("any;defined;sibling").DisallowsDefined).IsTrue();
        await Assert.That(Spec("any;defined;sibling").DisallowsSibling).IsTrue();
        await Assert.That(Spec("any").DisallowsDefined).IsFalse();
    }

    [Test]
    [Arguments("##any", "urn:t", "any")]
    [Arguments("##other", "urn:t", "not t L")]
    [Arguments("##other", "", "not L")]
    [Arguments("##local", "urn:t", "set L")]
    [Arguments("urn:a ##targetNamespace", "urn:t", "set a t")]
    public async Task FromCore_ConvertsTheOneWildcardOfXsd10(string value, string targetNamespace, string expected)
    {
        var core = NamespaceConstraintTests.Parse(value, targetNamespace);

        await Assert.That(NameConstraint.FromCore(core)).IsEqualTo(Spec(expected));
    }

    // Subset, union, and intersection tables (Structures 3.10.6, extended to the disallowed names and keywords).
    [Test]
    [Arguments("set a", "any", true)]
    [Arguments("not a", "any", true)]
    [Arguments("any", "any", true)]
    [Arguments("any", "not a", false)]
    [Arguments("any", "set a", false)]
    [Arguments("set a", "set a b", true)]
    [Arguments("set a b", "set a", false)]
    [Arguments("set", "set a", true)]
    [Arguments("set", "not a", true)]
    [Arguments("not a", "not a", true)]
    [Arguments("not a b", "not a", true)]
    [Arguments("not a", "not a b", false)]
    [Arguments("not a L", "not a", true)]
    [Arguments("not a", "not a L", false)]
    [Arguments("not a", "set a b", false)]
    [Arguments("set b", "not a", true)]
    [Arguments("set a", "not a", false)]
    [Arguments("set a L", "not a", false)]
    [Arguments("set b c", "not a", true)]
    [Arguments("set a;q a#x", "set a", true)]
    [Arguments("set a", "set a;q a#x", false)]
    [Arguments("set a;q a#x", "set a;q a#x", true)]
    [Arguments("set b", "set a b;q a#x", true)]
    [Arguments("any;q a#x", "any", true)]
    [Arguments("any", "any;q a#x", false)]
    [Arguments("any;q a#x b#y", "any;q a#x", true)]
    [Arguments("any;q a#x", "any;q a#x b#y", false)]
    [Arguments("any;defined", "any;defined", true)]
    [Arguments("any;defined", "any", true)]
    [Arguments("any", "any;defined", false)]
    [Arguments("any;defined;sibling", "any;sibling", true)]
    [Arguments("any;defined", "any;sibling", false)]
    [Arguments("any;sibling", "any;sibling", true)]
    public async Task IsSubsetOf_Table(string sub, string super, bool expected) =>
        await Assert.That(Spec(sub).IsSubsetOf(Spec(super))).IsEqualTo(expected);

    [Test]
    [Arguments("any", "not a", "not a")]
    [Arguments("not a", "any", "not a")]
    [Arguments("not a", "not b", "not a b")]
    [Arguments("not a L", "not b", "not a b L")]
    [Arguments("set a b", "set b c", "set b")]
    [Arguments("set a b", "not a", "set b")]
    [Arguments("not a", "set a b", "set b")]
    [Arguments("set a L", "not L", "set a")]
    [Arguments("set a", "set b", "set")]
    [Arguments("set a", "not a", "set")]
    [Arguments("not a;q c#x", "not b;q c#y", "not a b;q c#x c#y")]
    [Arguments("set a;q a#x", "any", "set a;q a#x")]
    [Arguments("set a;q a#x", "not a", "set")]
    [Arguments("set a b;q a#x", "set a;q b#y", "set a;q a#x")]
    [Arguments("any;defined", "any;sibling", "any;defined;sibling")]
    [Arguments("any;defined", "set a", "set a;defined")]
    public async Task Intersect_Table(string left, string right, string expected)
    {
        var forward = Spec(left).Intersect(Spec(right));
        var backward = Spec(right).Intersect(Spec(left));

        await Assert.That(forward).IsEqualTo(Spec(expected));
        await Assert.That(backward).IsEqualTo(Spec(expected));
    }

    [Test]
    [Arguments("any", "not a", "any")]
    [Arguments("not a", "any", "any")]
    [Arguments("not a", "not b", "any")]
    [Arguments("not a", "not a b", "not a")]
    [Arguments("not a b L", "not a", "not a")]
    [Arguments("set a", "set b", "set a b")]
    [Arguments("set a b", "set b", "set a b")]
    [Arguments("set", "set", "set")]
    [Arguments("not a b", "set a", "not b")]
    [Arguments("not a", "set a", "any")]
    [Arguments("not a L", "set a", "not L")]
    [Arguments("not a L", "set a L", "any")]
    [Arguments("not a", "set b", "not a")]
    [Arguments("set", "not a", "not a")]
    [Arguments("not a;q c#x", "not b;q c#x", "any;q c#x")]
    [Arguments("not a;q c#x", "not b", "any")]
    [Arguments("set c;q c#x", "set c;q c#y", "set c")]
    [Arguments("set c;q c#x", "set d", "set c d;q c#x")]
    [Arguments("set c;q c#x", "set c;q c#x", "set c;q c#x")]
    [Arguments("any;defined", "not a;defined", "any;defined")]
    [Arguments("any;defined;sibling", "set a;defined;sibling", "any;defined;sibling")]
    [Arguments("set a;defined", "set a b", "set a b")]
    [Arguments("set a b", "set a;defined", "set a b")]
    [Arguments("set a;sibling", "set a b", "set a b")]
    [Arguments("set a;defined;sibling", "set a b", "set a b")]
    // A keyword that is not in both operands is dropped, which may allow names neither operand allows (3.10.6.3, note).
    [Arguments("any;defined", "not a", "any")]
    [Arguments("set a;defined", "set b", "set a b")]
    [Arguments("set a;defined", "set a b;q a#x", "set a b")]
    [Arguments("set a;defined", "set a;sibling", "set a")]
    [Arguments("set a;defined;sibling", "set a;defined", "set a;defined")]
    [Arguments("set a;defined;sibling", "set a;defined;sibling", "set a;defined;sibling")]
    [Arguments("set c;defined;q c#x", "set d;defined", "set c d;defined;q c#x")]
    public async Task Union_Table(string left, string right, string expected)
    {
        var forward = Spec(left).Union(Spec(right));
        var backward = Spec(right).Union(Spec(left));

        await Assert.That(forward).IsEqualTo(Spec(expected));
        await Assert.That(backward).IsEqualTo(Spec(expected));
    }

    [Test]
    public async Task Union_OfTheSameConstraint_IsTheConstraint()
    {
        var constraint = Spec("set a;q a#x;defined");

        await Assert.That(constraint.Union(Spec("set a;q a#x;defined"))).IsSameReferenceAs(constraint);
    }

    [Test]
    [Arguments("not a", "not b", true)]
    [Arguments("any", "any", true)]
    [Arguments("set a", "set b", false)]
    [Arguments("set a", "set a b", true)]
    [Arguments("set a", "not a", false)]
    [Arguments("set a b", "not a", true)]
    [Arguments("set L", "not L", false)]
    [Arguments("any", "set", false)]
    [Arguments("any", "set a", true)]
    [Arguments("set a;q a#x", "set a", true)]
    public async Task Overlaps_WhenSomeNamespaceIsAllowedByBoth(string left, string right, bool expected)
    {
        await Assert.That(Spec(left).Overlaps(Spec(right))).IsEqualTo(expected);
        await Assert.That(Spec(right).Overlaps(Spec(left))).IsEqualTo(expected);
    }

    [Test]
    public async Task Equals_ComparesEveryPart()
    {
        var constraint = Spec("set a;q a#x;defined");

        await Assert.That(constraint.Equals(Spec("set a;q a#x;defined"))).IsTrue();
        await Assert.That(constraint.Equals((object)Spec("set a;q a#x;defined"))).IsTrue();
        object? none = null;
        await Assert.That(Spec("set a;q a#x;defined").Equals(none)).IsFalse();
        await Assert.That(constraint.Equals((object)"set a")).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a;q a#y;defined"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a;q a#x"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a;q a#x;defined;sibling"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set b;q a#x;defined"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a b;q a#x;defined"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("not a;q a#x;defined"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a;q a#x a#y;defined"))).IsFalse();
        await Assert.That(constraint.Equals(Spec("set a;q a#x;sibling"))).IsFalse();
    }

    [Test]
    [Arguments("any", "##any")]
    [Arguments("not a L", "not(##local,urn:a)")]
    [Arguments("set a L", "{##local,urn:a}")]
    [Arguments("set a;q a#x;defined;sibling", "{urn:a} except [Q{urn:a}x,##defined,##definedSibling]")]
    [Arguments("any;defined", "##any except [##defined]")]
    public async Task ToString_ShowsTheFormAndTheExceptions(string spec, string expected) =>
        await Assert.That(Spec(spec).ToString()).IsEqualTo(expected);

    [Test]
    public async Task Operations_WithNullArgument_Throw()
    {
        var constraint = Spec("any");

        await Assert.That(() => NameConstraint.Create(true, null!, [])).Throws<ArgumentNullException>();
        await Assert.That(() => NameConstraint.Create(true, [null!], [])).Throws<ArgumentNullException>();
        await Assert.That(() => NameConstraint.Create(true, [], null!)).Throws<ArgumentNullException>();
        await Assert.That(() => NameConstraint.FromCore(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => constraint.AllowsNamespace(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => constraint.IsSubsetOf(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => constraint.Union(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => constraint.Intersect(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => constraint.Overlaps(null!)).Throws<ArgumentNullException>();
    }
}
