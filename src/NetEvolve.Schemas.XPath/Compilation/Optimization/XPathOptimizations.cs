namespace NetEvolve.Schemas.XPath.Compilation.Optimization;

using System;

/// <summary>
/// The rewrites of the optimization stage (XPath 2.0, 2.3.4): each one is a unit of its own and can be switched off, so a test runs the
/// same expression with a rewrite on and off and compares the results.
/// </summary>
/// <remarks>Internal by design: no public member exposes the stage, so it can change without a breaking change.</remarks>
[Flags]
internal enum XPathOptimizations
{
    /// <summary>No rewrite; the lowered tree is used as it is.</summary>
    None = 0,

    /// <summary>Arithmetic, numeric comparisons, and logical operators over literals, and a conditional over a literal condition, are evaluated once at compile time.</summary>
    ConstantFolding = 1,

    /// <summary><c>[1]</c>, <c>[N]</c>, <c>[position() le N]</c>, and <c>[last()]</c> stop iteration, or skip the evaluation of the predicate.</summary>
    PositionalCutoff = 2,

    /// <summary><c>fn:exists</c> and <c>fn:empty</c> over a path read one node at most.</summary>
    ExistenceExit = 4,

    /// <summary>A path whose effective boolean value is taken (a condition, an operand of a logical operator, a predicate) reads one node at most.</summary>
    BooleanExit = 8,

    /// <summary>A comparison of <c>fn:count</c> over a path against an integer literal reads one node more than the literal at most.</summary>
    CountComparisonExit = 16,

    /// <summary>Every rewrite.</summary>
    All = ConstantFolding | PositionalCutoff | ExistenceExit | BooleanExit | CountComparisonExit,
}
