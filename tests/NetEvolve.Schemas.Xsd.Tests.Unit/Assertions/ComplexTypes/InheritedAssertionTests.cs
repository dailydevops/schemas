namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class InheritedAssertionTests
{
    private static ComplexTypeAssertion Assertion(string test) => AssertionFixture.Compile([test])[0];

    private static ComplexTypeDefinition Type(SchemaFixture schema, string name, IXsdComponent? derivedFrom = null) =>
        derivedFrom is null
            ? ComplexTypeDefinition.Implicit(SchemaFixture.Header(name), false, null, [], null, schema.Reporter)
            : ComplexTypeDefinition.FromComplexContent(
                SchemaFixture.Header(name),
                XsdDerivationMethod.Extension,
                derivedFrom,
                false,
                null,
                [],
                null,
                schema.Reporter
            )!;

    [Test]
    public async Task Derived_EvaluatesItsOwnAndAllInheritedAssertions()
    {
        var instance = AssertionFixture.Derived(["count(a) = 2"], ["count(b) = 1"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a></r>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("count(a) = 2");
        await Assert.That(issues[1].Message).Contains("count(b) = 1");
    }

    [Test]
    public async Task Derived_InheritedAssertionFails_OwnPasses_ReportsTheInheritedOne()
    {
        var instance = AssertionFixture.Derived(["count(a) = 2"], ["count(b) = 1"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a><b>1</b></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("count(a) = 2");
    }

    [Test]
    public async Task Derived_OwnAssertionFails_InheritedPasses_ReportsTheOwnOne()
    {
        var instance = AssertionFixture.Derived(["count(a) = 1"], ["count(b) = 1"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains("count(b) = 1");
    }

    [Test]
    public async Task Derived_BothPass_ReportsNothing()
    {
        var instance = AssertionFixture.Derived(["count(a) = 1"], ["count(b) = 1"]);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a><b>1</b></r>");

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Derived_WithoutOwnAssertions_StillEvaluatesTheInheritedOnes()
    {
        var instance = AssertionFixture.Derived(["count(a) = 2"], []);

        var issues = AssertionFixture.Run(instance, "\n<r><a>1</a></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Of_BaseAssertionsComeFirst_ThenOwn_TransitivelyThroughTheChain()
    {
        var schema = new SchemaFixture();
        var root = Type(schema, "A");
        var middle = Type(schema, "B", root);
        var leaf = Type(schema, "C", middle);
        var first = Assertion("true()");
        var second = Assertion("false()");
        var third = Assertion("1 = 1");
        var assertions = new ComplexTypeAssertions(
            new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>
            {
                [root] = [first],
                [middle] = [second],
                [leaf] = [third],
            }
        );

        await Assert.That(assertions.Of(leaf)).IsEquivalentTo([first, second, third]);
        await Assert.That(assertions.Of(leaf)[0]).IsSameReferenceAs(first);
        await Assert.That(assertions.Of(leaf)[2]).IsSameReferenceAs(third);
        await Assert.That(assertions.Of(middle)).IsEquivalentTo([first, second]);
        await Assert.That(assertions.Of(root)).IsEquivalentTo([first]);
    }

    [Test]
    public async Task Of_TypeWithoutOwnAssertions_InheritsFromItsBase()
    {
        var schema = new SchemaFixture();
        var root = Type(schema, "A");
        var leaf = Type(schema, "B", root);
        var only = Assertion("true()");
        var assertions = new ComplexTypeAssertions(
            new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> { [root] = [only] }
        );

        await Assert.That(assertions.Of(leaf)).IsEquivalentTo([only]);
    }

    [Test]
    public async Task Of_UnknownTypeNullAndSimpleType_AreEmpty()
    {
        var schema = new SchemaFixture();
        var assertions = new ComplexTypeAssertions(
            new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>
            {
                [Type(schema, "A")] = [Assertion("true()")],
            }
        );

        await Assert.That(assertions.Of(Type(schema, "Other"))).IsEmpty();
        await Assert.That(assertions.Of(null)).IsEmpty();
        await Assert.That(assertions.Of(SchemaFixture.Simple("int"))).IsEmpty();
        await Assert.That(assertions.Of(SchemaFixture.AnyType)).IsEmpty();
    }

    [Test]
    public async Task Of_SameType_GivesTheSameListEveryTime()
    {
        var schema = new SchemaFixture();
        var root = Type(schema, "A");
        var leaf = Type(schema, "B", root);
        var assertions = new ComplexTypeAssertions(
            new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>
            {
                [root] = [Assertion("true()")],
            }
        );

        await Assert.That(assertions.Of(leaf)).IsSameReferenceAs(assertions.Of(leaf));
    }

    [Test]
    public async Task Count_IsTheNumberOfTypesThatDeclareAssertions()
    {
        var schema = new SchemaFixture();
        var assertions = new ComplexTypeAssertions(
            new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>
            {
                [Type(schema, "A")] = [Assertion("true()")],
                [Type(schema, "B")] = [Assertion("true()")],
            }
        );

        await Assert.That(assertions.Count).IsEqualTo(2);
        await Assert
            .That(
                new ComplexTypeAssertions(
                    new Dictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>>()
                ).Count
            )
            .IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_NullTable_Throws() =>
        await Assert.That(() => new ComplexTypeAssertions(null!)).Throws<ArgumentNullException>();
}
