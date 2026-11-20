namespace NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

public class NegatedWildcardTests
{
    private const string Tns = "urn:test";
    private static readonly DocumentLocation Where = new("file:///a.xsd", 7, 4);

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private static NegatedWildcard Wild(
        string spec,
        XsdProcessContents process = XsdProcessContents.Strict,
        IDeclaredNames? defined = null,
        IDeclaredNames? siblings = null
    ) => NegatedWildcard.Create(NameConstraintTests.Spec(spec), process, Where).WithContext(defined, siblings);

    private static SchemaSyntaxNode Parse(string any, string anyAttribute = "")
    {
        var text =
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:t=\"urn:test\" targetNamespace=\"urn:test\">"
            + "<xs:element name=\"e\"><xs:complexType><xs:sequence>"
            + any
            + "</xs:sequence>"
            + anyAttribute
            + "</xs:complexType></xs:element></xs:schema>";
        var (document, _) = SchemaFixture.Parse(text);
        return SchemaFixture.Find(
            document,
            anyAttribute.Length == 0 ? "element/complexType/sequence/any" : "element/complexType/anyAttribute"
        );
    }

    private bool TryCreate(
        string attributes,
        bool attributeWildcard,
        out NegatedWildcard? wildcard,
        string scopeNamespace = Tns
    )
    {
        var node = attributeWildcard
            ? Parse("<xs:any/>", "<xs:anyAttribute " + attributes + "/>")
            : Parse("<xs:any " + attributes + "/>");
        return NegatedWildcard.TryCreate(
            node,
            new SchemaNamespaceScope(scopeNamespace, false),
            attributeWildcard,
            Reporter,
            out wildcard
        );
    }

