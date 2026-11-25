namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Predicates are only judged against the built-in functions, so another default function namespace never qualifies.</summary>
public class NonPositionalPredicatesTests
{
    [Test]
    public async Task All_DefaultFunctionNamespaceIsNotTheBuiltInOne_IsFalse() =>
        await Assert
            .That(NonPositionalPredicates.All(Array.Empty<Expression>(), "urn:other", "urn:built-in"))
            .IsFalse();

    [Test]
    public async Task All_DefaultFunctionNamespaceIsTheBuiltInOne_IsTrueWithoutPredicates() =>
        await Assert
            .That(NonPositionalPredicates.All(Array.Empty<Expression>(), "urn:built-in", "urn:built-in"))
            .IsTrue();
}
