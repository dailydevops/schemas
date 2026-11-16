namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading;
using System.Threading.Tasks;

public class LimitsShapeTests
{
    [Test]
    public async Task Ceilings_AreTheApprovedConstants(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var maxDepthCeiling = ValidationLimits.MaxDepthCeiling;
        await Assert.That(maxDepthCeiling).IsEqualTo(10_000);
        var maxEntityDepthCeiling = ValidationLimits.MaxEntityDepthCeiling;
        await Assert.That(maxEntityDepthCeiling).IsEqualTo(1_000);
        var maxInputSizeCeiling = ValidationLimits.MaxInputSizeCeiling;
        await Assert.That(maxInputSizeCeiling).IsEqualTo(2_000_000_000L);
        var maxResourceSizeCeiling = ValidationLimits.MaxResourceSizeCeiling;
        await Assert.That(maxResourceSizeCeiling).IsEqualTo(2_000_000_000L);
    }

    [Test]
    public async Task ValidationLimits_IsSealedWithApprovedMembers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Assert.That(typeof(ValidationLimits).IsSealed).IsTrue();
        await Assert
            .That(Shape.Of(typeof(ValidationLimits)))
            .IsEquivalentTo(
                Shape.Sorted(
                    "const Int32 MaxDepthCeiling=10000",
                    "const Int32 MaxEntityDepthCeiling=1000",
                    "const Int64 MaxInputSizeCeiling=2000000000",
                    "const Int64 MaxResourceSizeCeiling=2000000000",
                    "static ValidationLimits Default { get }",
                    "Int32 MaxIssues { get }",
                    "Int64 MaxInputSize { get }",
                    "Int32 MaxDepth { get }",
                    "Int32 MaxAttributesPerElement { get }",
                    "Int64 MaxEntityExpansion { get }",
                    "Int32 MaxEntityDepth { get }",
                    "Int32 MaxExternalResources { get }",
                    "Int64 MaxResourceSize { get }",
                    "ValidationLimits WithMaxIssues(Int32)",
                    "ValidationLimits WithMaxInputSize(Int64)",
                    "ValidationLimits WithMaxDepth(Int32)",
                    "ValidationLimits WithMaxAttributesPerElement(Int32)",
                    "ValidationLimits WithMaxEntityExpansion(Int64)",
                    "ValidationLimits WithMaxEntityDepth(Int32)",
                    "ValidationLimits WithMaxExternalResources(Int32)",
                    "ValidationLimits WithMaxResourceSize(Int64)"
                )
            );
    }
}
