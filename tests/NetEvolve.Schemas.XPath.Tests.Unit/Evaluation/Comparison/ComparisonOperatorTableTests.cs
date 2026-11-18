namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The dispatch table of the comparisons: keyed by relation and operand types, open for entries from other folders.</summary>
public class ComparisonOperatorTableTests
{
    private static readonly ComparisonRelation[] Relations =
    [
        ComparisonRelation.Equal,
        ComparisonRelation.NotEqual,
        ComparisonRelation.LessThan,
        ComparisonRelation.LessThanOrEqual,
        ComparisonRelation.GreaterThan,
        ComparisonRelation.GreaterThanOrEqual,
    ];

    private static readonly XdmTypeId[] Numeric =
    [
        XdmTypeId.XsInteger,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
    ];

    private static bool Always(ComparisonRelation relation, in XdmItem left, in XdmItem right) => true;

    private static bool Never(ComparisonRelation relation, in XdmItem left, in XdmItem right) => false;

    [Test]
    public async Task Registration_CoversEveryPairOfNumericTypes_ForEveryRelation()
    {
        var table = Table();

        foreach (var relation in Relations)
        {
            foreach (var left in Numeric)
            {
                foreach (var right in Numeric)
                {
                    await Assert.That(table.TryGet(relation, left, right, out _)).IsTrue();
                }
            }
        }
    }

    [Test]
    public async Task Registration_CoversStringAndAnyUriInEveryCombination_ForEveryRelation()
    {
        var table = Table();
        XdmTypeId[] texts = [XdmTypeId.XsString, XdmTypeId.AnyUri];

        foreach (var relation in Relations)
        {
            foreach (var left in texts)
            {
                foreach (var right in texts)
                {
                    await Assert.That(table.TryGet(relation, left, right, out _)).IsTrue();
                }
            }

            await Assert.That(table.TryGet(relation, XdmTypeId.Boolean, XdmTypeId.Boolean, out _)).IsTrue();
        }
    }

    [Test]
    public async Task Registration_LeavesTheMixedFamilies_WithoutAnEntry()
    {
        var table = Table();

        foreach (var relation in Relations)
        {
            await Assert.That(table.TryGet(relation, XdmTypeId.XsString, XdmTypeId.XsInteger, out _)).IsFalse();
            await Assert.That(table.TryGet(relation, XdmTypeId.Boolean, XdmTypeId.XsInteger, out _)).IsFalse();
            await Assert.That(table.TryGet(relation, XdmTypeId.XsDouble, XdmTypeId.XsString, out _)).IsFalse();
            await Assert.That(table.TryGet(relation, XdmTypeId.UntypedAtomic, XdmTypeId.XsString, out _)).IsFalse();
        }
    }

    [Test]
    public async Task EntryRegisteredFromTheTestProject_ForAnotherType_IsFoundAndCalledByTheDispatch()
    {
        // A test project cannot add an item kind, so the fake type is a pair of an existing type that the numeric registration leaves out.
        var table = Table();
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsInteger, Always);

