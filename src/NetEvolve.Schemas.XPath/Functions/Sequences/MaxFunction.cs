namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary><c>fn:max</c>: the greatest value of the argument (<see cref="ExtremumFunction"/>).</summary>
internal sealed class MaxFunction : ExtremumFunction
{
    /// <summary>Initializes a new instance of the <see cref="MaxFunction"/> class.</summary>
    public MaxFunction()
        : base("max", ComparisonRelation.GreaterThan) { }
}