    private async Task AssertSingleSrcWildcard()
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcWildcard);
    }

    private sealed class Names(params XPathQName[] names) : IDeclaredNames
    {
        public bool Contains(XPathQName name) => Array.IndexOf(names, name) >= 0;
    }

    [Test]
    public async Task Create_HasTheComponentProperties()
    {
        var constraint = NameConstraintTests.Spec("not a");
        var wildcard = NegatedWildcard.Create(constraint, XsdProcessContents.Lax, Where);

        await Assert.That(wildcard.Kind).IsEqualTo(XsdComponentKind.Wildcard);
        await Assert.That(wildcard.Name).IsEqualTo(default(XPathQName));
        await Assert.That(wildcard.Location).IsEqualTo(Where);
        await Assert.That(wildcard.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(wildcard.Constraint).IsSameReferenceAs(constraint);
        await Assert.That(wildcard.DefinedNames).IsNull();
        await Assert.That(wildcard.SiblingNames).IsNull();
    }

    [Test]
    public async Task TryCreate_Any_DefaultsToAnyNamespaceAndStrict()
    {
        var created = TryCreate(string.Empty, false, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.Constraint).IsEqualTo(NameConstraint.Any);
        await Assert.That(wildcard.ProcessContents).IsEqualTo(XsdProcessContents.Strict);
        await Assert.That(wildcard.Location.Line).IsGreaterThan(0);
    }

    [Test]
    [Arguments("namespace=\"##other\"", "not test L")]
    [Arguments("namespace=\"##local urn:a ##targetNamespace\"", "set L a test")]
    [Arguments("notNamespace=\"urn:a\"", "not a")]
    [Arguments("notNamespace=\"##local ##targetNamespace\"", "not L test")]
    [Arguments("notNamespace=\"##targetNamespace urn:a\"", "not a test")]
    [Arguments("notNamespace=\"\"", "any")]
    public async Task TryCreate_ReadsNamespaceAndNotNamespace(string attributes, string expected)
    {
        var created = TryCreate(attributes, false, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.Constraint).IsEqualTo(NameConstraintTests.Spec(expected));
        await Assert.That(_collector.ToResult().Issues).IsEmpty();
    }

    [Test]
    public async Task TryCreate_NotQName_ReadsQNamesAndKeywords()
    {
        var created = TryCreate("notQName=\"t:x ##defined  urn-less ##definedSibling\"", false, out var wildcard);

        await Assert.That(created).IsTrue();
        var constraint = wildcard!.Constraint;
        await Assert.That(constraint.DisallowsDefined).IsTrue();
        await Assert.That(constraint.DisallowsSibling).IsTrue();
        await Assert.That(constraint.DisallowedNames).Count().IsEqualTo(2);
        await Assert.That(constraint.Allows(new XPathQName(Tns, "x"))).IsFalse();
        await Assert.That(constraint.Allows(new XPathQName(string.Empty, "urn-less"))).IsFalse();
        await Assert.That(constraint.Allows(new XPathQName(Tns, "y"))).IsTrue();
    }

    [Test]
    public async Task TryCreate_NotQNameWithNotNamespace_CombinesBoth()
    {
        var created = TryCreate("notNamespace=\"urn:a\" notQName=\"t:x\"", false, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.Allows(new XPathQName(Tns, "x"))).IsFalse();
        await Assert.That(wildcard.Allows(new XPathQName(Tns, "y"))).IsTrue();
        await Assert.That(wildcard.Allows(new XPathQName("urn:a", "y"))).IsFalse();
    }

    [Test]
    [Arguments("lax", "Lax")]
    [Arguments("skip", "Skip")]
    [Arguments("strict", "Strict")]
    public async Task TryCreate_ReadsProcessContents(string value, string expected)
    {
        var created = TryCreate("processContents=\"" + value + "\"", false, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.ProcessContents).IsEqualTo(Enum.Parse<XsdProcessContents>(expected));
    }

    [Test]
    public async Task TryCreate_AnyAttribute_AcceptsDefinedButNotDefinedSibling()
    {
        var created = TryCreate("notQName=\"##defined t:x\"", true, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.Constraint.DisallowsDefined).IsTrue();
        await Assert.That(wildcard.Constraint.DisallowsSibling).IsFalse();
    }

    [Test]
    public async Task TryCreate_AnyAttribute_WithDefinedSibling_IsASchemaError()
    {
        var created = TryCreate("notQName=\"##definedSibling\"", true, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
        await AssertSingleSrcWildcard();
    }

    [Test]
    public async Task TryCreate_NamespaceTogetherWithNotNamespace_IsASchemaError()
    {
        var created = TryCreate("namespace=\"##any\" notNamespace=\"urn:a\"", false, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
        await AssertSingleSrcWildcard();
    }

    [Test]
    [Arguments("notNamespace=\"##any\"")]
    [Arguments("notNamespace=\"##other\"")]
    [Arguments("notNamespace=\"##foo\"")]
    [Arguments("notQName=\"##foo\"")]
    [Arguments("notQName=\"##defined##definedSibling\"")]
    [Arguments("notQName=\"nope:x\"")]
    [Arguments("notQName=\"t:\"")]
    [Arguments("namespace=\"##any ##local\"")]
    [Arguments("processContents=\"other\"")]
    public async Task TryCreate_InvalidValue_IsASchemaError(string attributes)
    {
        var created = TryCreate(attributes, false, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
        await AssertSingleSrcWildcard();
        await Assert.That(_collector.ToResult().Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Allows_Defined_UsesTheBoundNames_AndIsUnboundWithoutThem()
    {
        var declared = new Names(new XPathQName(Tns, "x"));
        var unbound = Wild("any;defined");
        var bound = unbound.WithContext(declared, null);

        await Assert.That(unbound.Allows(new XPathQName(Tns, "x"))).IsTrue();
        await Assert.That(bound.Allows(new XPathQName(Tns, "x"))).IsFalse();
        await Assert.That(bound.Allows(new XPathQName(Tns, "y"))).IsTrue();
        await Assert.That(bound.DefinedNames).IsSameReferenceAs(declared);
        await Assert.That(bound.SiblingNames).IsNull();
        await Assert.That(bound).IsNotSameReferenceAs(unbound);
        await Assert.That(bound.Location).IsEqualTo(unbound.Location);
        await Assert.That(bound.ProcessContents).IsEqualTo(unbound.ProcessContents);
    }

    [Test]
    public async Task Allows_DefinedSibling_UsesTheBoundSiblings()
    {
        var siblings = new Names(new XPathQName(Tns, "x"));
        var wildcard = Wild("any;sibling", XsdProcessContents.Strict, null, siblings);

        await Assert.That(wildcard.Allows(new XPathQName(Tns, "x"))).IsFalse();
        await Assert.That(wildcard.Allows(new XPathQName(Tns, "y"))).IsTrue();
        await Assert.That(wildcard.SiblingNames).IsSameReferenceAs(siblings);
    }

    [Test]
    public async Task Allows_NotNamespaceAndNotQName_ExcludeNamespacesAndNames()
    {
        var wildcard = Wild("not a;q b#x");

        await Assert.That(wildcard.Allows(new XPathQName("urn:a", "y"))).IsFalse();
        await Assert.That(wildcard.Allows(new XPathQName("urn:b", "x"))).IsFalse();
        await Assert.That(wildcard.Allows(new XPathQName("urn:b", "y"))).IsTrue();
    }

    [Test]
    public async Task Operations_OnNegatedWildcards_FollowTheConstraints()
    {
        var first = Wild("not a", XsdProcessContents.Lax);
        var second = Wild("not b", XsdProcessContents.Skip);

        var union = first.Union(second);
        var intersection = first.Intersect(second);

        await Assert.That(union).IsNotNull();
        await Assert.That(ConstraintOf(union!)).IsEqualTo(NameConstraint.Any);
        await Assert.That(union!.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(intersection).IsNotNull();
        await Assert.That(ConstraintOf(intersection!)).IsEqualTo(NameConstraintTests.Spec("not a b"));
        await Assert.That(intersection!.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(first.IsSubsetOf(Wild("any"))).IsTrue();
        await Assert.That(first.IsSubsetOf(second)).IsFalse();
        await Assert.That(Wild("set b").IsSubsetOf(first)).IsTrue();
    }

    private static NameConstraint ConstraintOf(IWildcard wildcard) => NegatedWildcard.ConstraintOf(wildcard);

    [Test]
    public async Task Operations_KeepTheContextOfTheFirstOperand_OtherwiseThatOfTheSecond()
    {
        var firstNames = new Names(new XPathQName(Tns, "x"));
        var secondNames = new Names(new XPathQName(Tns, "y"));
        var bound = Wild("any;defined", XsdProcessContents.Strict, firstNames, firstNames);
        var other = Wild("any;defined", XsdProcessContents.Strict, secondNames, secondNames);
        var unbound = Wild("any;defined");

        var both = (NegatedWildcard)bound.Intersect(other)!;
        var fallback = (NegatedWildcard)unbound.Intersect(other)!;
        var none = (NegatedWildcard)unbound.Intersect(unbound)!;

        await Assert.That(both.DefinedNames).IsSameReferenceAs(firstNames);
        await Assert.That(both.SiblingNames).IsSameReferenceAs(firstNames);
        await Assert.That(fallback.DefinedNames).IsSameReferenceAs(secondNames);
        await Assert.That(fallback.SiblingNames).IsSameReferenceAs(secondNames);
        await Assert.That(none.DefinedNames).IsNull();
        await Assert.That(none.SiblingNames).IsNull();
    }

    [Test]
    public async Task Operations_AcceptTheWildcardOfXsd10_AsEitherOperandOrder()
    {
        var core = CoreWildcard("##other", XsdProcessContents.Skip);
        var negated = Wild("any;q a#x");

        var intersection = negated.Intersect(core);
        var union = negated.Union(core);

        // ##other of the 1.0 component excludes the target namespace and no namespace.
        await Assert.That(intersection!.Allows(new XPathQName("urn:o", "y"))).IsTrue();
        await Assert.That(intersection.Allows(new XPathQName("urn:t", "y"))).IsFalse();
        await Assert.That(intersection.Allows(new XPathQName(string.Empty, "y"))).IsFalse();
        await Assert.That(intersection.Allows(new XPathQName("urn:a", "x"))).IsFalse();
        await Assert.That(union!.Allows(new XPathQName("urn:t", "y"))).IsTrue();
        await Assert.That(union.Allows(new XPathQName("urn:a", "x"))).IsTrue();
        await Assert.That(Wild("set o").IsSubsetOf(core)).IsTrue();
        await Assert.That(Wild("set t").IsSubsetOf(core)).IsFalse();
        await Assert.That(NegatedWildcard.ConstraintOf(core)).IsEqualTo(NameConstraintTests.Spec("not t L"));
    }

    private static Wildcard CoreWildcard(string value, XsdProcessContents process)
    {
        var processText = process == XsdProcessContents.Skip ? "skip" : "strict";
        _ = Wildcard.TryCreate(value, processText, "urn:t", Where, out var wildcard);
        return wildcard!;
    }

    [Test]
    public async Task ConstraintOf_AnUnknownImplementation_IsRejected()
    {
        var other = new FakeWildcard([], XsdProcessContents.Strict, "urn:a");

        await Assert.That(() => NegatedWildcard.ConstraintOf(other)).Throws<InvalidCastException>();
        await Assert.That(() => Wild("any").Union(other)).Throws<InvalidCastException>();
        await Assert.That(() => NegatedWildcard.ConstraintOf(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Union_WithDifferingKeywords_IsStillExpressible_AndDropsTheKeyword()
    {
        var union = Wild("any;defined").Union(Wild("not a"));

        await Assert.That(union).IsNotNull();
        await Assert.That(ConstraintOf(union!)).IsEqualTo(NameConstraint.Any);
    }

    [Test]
    public async Task TryCreate_NotQNameInAnExcludedNamespace_IsASchemaError()
    {
        var created = TryCreate("notNamespace=\"urn:test\" notQName=\"t:x\"", false, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.WPropsCorrect);
    }

    [Test]
    public async Task TryCreate_NotQNameInAnAllowedNamespace_IsKept()
    {
        var created = TryCreate("namespace=\"##targetNamespace\" notQName=\"t:x\"", false, out var wildcard);

        await Assert.That(created).IsTrue();
        await Assert.That(wildcard!.Allows(new XPathQName(Tns, "x"))).IsFalse();
    }

    [Test]
    public async Task Operations_WithNullArgument_Throw()
    {
        var wildcard = Wild("any");

        await Assert
            .That(() => NegatedWildcard.Create(null!, XsdProcessContents.Strict, Where))
            .Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.Union(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.Intersect(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.IsSubsetOf(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => NegatedWildcard.TryCreate(null!, new SchemaNamespaceScope(Tns, false), false, Reporter, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                NegatedWildcard.TryCreate(Parse("<xs:any/>"), new SchemaNamespaceScope(Tns, false), false, null!, out _)
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Create_RejectsAnInvalidProcessContents()
    {
        var constraint = NameConstraint.Any;

        await Assert
            .That(() => NegatedWildcard.Create(constraint, (XsdProcessContents)99, Where))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => NegatedWildcard.Create(constraint, (XsdProcessContents)(-1), Where))
            .Throws<ArgumentOutOfRangeException>();
    }
}
