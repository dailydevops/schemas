namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ArithmeticFixture;

public class OperatorTableTests
{
    private static XdmItem Concatenate(in XdmItem left, in XdmItem right)
    {
        _ = left.TryGetString(out var a);
        _ = right.TryGetString(out var b);
        return XdmItem.FromString(a + b);
    }

    private static XdmItem Either(in XdmItem left, in XdmItem right)
    {
        _ = left.TryGetBoolean(out var a);
        _ = right.TryGetBoolean(out var b);
        return XdmItem.FromBoolean(a || b);
    }

    private static XdmItem Marker(in XdmItem left, in XdmItem right) => XdmItem.FromString("marker");

    private static XdmItem NegateString(in XdmItem operand)
    {
        _ = operand.TryGetString(out var text);
        return XdmItem.FromString("-" + text);
    }

    [Test]
    public async Task EntryRegisteredFromTheTestProject_ForAnotherType_IsFoundAndCalledByTheDispatch()
    {
        var table = Table();
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, Concatenate);

        var result = Run(
            ArithmeticOperator.Add,
            XdmSequence.Create(Item("s:ab")),
            XdmSequence.Create(Item("s:cd")),
            table
        );

        await Assert.That(Describe(result)).IsEqualTo("string:abcd");
    }

    [Test]
    public async Task EntryForAnotherType_DoesNotDisturbTheNumericEntries()
    {
        var table = Table();
        table.Register(ArithmeticOperator.Add, XdmTypeId.Boolean, XdmTypeId.Boolean, Either);

        var flags = Run(
            ArithmeticOperator.Add,
            XdmSequence.Create(Item("b:false")),
            XdmSequence.Create(Item("b:true")),
            table
        );
        var numbers = Run(
            ArithmeticOperator.Add,
            XdmSequence.Create(Item("i:2")),
            XdmSequence.Create(Item("i:3")),
            table
        );

        await Assert.That(Describe(flags)).IsEqualTo("boolean:True");
        await Assert.That(Describe(numbers)).IsEqualTo("integer:5");
    }

    [Test]
    public async Task EntryOfAnOperatorOnly_LeavesTheOtherOperatorsWithoutAnImplementation()
    {
        var table = Table();
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, Concatenate);

        var error = Raised(() =>
            Run(ArithmeticOperator.Subtract, XdmSequence.Create(Item("s:ab")), XdmSequence.Create(Item("s:cd")), table)
        );

        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPTY0004);
    }

    [Test]
    public async Task UnaryEntryRegisteredFromTheTestProject_IsFoundAndCalled()
    {
        var table = Table();
        table.Register(UnaryOperator.Minus, XdmTypeId.XsString, NegateString);
        var node = new UnaryArithmeticNode(
            UnaryOperator.Minus,
            new ConstantNode(XdmSequence.Create(Item("s:x"))),
            table
        );

        using var context = new NetEvolve.Schemas.XPath.Context.XPathEvaluationContext();

        await Assert.That(Describe(node.Evaluate(context))).IsEqualTo("string:-x");
    }

    [Test]
    public async Task TryGet_FindsARegisteredEntryAndNotAnUnregisteredOne()
    {
        var table = new ArithmeticOperatorTable();
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, Concatenate);

        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, out var found))
            .IsTrue();
        await Assert.That(found).IsNotNull();
        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.Boolean, out var missing))
            .IsFalse();
        await Assert.That(missing).IsNull();
        await Assert
            .That(table.TryGet(ArithmeticOperator.Subtract, XdmTypeId.XsString, XdmTypeId.XsString, out _))
            .IsFalse();
        await Assert.That(table.TryGet(UnaryOperator.Minus, XdmTypeId.XsString, out _)).IsFalse();
    }

    [Test]
    public async Task Register_TheSameKeyTwice_Throws()
    {
        var table = new ArithmeticOperatorTable();
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, Concatenate);
        table.Register(UnaryOperator.Minus, XdmTypeId.XsString, NegateString);

        await Assert
            .That(() => table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, Marker))
            .Throws<InvalidOperationException>();
        await Assert
            .That(() => table.Register(UnaryOperator.Minus, XdmTypeId.XsString, NegateString))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_NumericOperatorsTwice_Throws()
    {
        var table = Table();

        await Assert.That(() => NumericOperators.Register(table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_NullArguments_Throw()
    {
        var table = new ArithmeticOperatorTable();

        await Assert
            .That(() => table.Register(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => table.Register(UnaryOperator.Minus, XdmTypeId.XsString, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => NumericOperators.Register(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Register_AnOperatorOutsideTheEnumeration_Throws()
    {
        var table = new ArithmeticOperatorTable();

        await Assert
            .That(() => table.Register((ArithmeticOperator)99, XdmTypeId.XsString, XdmTypeId.XsString, Concatenate))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => table.Register((UnaryOperator)99, XdmTypeId.XsString, NegateString))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EveryNumericPair_OfEveryOperator_HasAnEntry()
    {
        var table = Table();
        XdmTypeId[] numeric = [XdmTypeId.XsInteger, XdmTypeId.XsDecimal, XdmTypeId.XsFloat, XdmTypeId.XsDouble];
        var missing = 0;

        foreach (var op in Enum.GetValues<ArithmeticOperator>())
        {
            foreach (var left in numeric)
            {
                foreach (var right in numeric)
                {
                    missing += table.TryGet(op, left, right, out _) ? 0 : 1;
                }
            }
        }

        foreach (var op in Enum.GetValues<UnaryOperator>())
        {
            foreach (var operand in numeric)
            {
                missing += table.TryGet(op, operand, out _) ? 0 : 1;
            }
        }

        await Assert.That(missing).IsEqualTo(0);
    }

    [Test]
    public async Task TryGet_ForADerivedType_FindsTheEntryOfTheMostSpecificRegisteredBase()
    {
        // xs:int derives from xs:long, xs:integer, and xs:decimal: the integer entry is the most specific one registered, and a decimal operand beside it leaves the decimal entry.
        var table = new ArithmeticOperatorTable();
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsDecimal, XdmTypeId.XsDecimal, Marker);
        table.Register(ArithmeticOperator.Add, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Concatenate);
        var integerType = XdmTypes.Get("int");

        await Assert.That(table.TryGet(ArithmeticOperator.Add, integerType, integerType, out var both)).IsTrue();
        await Assert.That(both).IsEqualTo(new BinaryOperatorImplementation(Concatenate));
        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, integerType, XdmTypeId.XsDecimal, out var mixed))
            .IsTrue();
        await Assert.That(mixed).IsEqualTo(new BinaryOperatorImplementation(Marker));
        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, XdmTypes.Get("byte"), XdmTypes.Get("short"), out var small))
            .IsTrue();
        await Assert.That(small).IsEqualTo(new BinaryOperatorImplementation(Concatenate));
    }

    [Test]
    public async Task TryGet_ForATypeWithoutARegisteredBase_FindsNothing()
    {
        var table = Table();

        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsInteger, out _))
            .IsFalse();
        await Assert
            .That(table.TryGet(ArithmeticOperator.Add, XdmTypeId.UntypedAtomic, XdmTypeId.XsInteger, out _))
            .IsFalse();
        await Assert.That(table.TryGet(ArithmeticOperator.Add, default, default, out _)).IsFalse();
    }

    [Test]
    public async Task TryGet_ForADerivedNumericType_FindsTheNumericEntry()
    {
        var table = Table();

        await Assert
            .That(table.TryGet(ArithmeticOperator.Multiply, XdmTypes.Get("unsignedInt"), XdmTypes.Get("double"), out _))
            .IsTrue();
        await Assert.That(table.TryGet(UnaryOperator.Minus, XdmTypes.Get("negativeInteger"), out _)).IsTrue();
    }
}
