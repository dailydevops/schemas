namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The operator mapping of XPath 2.0, B.2: the implementation of an arithmetic operator by the types of its operands. The table is
/// open: the numeric folder fills it through <see cref="NumericOperators"/>, and the folder of another type family (dates, times,
/// and durations) adds its entries through a registration of its own, without a shared list or a switch. A lookup finds the entry
/// of the exact types first and then applies subtype substitution: an operand of a derived type uses the entry of the base type it
/// derives from, the most specific entry winning.
/// </summary>
/// <remarks>
/// <para>
/// Not thread-safe for registration; fill it before compiling. Reading is safe for concurrent evaluations. Limitation: a missing
/// entry is not reported here; the caller raises XPTY0004, which XPath 2.0, 3.4 defines for operand types without an operator.
/// </para>
/// <para>
/// An entry is registered with <see cref="BinaryOperatorImplementation"/> when it needs the operands only, and with
/// <see cref="ContextBinaryOperatorImplementation"/> when it reads the dynamic context, as the operators of dates and times do for the
/// implicit timezone. The evaluation looks entries up with <c>TryGetContextual</c>, which finds both kinds; <c>TryGet</c> finds the
/// entries registered without the context only.
/// </para>
/// </remarks>
internal sealed class ArithmeticOperatorTable
{
    private readonly Dictionary<(int Operator, int Left, int Right), BinaryEntry> _binary = [];
    private readonly Dictionary<(int Operator, int Operand), UnaryOperatorImplementation> _unary = [];
    private readonly List<BinaryEntry>[] _binaryEntries;
    private readonly List<UnaryEntry>[] _unaryEntries;

    /// <summary>Initializes a new instance of the <see cref="ArithmeticOperatorTable"/> class, empty.</summary>
    public ArithmeticOperatorTable()
    {
        _binaryEntries = new List<BinaryEntry>[(int)ArithmeticOperator.Modulo + 1];
        for (var i = 0; i < _binaryEntries.Length; i++)
        {
            _binaryEntries[i] = [];
        }

        _unaryEntries = new List<UnaryEntry>[(int)UnaryOperator.Minus + 1];
        for (var i = 0; i < _unaryEntries.Length; i++)
        {
            _unaryEntries[i] = [];
        }
    }

    /// <summary>Registers the implementation of a binary operator for a pair of operand types.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <exception cref="InvalidOperationException">The operator and the pair of types already have an implementation.</exception>
    public void Register(
        ArithmeticOperator @operator,
        XdmTypeId left,
        XdmTypeId right,
        BinaryOperatorImplementation implementation
    )
    {
        CheckOperator(@operator);
        ArgumentNullException.ThrowIfNull(implementation);

        Add(
            @operator,
            new BinaryEntry(left, right, implementation, (in XdmItem a, in XdmItem b, XPathEvaluationContext _) =>
                implementation(a, b)
            )
        );
    }

    /// <summary>Registers a binary operator for a pair of operand types with a form that reads the dynamic context and a plain form for callers without one.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="plain">The form without the context.</param>
    /// <param name="contextual">The form with the context; the evaluation uses it.</param>
    /// <exception cref="ArgumentNullException">An implementation is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <exception cref="InvalidOperationException">The operator and the pair of types already have an implementation.</exception>
    public void Register(
        ArithmeticOperator @operator,
        XdmTypeId left,
        XdmTypeId right,
        BinaryOperatorImplementation plain,
        ContextBinaryOperatorImplementation contextual
    )
    {
        CheckOperator(@operator);
        ArgumentNullException.ThrowIfNull(plain);
        ArgumentNullException.ThrowIfNull(contextual);

        Add(@operator, new BinaryEntry(left, right, plain, contextual));
    }

    /// <summary>Registers the implementation of a binary operator for a pair of operand types that reads the dynamic context.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <exception cref="InvalidOperationException">The operator and the pair of types already have an implementation.</exception>
    public void RegisterContextual(
        ArithmeticOperator @operator,
        XdmTypeId left,
        XdmTypeId right,
        ContextBinaryOperatorImplementation implementation
    )
    {
        CheckOperator(@operator);
        ArgumentNullException.ThrowIfNull(implementation);

        Add(@operator, new BinaryEntry(left, right, null, implementation));
    }

    /// <summary>Registers the implementation of a unary operator for an operand type.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="operand">The type of the operand.</param>
    /// <param name="implementation">The implementation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="implementation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <exception cref="InvalidOperationException">The operator and the type already have an implementation.</exception>
    public void Register(UnaryOperator @operator, XdmTypeId operand, UnaryOperatorImplementation implementation)
    {
        CheckOperator(@operator);
        ArgumentNullException.ThrowIfNull(implementation);

        var key = ((int)@operator, operand.Value);
        if (_unary.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"An implementation of '{@operator}' for the operand type {operand.Value} is already registered."
            );
        }

