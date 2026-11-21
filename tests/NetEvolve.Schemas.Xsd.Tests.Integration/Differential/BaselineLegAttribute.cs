namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Skips a test on every leg except the one that runs the System.Xml.Schema baseline.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
internal sealed class BaselineLegAttribute : SkipAttribute
{
    public BaselineLegAttribute()
        : base("The System.Xml.Schema baseline runs on one leg only.") { }

    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !DifferentialLeg.RunsBaseline(TargetFrameworkName.Of(context.TestDetails.ClassType.Assembly))
        );
    }
}
