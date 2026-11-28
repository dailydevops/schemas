namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class TemporalOperatorTableTests
{
    private const string TemporalNamespace = "NetEvolve.Schemas.XPath.Evaluation.Temporal";

    private static readonly Type[] TemporalValueTypes = [typeof(XsdDateTime), typeof(XsdDuration)];
    private static readonly XdmTypeId Duration = XdmTypeId.Duration;
    private static readonly XdmTypeId YearMonth = XdmTypeId.YearMonthDuration;
    private static readonly XdmTypeId DayTime = XdmTypeId.DayTimeDuration;
    private static readonly XdmTypeId DateTime = XdmTypeId.DateTime;
    private static readonly XdmTypeId Date = XdmTypeId.Date;
    private static readonly XdmTypeId Time = XdmTypeId.Time;

    private static readonly XdmTypeId[] Numeric =
    [
        XdmTypeId.XsInteger,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
    ];

    private static readonly XdmTypeId[] Gregorian =
    [
        XdmTypes.Get("gYearMonth"),
        XdmTypes.Get("gYear"),
        XdmTypes.Get("gMonthDay"),
        XdmTypes.Get("gDay"),
        XdmTypes.Get("gMonth"),
    ];

    private static readonly XdmTypeId[] EqualityOnly = [Duration, YearMonth, DayTime, .. Gregorian];

    private static readonly XdmTypeId[] NoOrder = [Duration, .. Gregorian];

    public static IEnumerable<Func<(int Operator, int Left, int Right)>> ArithmeticEntries()
    {
        foreach (var (op, left, right) in ExpectedArithmetic())
        {
            var captured = ((int)op, left.Value, right.Value);
            yield return () => captured;
        }
    }

    public static IEnumerable<Func<(int Operator, int Left, int Right)>> MissingEntries()
    {
        (ArithmeticOperator, XdmTypeId, XdmTypeId)[] missing =
        [
            (ArithmeticOperator.Add, Date, Date),
            (ArithmeticOperator.Add, DateTime, DateTime),
            (ArithmeticOperator.Add, Time, Time),
            (ArithmeticOperator.Add, Time, YearMonth),
            (ArithmeticOperator.Add, YearMonth, DayTime),
            (ArithmeticOperator.Add, Duration, Duration),
            (ArithmeticOperator.Add, Duration, DateTime),
            (ArithmeticOperator.Subtract, DayTime, DateTime),
            (ArithmeticOperator.Subtract, Date, DateTime),
            (ArithmeticOperator.Subtract, Time, YearMonth),
            (ArithmeticOperator.Subtract, Duration, Duration),
            (ArithmeticOperator.Multiply, DayTime, DayTime),
            (ArithmeticOperator.Multiply, Duration, XdmTypeId.XsDouble),
            (ArithmeticOperator.Multiply, Date, XdmTypeId.XsDouble),
            (ArithmeticOperator.Divide, Duration, Duration),
            (ArithmeticOperator.Divide, YearMonth, DayTime),
            (ArithmeticOperator.Divide, XdmTypeId.XsDouble, DayTime),
            (ArithmeticOperator.IntegerDivide, DayTime, DayTime),
            (ArithmeticOperator.IntegerDivide, DayTime, XdmTypeId.XsDouble),
            (ArithmeticOperator.Modulo, DayTime, XdmTypeId.XsDouble),
            (ArithmeticOperator.Modulo, YearMonth, YearMonth),
        ];
        foreach (var (op, left, right) in missing)
        {
            var captured = ((int)op, left.Value, right.Value);
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(ArithmeticEntries))]
    public async Task EveryTemporalArithmeticOperator_IsFoundThroughTheOperatorTable(int @operator, int left, int right)
    {
        var table = new ArithmeticOperatorTable();
        TemporalOperators.Register(table);

        await Assert
            .That(
                table.TryGetContextual(
                    (ArithmeticOperator)@operator,
                    new XdmTypeId(left),
                    new XdmTypeId(right),
                    out var implementation
                )
            )
            .IsTrue();
        await Assert.That(implementation).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(MissingEntries))]
    public async Task APairWithoutAnOperatorInTheSpecification_HasNoEntry(int @operator, int left, int right)
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        TemporalOperators.Register(table);

        await Assert
            .That(
                table.TryGetContextual((ArithmeticOperator)@operator, new XdmTypeId(left), new XdmTypeId(right), out _)
            )
            .IsFalse();
    }

    [Test]
    public async Task TheTemporalEntries_AreExactlyThoseOfTheOperatorMapping()
    {
        var table = new ArithmeticOperatorTable();
        TemporalOperators.Register(table);
        XdmTypeId[] all = [Duration, YearMonth, DayTime, DateTime, Date, Time, .. Numeric];
        var found = 0;

        foreach (var op in Enum.GetValues<ArithmeticOperator>())
        {
            foreach (var left in all)
            {
                foreach (var right in all)
                {
                    found += table.TryGetContextual(op, left, right, out _) ? 1 : 0;
                }
            }
        }

        await Assert.That(found).IsEqualTo(ExpectedArithmetic().Length);
    }

    [Test]
    public async Task TheTemporalEntries_AreDistinctFromTheNumericOnes_SoBothFamiliesFillOneTable()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        TemporalOperators.Register(table);

        await Assert
            .That(table.TryGetContextual(ArithmeticOperator.Add, XdmTypeId.XsInteger, XdmTypeId.XsDouble, out _))
            .IsTrue();
        await Assert.That(table.TryGetContextual(ArithmeticOperator.Add, YearMonth, YearMonth, out _)).IsTrue();
        await Assert.That(() => TemporalOperators.Register(table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EveryTemporalComparison_IsFoundThroughTheComparisonTable()
    {
        var table = new ComparisonOperatorTable();
        TemporalOperators.Register(table);
        var missing = new List<string>();

        for (var relation = ComparisonRelation.Equal; relation <= ComparisonRelation.GreaterThanOrEqual; relation++)
        {
            foreach (var type in new[] { DateTime, Date, Time, YearMonth, DayTime })
            {
                if (!table.TryGetContextual(relation, type, type, out _))
                {
                    missing.Add(relation + " " + type.Value);
                }
            }
        }

        foreach (var relation in new[] { ComparisonRelation.Equal, ComparisonRelation.NotEqual })
        {
            foreach (var type in EqualityOnly)
            {
                if (!table.TryGetContextual(relation, type, type, out _))
                {
                    missing.Add(relation + " " + type.Value);
                }
            }
        }

        await Assert.That(missing).IsEmpty();
        // A year-month and a day-time duration are durations, so equality finds the entry of the base type by substitution.
        await Assert.That(table.TryGetContextual(ComparisonRelation.Equal, YearMonth, DayTime, out _)).IsTrue();
    }

    [Test]
    public async Task AnOrderingOfDurationsOrOfTheGregorianTypes_HasNoEntry()
    {
        var table = new ComparisonOperatorTable();
        TemporalOperators.Register(table);
        var found = 0;

        for (var relation = ComparisonRelation.LessThan; relation <= ComparisonRelation.GreaterThanOrEqual; relation++)
        {
            foreach (var type in NoOrder)
            {
                found += table.TryGetContextual(relation, type, type, out _) ? 1 : 0;
            }

            found += table.TryGetContextual(relation, YearMonth, DayTime, out _) ? 1 : 0;
            found += table.TryGetContextual(relation, YearMonth, Duration, out _) ? 1 : 0;
        }

        await Assert.That(found).IsEqualTo(0);
        await Assert.That(table.TryGetContextual(ComparisonRelation.Equal, Date, DateTime, out _)).IsFalse();
        await Assert
            .That(table.TryGetContextual(ComparisonRelation.Equal, Gregorian[0], Gregorian[1], out _))
            .IsFalse();
        await Assert.That(() => TemporalOperators.Register(table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TheNumericTypesAndTheTables_HaveNoReferenceToTheTemporalTypes()
    {
        // The folders of the numeric arithmetic and of the comparisons are the shared files of this work package; they know entries
        // only as delegates in a table, and the registration of the temporal entries is the single place that knows the temporal types.
        // The scan reads the types of the Evaluation.Temporal namespace and the value types XsdDateTime and XsdDuration, in signatures,
        // fields, locals, and the call targets of the IL. Looser reading: the kinds XdmItemKind.Date, Time, DateTime, and the durations are
        // not scanned. A member of an enumeration compiles to an integer constant, which the IL does not tell from any other integer, and
        // the comparison folder names these kinds on purpose: ComparisonOperands maps the kind of an operand to the cast target of an
        // xs:untypedAtomic operand, which is no knowledge of an operator.
        var assembly = typeof(TemporalOperators).Assembly;
        var violations = new List<string>();
        var checkedTypes = 0;
        foreach (var type in assembly.GetTypes())
        {
            var ns = type.Namespace ?? string.Empty;
            if (
                !ns.EndsWith("Evaluation.Arithmetic", StringComparison.Ordinal)
                && !ns.EndsWith("Evaluation.Comparison", StringComparison.Ordinal)
            )
            {
                continue;
            }

            checkedTypes++;
            Scan(type, violations);
        }

        await Assert.That(checkedTypes).IsGreaterThan(20);
        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task TheScanOfTheNumericTypes_ReportsAFixtureThatReferencesATemporalType()
    {
        var violations = new List<string>();

        Scan(typeof(ReferencingFixture), violations);

        await Assert.That(violations).IsNotEmpty();
    }

    [Test]
    [Arguments(typeof(ReferencingDurationFixture))]
    [Arguments(typeof(ReferencingDateTimeFixture))]
    public async Task TheScanOfTheNumericTypes_ReportsAFixtureThatReferencesAnXsdTemporalValueType(Type fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var violations = new List<string>();

        Scan(fixture, violations);

        await Assert.That(violations).IsNotEmpty();
    }

    [Test]
    public async Task ARegisteredTemporalOperator_IsReadByTheNodesOfTheArithmeticFolder_WithoutEditingThem()
    {
        // The arithmetic lowering reads the table at evaluation: a table with the numeric entries only rejects the pair, the same table
        // with the temporal registration added after the node was created accepts it.
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        var node = new ArithmeticNode(
            ArithmeticOperator.Add,
            new TemporalFixture.ConstantNode(XdmSequence.Create(DurationItem("PT1S"))),
            new TemporalFixture.ConstantNode(XdmSequence.Create(DurationItem("PT2S"))),
            table
        );
        using var context = new XPathEvaluationContext();

        await Assert.That(() => node.Evaluate(context)).Throws<NetEvolve.Schemas.XPath.Errors.XPathException>();

        TemporalOperators.Register(table);

        await Assert.That(TemporalFixture.Describe(node.Evaluate(context))).IsEqualTo("DayTimeDuration:PT3S");
    }

    private static XdmItem DurationItem(string lexical) =>
        XdmItem.FromDuration(
            NetEvolve
                .Schemas.Xml.Datatypes.Duration.XsdDuration.Parse(
                    lexical.AsSpan(),
                    NetEvolve.Schemas.Xml.Datatypes.Duration.DurationKind.DayTime
                )
                .Value,
            NetEvolve.Schemas.Xml.Datatypes.Duration.DurationKind.DayTime
        );

    private static (ArithmeticOperator, XdmTypeId, XdmTypeId)[] ExpectedArithmetic()
    {
        var entries = new List<(ArithmeticOperator, XdmTypeId, XdmTypeId)>
        {
            (ArithmeticOperator.Add, YearMonth, YearMonth),
            (ArithmeticOperator.Add, DayTime, DayTime),
            (ArithmeticOperator.Subtract, YearMonth, YearMonth),
            (ArithmeticOperator.Subtract, DayTime, DayTime),
            (ArithmeticOperator.Divide, YearMonth, YearMonth),
            (ArithmeticOperator.Divide, DayTime, DayTime),
        };
        foreach (var moment in new[] { DateTime, Date })
        {
            entries.Add((ArithmeticOperator.Add, moment, YearMonth));
            entries.Add((ArithmeticOperator.Add, YearMonth, moment));
            entries.Add((ArithmeticOperator.Add, moment, DayTime));
            entries.Add((ArithmeticOperator.Add, DayTime, moment));
            entries.Add((ArithmeticOperator.Subtract, moment, YearMonth));
            entries.Add((ArithmeticOperator.Subtract, moment, DayTime));
        }

        entries.Add((ArithmeticOperator.Add, Time, DayTime));
        entries.Add((ArithmeticOperator.Add, DayTime, Time));
        entries.Add((ArithmeticOperator.Subtract, Time, DayTime));
        foreach (var moment in new[] { DateTime, Date, Time })
        {
            entries.Add((ArithmeticOperator.Subtract, moment, moment));
        }

        foreach (var number in Numeric)
        {
            foreach (var duration in new[] { YearMonth, DayTime })
            {
                entries.Add((ArithmeticOperator.Multiply, duration, number));
                entries.Add((ArithmeticOperator.Multiply, number, duration));
                entries.Add((ArithmeticOperator.Divide, duration, number));
            }
        }

        return [.. entries];
    }

    // Reads the signatures, the fields, and the IL of the methods of a type for tokens of the temporal namespace.
    private static void Scan(Type type, List<string> violations)
    {
        const BindingFlags all =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        foreach (var field in type.GetFields(all))
        {
            Collect(field.FieldType, type, violations);
        }

        foreach (var method in type.GetMethods(all))
        {
            ScanMethod(method, type, violations);
        }

        foreach (var constructor in type.GetConstructors(all))
        {
            ScanMethod(constructor, type, violations);
        }
    }

    private static void ScanMethod(MethodBase method, Type owner, List<string> violations)
    {
        foreach (var parameter in method.GetParameters())
        {
            Collect(parameter.ParameterType, owner, violations);
        }

        if (method is MethodInfo info)
        {
            Collect(info.ReturnType, owner, violations);
        }

        var body = method.GetMethodBody();
        var il = body?.GetILAsByteArray();
        if (body is null || il is null)
        {
            return;
        }

        foreach (var local in body.LocalVariables)
        {
            Collect(local.LocalType, owner, violations);
        }

        // call, callvirt, newobj, ldfld, ldsfld, and ldftn tokens; a token that does not resolve is no operand and is skipped.
        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] is not (0x28 or 0x6F or 0x73 or 0x7B or 0x7E or 0xFE))
            {
                continue;
            }

            var offset = il[i] == 0xFE ? i + 2 : i + 1;
            if (offset + 4 > il.Length)
            {
                continue;
            }

            MemberInfo? target;
            try
            {
                target = method.Module.ResolveMember(BitConverter.ToInt32(il, offset));
            }
            catch (Exception ex) when (ex is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            if (target?.DeclaringType is { } declaring)
            {
                Collect(declaring, owner, violations);
            }
        }
    }

    private static void Collect(Type used, Type owner, List<string> violations)
    {
        var element = used.HasElementType ? used.GetElementType()! : used;
        if (
            (element.Namespace ?? string.Empty).StartsWith(TemporalNamespace, StringComparison.Ordinal)
            || Array.IndexOf(TemporalValueTypes, element) >= 0
        )
        {
            violations.Add($"{owner.Name} references {element.FullName}");
        }

        if (element.IsGenericType)
        {
            foreach (var argument in element.GetGenericArguments())
            {
                Collect(argument, owner, violations);
            }
        }
    }

    /// <summary>The violating fixture: a type of the arithmetic folder shape that takes a duration value.</summary>
    private static class ReferencingDurationFixture
    {
        internal static XsdDuration Zero() => default;
    }

    /// <summary>The violating fixture: a type of the comparison folder shape that takes a dateTime value.</summary>
    private static class ReferencingDateTimeFixture
    {
        internal static bool IsDefault(in XsdDateTime value) => value.Equals(default);
    }

    /// <summary>The violating fixture: a type of the arithmetic folder shape that calls the temporal registration.</summary>
    private static class ReferencingFixture
    {
        internal static void Fill(ArithmeticOperatorTable table) => TemporalOperators.Register(table);
    }
}
