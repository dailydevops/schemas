namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static MapFixture;

/// <summary>Looking up a key of a map allocates nothing (XPath 3.1, 3.11.1; Functions and Operators 3.1, 17.1.6).</summary>
public class MapAllocationTests
{
    private const int Size = 10_000;

    private static XdmFunction BuildMap(string keyExpression)
    {
        var map = Evaluate($"map:merge(for $i in 1 to {Size} return map:entry({keyExpression}, $i))");
        _ = map[0].TryGetFunction(out var function);
        return function!;
    }

    [Test]
    [Arguments("$i")]
    [Arguments("string($i)")]
    [Arguments("xs:double($i) + 0.5")]
    [Arguments("xs:date('2000-01-01') + xs:dayTimeDuration('P1D') * $i")]
    public async Task Get_OnATenThousandEntryMap_AllocatesNothing(
        string keyExpression,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(keyExpression);

        var function = BuildMap(keyExpression);
        var probe = Evaluate(keyExpression.Replace("$i", "4242", StringComparison.Ordinal))[0];
        var missing = Evaluate(keyExpression.Replace("$i", "-1", StringComparison.Ordinal))[0];
        var found = XdmSequence.Create(probe);
        var absent = XdmSequence.Create(missing);
        XdmSequence[] hit = [found];
        XdmSequence[] miss = [absent];
        using var context = new XPathEvaluationContext();
        var result = default(XdmSequence);

        AllocationAssert.NoAllocation(
            () =>
            {
                result = function.Invoke(context, hit);
                _ = function.Invoke(context, miss);
            },
            warmups: 50
        );

        await Assert.That(function.Arity).IsEqualTo(1);
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetInteger(out var value) && value == 4242).IsTrue();
    }
}
