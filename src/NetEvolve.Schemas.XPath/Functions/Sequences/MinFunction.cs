namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary><c>fn:min</c>: the least value of the argument (<see cref="ExtremumFunction"/>).</summary>
internal sealed class MinFunction : ExtremumFunction
{
    /// <summary>Initializes a new instance of the <see cref="MinFunction"/> class.</summary>
    public MinFunction()
        : base("min", ComparisonRelation.LessThan) { }
}
