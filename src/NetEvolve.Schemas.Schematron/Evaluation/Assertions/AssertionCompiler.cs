namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Compiles an <c>assert</c> or <c>report</c> with its message, diagnostics, and properties in the scope of its rule.</summary>
internal static class AssertionCompiler
{
    /// <summary>Compiles an assertion; every problem is reported and compiling continues, so one call finds them all.</summary>
    internal static bool TryCompile(
        SchematronAssertion assertion,
        RuleScope rule,
        SchematronSchema schema,
        IQueryBinding binding,
        in AssertionPosition position,
        IProblemSink problems,
        out CompiledAssertion compiled
    ) => throw NotImplementedFailure.For("assertion evaluation");
}
