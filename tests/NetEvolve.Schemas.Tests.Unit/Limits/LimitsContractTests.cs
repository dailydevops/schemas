namespace NetEvolve.Schemas.Tests.Unit.Limits;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LimitsContractTests
{
    private sealed record LimitCase(
        string Name,
        Func<ValidationLimits, long> Get,
        Func<ValidationLimits, long, ValidationLimits> With,
        long Default,
        long Ceiling
    );

    private static readonly Dictionary<string, LimitCase> Cases = new(StringComparer.Ordinal)
    {
        ["MaxIssues"] = new("MaxIssues", l => l.MaxIssues, (l, v) => l.WithMaxIssues((int)v), 1_000, int.MaxValue),
        ["MaxInputSize"] = new(
            "MaxInputSize",
            l => l.MaxInputSize,
            (l, v) => l.WithMaxInputSize(v),
            64L * 1024 * 1024,
            ValidationLimits.MaxInputSizeCeiling
        ),
        ["MaxDepth"] = new(
            "MaxDepth",
            l => l.MaxDepth,
            (l, v) => l.WithMaxDepth((int)v),
            256,
            ValidationLimits.MaxDepthCeiling
        ),
        ["MaxAttributesPerElement"] = new(
            "MaxAttributesPerElement",
            l => l.MaxAttributesPerElement,
            (l, v) => l.WithMaxAttributesPerElement((int)v),
            1_024,
            int.MaxValue
        ),
        ["MaxEntityExpansion"] = new(
            "MaxEntityExpansion",
            l => l.MaxEntityExpansion,
            (l, v) => l.WithMaxEntityExpansion(v),
            10_000_000,
            long.MaxValue
        ),
        ["MaxEntityDepth"] = new(
            "MaxEntityDepth",
            l => l.MaxEntityDepth,
            (l, v) => l.WithMaxEntityDepth((int)v),
            16,
            ValidationLimits.MaxEntityDepthCeiling
        ),
        ["MaxExternalResources"] = new(
            "MaxExternalResources",
            l => l.MaxExternalResources,
            (l, v) => l.WithMaxExternalResources((int)v),
            16,
            int.MaxValue
        ),
        ["MaxResourceSize"] = new(
            "MaxResourceSize",
            l => l.MaxResourceSize,
            (l, v) => l.WithMaxResourceSize(v),
            16L * 1024 * 1024,
            ValidationLimits.MaxResourceSizeCeiling
        ),
    };

    [Test]
    [Arguments("MaxIssues")]
    [Arguments("MaxInputSize")]
    [Arguments("MaxDepth")]
    [Arguments("MaxAttributesPerElement")]
    [Arguments("MaxEntityExpansion")]
    [Arguments("MaxEntityDepth")]
    [Arguments("MaxExternalResources")]
    [Arguments("MaxResourceSize")]
    public async Task Default_ReturnsApprovedModerateValue(string name)
    {
        var limitCase = Cases[name];

        await Assert.That(limitCase.Get(ValidationLimits.Default)).IsEqualTo(limitCase.Default);
    }

    [Test]
    public async Task Default_ReturnsSharedInstance() =>
        await Assert.That(ValidationLimits.Default).IsSameReferenceAs(ValidationLimits.Default);

    [Test]
    [Arguments("MaxIssues", 0L)]
    [Arguments("MaxIssues", -1L)]
    [Arguments("MaxIssues", int.MinValue)]
    [Arguments("MaxInputSize", 0L)]
    [Arguments("MaxInputSize", -1L)]
    [Arguments("MaxInputSize", long.MinValue)]
    [Arguments("MaxDepth", 0L)]
    [Arguments("MaxDepth", -1L)]
    [Arguments("MaxDepth", int.MinValue)]
    [Arguments("MaxAttributesPerElement", 0L)]
    [Arguments("MaxAttributesPerElement", -1L)]
    [Arguments("MaxAttributesPerElement", int.MinValue)]
    [Arguments("MaxEntityExpansion", 0L)]
    [Arguments("MaxEntityExpansion", -1L)]
    [Arguments("MaxEntityExpansion", long.MinValue)]
    [Arguments("MaxEntityDepth", 0L)]
    [Arguments("MaxEntityDepth", -1L)]
    [Arguments("MaxEntityDepth", int.MinValue)]
    [Arguments("MaxExternalResources", 0L)]
    [Arguments("MaxExternalResources", -1L)]
    [Arguments("MaxExternalResources", int.MinValue)]
    [Arguments("MaxResourceSize", 0L)]
    [Arguments("MaxResourceSize", -1L)]
    [Arguments("MaxResourceSize", long.MinValue)]
    public async Task With_ValueBelowOne_ThrowsArgumentOutOfRangeException(string name, long value)
    {
        var limitCase = Cases[name];

        await Assert.That(() => limitCase.With(ValidationLimits.Default, value)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments("MaxDepth")]
    [Arguments("MaxEntityDepth")]
    [Arguments("MaxInputSize")]
    [Arguments("MaxResourceSize")]
    public async Task With_ValueAboveCeiling_ThrowsArgumentOutOfRangeException(string name)
    {
        var limitCase = Cases[name];

        await Assert
            .That(() => limitCase.With(ValidationLimits.Default, limitCase.Ceiling + 1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments("MaxIssues")]
    [Arguments("MaxInputSize")]
    [Arguments("MaxDepth")]
    [Arguments("MaxAttributesPerElement")]
    [Arguments("MaxEntityExpansion")]
    [Arguments("MaxEntityDepth")]
    [Arguments("MaxExternalResources")]
    [Arguments("MaxResourceSize")]
    public async Task With_BoundaryValues_AreAccepted(string name)
    {
        var limitCase = Cases[name];

        await Assert.That(limitCase.Get(limitCase.With(ValidationLimits.Default, 1))).IsEqualTo(1);
        await Assert
            .That(limitCase.Get(limitCase.With(ValidationLimits.Default, limitCase.Ceiling)))
            .IsEqualTo(limitCase.Ceiling);
    }

    [Test]
    public async Task With_ReturnsNewInstanceAndKeepsEveryOtherLimit()
    {
        var original = ValidationLimits.Default;

        foreach (var limitCase in Cases.Values)
        {
            var changed = limitCase.With(original, 7);

            await Assert.That(changed).IsNotSameReferenceAs(original);
            await Assert.That(limitCase.Get(original)).IsEqualTo(limitCase.Default);
            foreach (var other in Cases.Values)
            {
                var expected = ReferenceEquals(other, limitCase) ? 7 : other.Default;
                await Assert.That(other.Get(changed)).IsEqualTo(expected);
            }
        }
    }
}