        await Assert.That(Show(Run(Operator("eq"), One("s:a"), One("i:1"), table))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), One("s:a"), One("i:1"), table))).IsEqualTo("true");
        await Assert.That(Outcome(() => Run(Operator("ne"), One("s:a"), One("i:1"), table))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task EntryForAnotherType_DoesNotDisturbTheRegisteredFamilies()
    {
        var table = Table();
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsInteger, Always);

        await Assert.That(Show(Run(Operator("eq"), One("i:1"), One("i:2"), table))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("eq"), One("s:a"), One("s:a"), table))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("lt"), One("b:false"), One("b:true"), table))).IsEqualTo("true");
    }

    [Test]
    public async Task AnEntryAddedAfterTheNodeWasCreated_IsSeenByTheNode()
    {
        var table = new ComparisonOperatorTable();
        var left = new ComparisonFixture.ConstantOperand(One("i:1"));
        var right = new ComparisonFixture.ConstantOperand(One("i:2"));
        var node = new ValueComparisonNode(ComparisonOperator.ValueEqual, left, right, table);
        using var context = new NetEvolve.Schemas.XPath.Context.XPathEvaluationContext();

        await Assert.That(Raises(node, context)).IsTrue();

        table.Register(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Never);

        await Assert.That(Show(node.Evaluate(context))).IsEqualTo("false");

        static bool Raises(ValueComparisonNode node, NetEvolve.Schemas.XPath.Context.XPathEvaluationContext context)
        {
            try
            {
                _ = node.Evaluate(context);
                return false;
            }
            catch (NetEvolve.Schemas.XPath.Errors.XPathException)
            {
                return true;
            }
        }
    }

    [Test]
    public async Task TheEntryOfAnotherRelation_IsNotUsed()
    {
        var table = new ComparisonOperatorTable();
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Always);

        await Assert
            .That(table.TryGet(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsInteger, out _))
            .IsTrue();
        await Assert
            .That(table.TryGet(ComparisonRelation.NotEqual, XdmTypeId.XsInteger, XdmTypeId.XsInteger, out _))
            .IsFalse();
    }

    [Test]
    public async Task TheEntryIsKeyedByTheOrderOfTheTypes()
    {
        var table = new ComparisonOperatorTable();
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsInteger, Always);

        await Assert
            .That(table.TryGet(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsInteger, out _))
            .IsTrue();
        await Assert
            .That(table.TryGet(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsString, out _))
            .IsFalse();
    }

    [Test]
    public async Task TheImplementationReceivesTheRelationItWasLookedUpWith()
    {
        var seen = ComparisonRelation.Equal;
        var table = new ComparisonOperatorTable();
        ComparisonImplementation capture = (ComparisonRelation relation, in XdmItem left, in XdmItem right) =>
        {
            seen = relation;
            return true;
        };
        table.Register(ComparisonRelation.GreaterThanOrEqual, XdmTypeId.XsString, XdmTypeId.XsString, capture);

        _ = table.TryGet(ComparisonRelation.GreaterThanOrEqual, XdmTypeId.XsString, XdmTypeId.XsString, out var found);
        var item = XdmItem.FromString("a");
        _ = found!(ComparisonRelation.GreaterThanOrEqual, item, item);

        await Assert.That(seen).IsEqualTo(ComparisonRelation.GreaterThanOrEqual);
    }

    [Test]
    public async Task Register_ForAPairThatHasAnEntry_Throws()
    {
        var table = new ComparisonOperatorTable();
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsString, Always);

        await Assert
            .That(() => table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsString, Never))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_OfTheComparisonOperators_Twice_Throws()
    {
        var table = Table();

        await Assert.That(() => ComparisonOperators.Register(table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TryGet_ForADerivedType_FindsTheEntryOfTheBaseType()
    {
        var table = Table();
        var integer = XdmTypes.Get("int");
        var normalized = XdmTypes.Get("normalizedString");

        await Assert.That(table.TryGet(ComparisonRelation.LessThan, integer, XdmTypeId.XsDouble, out _)).IsTrue();
        await Assert.That(table.TryGet(ComparisonRelation.Equal, integer, integer, out _)).IsTrue();
        await Assert.That(table.TryGet(ComparisonRelation.Equal, normalized, XdmTypeId.XsString, out _)).IsTrue();
        await Assert.That(table.TryGet(ComparisonRelation.Equal, normalized, integer, out _)).IsFalse();
    }

    [Test]
    public async Task TryGet_FindsTheMostSpecificEntry_AmongTheEntriesASubstitutionMatches()
    {
        var table = new ComparisonOperatorTable();
        var integer = XdmTypes.Get("int");
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsDecimal, XdmTypeId.XsDecimal, Always);
        table.Register(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Never);

        _ = table.TryGet(ComparisonRelation.Equal, integer, integer, out var found);
        var item = XdmItem.FromInteger(1);

        await Assert.That(found!(ComparisonRelation.Equal, item, item)).IsFalse();
    }

    [Test]
    public async Task TryGet_ForATypeWithoutAnEntry_ReturnsFalse_AndNoImplementation()
    {
        var table = new ComparisonOperatorTable();

        var found = table.TryGet(
            ComparisonRelation.Equal,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            out var implementation
        );

        await Assert.That(found).IsFalse();
        await Assert.That(implementation).IsNull();
    }

    [Test]
    public async Task ADerivedTypeLookup_AllocatesNothing()
    {
        var table = Table();
        var integer = XdmTypes.Get("int");
        var found = false;

        AllocationAssert.NoAllocation(
            () => found = table.TryGet(ComparisonRelation.Equal, integer, integer, out _),
            warmups: 50
        );

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task AnExactLookup_AllocatesNothing()
    {
        var table = Table();
        var found = false;

        AllocationAssert.NoAllocation(
            () => found = table.TryGet(ComparisonRelation.LessThan, XdmTypeId.XsDouble, XdmTypeId.XsInteger, out _),
            warmups: 50
        );

        await Assert.That(found).IsTrue();
    }

    [Test]
    public async Task ARelationThatIsNotDefined_IsRejected()
    {
        var table = new ComparisonOperatorTable();

        await Assert
            .That(() => table.TryGet((ComparisonRelation)(-1), XdmTypeId.XsString, XdmTypeId.XsString, out _))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => table.TryGet((ComparisonRelation)6, XdmTypeId.XsString, XdmTypeId.XsString, out _))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => table.Register((ComparisonRelation)6, XdmTypeId.XsString, XdmTypeId.XsString, Always))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ANullImplementation_IsRejected()
    {
        var table = new ComparisonOperatorTable();

        await Assert
            .That(() => table.Register(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsString, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ANullTable_IsRejectedByTheRegistration() =>
        await Assert.That(() => ComparisonOperators.Register(null!)).Throws<ArgumentNullException>();
}