        _unary.Add(key, implementation);
        _unaryEntries[(int)@operator].Add(new UnaryEntry(operand, implementation));
    }

    /// <summary>Finds the implementation of a binary operator for a pair of operand types.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation, <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when an entry matches the types exactly or by subtype substitution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <remarks>
    /// Allocates nothing. The exact entry is one dictionary lookup; substitution scans the entries of the operator. When two entries
    /// match and the types of neither derive from the types of the other (for example <c>(decimal, integer)</c> and
    /// <c>(integer, decimal)</c> for two integers), the one registered first wins. It returns <see langword="false"/> when the most specific
    /// match is a contextual entry, even if a less specific plain entry also matches; <c>TryGetContextual</c> finds both kinds.
    /// </remarks>
    public bool TryGet(
        ArithmeticOperator @operator,
        XdmTypeId left,
        XdmTypeId right,
        out BinaryOperatorImplementation? implementation
    )
    {
        var found = TryFind(@operator, left, right, out var entry);
        implementation = found ? entry.Plain : null;
        return implementation is not null;
    }

    /// <summary>Finds the implementation of a binary operator for a pair of operand types, whether it reads the dynamic context or not.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="left">The type of the left operand.</param>
    /// <param name="right">The type of the right operand.</param>
    /// <param name="implementation">The implementation, <see langword="null"/> when there is none; an entry registered without the context is wrapped once, at registration.</param>
    /// <returns><see langword="true"/> when an entry matches the types exactly or by subtype substitution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <remarks>Allocates nothing; the lookup is the one of <c>TryGet</c>.</remarks>
    public bool TryGetContextual(
        ArithmeticOperator @operator,
        XdmTypeId left,
        XdmTypeId right,
        out ContextBinaryOperatorImplementation? implementation
    )
    {
        var found = TryFind(@operator, left, right, out var entry);
        implementation = found ? entry.Contextual : null;
        return found;
    }

    /// <summary>Finds the implementation of a unary operator for an operand type.</summary>
    /// <param name="operator">The operator.</param>
    /// <param name="operand">The type of the operand.</param>
    /// <param name="implementation">The implementation, <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when an entry matches the type exactly or by subtype substitution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operator"/> is not a defined operator.</exception>
    /// <remarks>Allocates nothing.</remarks>
    public bool TryGet(UnaryOperator @operator, XdmTypeId operand, out UnaryOperatorImplementation? implementation)
    {
        CheckOperator(@operator);

        if (_unary.TryGetValue(((int)@operator, operand.Value), out implementation))
        {
            return true;
        }

        var entries = _unaryEntries[(int)@operator];
        var best = -1;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (
                XdmTypes.IsDerivedFrom(operand, entry.Operand)
                && (best < 0 || XdmTypes.IsDerivedFrom(entry.Operand, entries[best].Operand))
            )
            {
                best = i;
            }
        }

        implementation = best < 0 ? null : entries[best].Implementation;
        return best >= 0;
    }

    private void Add(ArithmeticOperator @operator, in BinaryEntry entry)
    {
        var key = ((int)@operator, entry.Left.Value, entry.Right.Value);
        if (_binary.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"An implementation of '{@operator}' for the operand types {entry.Left.Value} and {entry.Right.Value} is already registered."
            );
        }

        _binary.Add(key, entry);
        _binaryEntries[(int)@operator].Add(entry);
    }

    private bool TryFind(ArithmeticOperator @operator, XdmTypeId left, XdmTypeId right, out BinaryEntry entry)
    {
        CheckOperator(@operator);

        if (_binary.TryGetValue(((int)@operator, left.Value, right.Value), out entry))
        {
            return true;
        }

        // Substitution: the entry whose types both operands derive from; of two such entries, the one whose types derive from the other's.
        var entries = _binaryEntries[(int)@operator];
        var best = -1;
        for (var i = 0; i < entries.Count; i++)
        {
            var candidate = entries[i];
            if (!XdmTypes.IsDerivedFrom(left, candidate.Left) || !XdmTypes.IsDerivedFrom(right, candidate.Right))
            {
                continue;
            }

            if (
                best < 0
                || (
                    XdmTypes.IsDerivedFrom(candidate.Left, entries[best].Left)
                    && XdmTypes.IsDerivedFrom(candidate.Right, entries[best].Right)
                )
            )
            {
                best = i;
            }
        }

        entry = best < 0 ? default : entries[best];
        return best >= 0;
    }

    private static void CheckOperator(ArithmeticOperator @operator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)ArithmeticOperator.Add, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            (int)@operator,
            (int)ArithmeticOperator.Modulo,
            nameof(@operator)
        );
    }

    private static void CheckOperator(UnaryOperator @operator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)@operator, (int)UnaryOperator.Plus, nameof(@operator));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)@operator, (int)UnaryOperator.Minus, nameof(@operator));
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct BinaryEntry(
        XdmTypeId left,
        XdmTypeId right,
        BinaryOperatorImplementation? plain,
        ContextBinaryOperatorImplementation contextual
    )
    {
        public XdmTypeId Left { get; } = left;

        public XdmTypeId Right { get; } = right;

        public BinaryOperatorImplementation? Plain { get; } = plain;

        public ContextBinaryOperatorImplementation Contextual { get; } = contextual;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct UnaryEntry(XdmTypeId operand, UnaryOperatorImplementation implementation)
    {
        public XdmTypeId Operand { get; } = operand;

        public UnaryOperatorImplementation Implementation { get; } = implementation;
    }
}
