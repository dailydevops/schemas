namespace NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Skips the tests of a conformance suite class on every leg that <see cref="ConformanceLeg"/> does not designate.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConformanceLegAttribute : SkipAttribute
{
    /// <summary>Initializes a new instance of the <see cref="ConformanceLegAttribute"/> class.</summary>
    public ConformanceLegAttribute()
        : base("The conformance suites run on the designated leg only.") { }

    /// <inheritdoc />
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !ConformanceLeg.IsDesignated(TargetFrameworkName.Of(context.TestDetails.ClassType.Assembly))
        );
    }
}
