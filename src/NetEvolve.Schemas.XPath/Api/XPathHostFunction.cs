namespace NetEvolve.Schemas.XPath.Api;

/// <summary>A function supplied by the host. Implementations must be stateless or thread-safe.</summary>
public abstract class XPathHostFunction
{
    /// <summary>Initializes a new instance of the <see cref="XPathHostFunction"/> class.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments; <see cref="int.MaxValue"/> for a variadic function.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="minArity"/> is negative or <paramref name="maxArity"/> is below it.</exception>
    protected XPathHostFunction(XPathQName name, int minArity, int maxArity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minArity, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxArity, minArity);

        Name = name;
        MinArity = minArity;
        MaxArity = maxArity;
    }

    /// <summary>Gets the expanded name.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the minimum number of arguments.</summary>
    public int MinArity { get; }

    /// <summary>Gets the maximum number of arguments.</summary>
    public int MaxArity { get; }

    /// <summary>Gets the language versions in which the function is available; every version by default.</summary>
    public virtual XPathVersionMask Versions => XPathVersionMask.All;

    /// <summary>Gets the host slots the function needs; none by default.</summary>
    public virtual XPathHostRequirements HostRequirements => XPathHostRequirements.None;

    /// <summary>Invokes the function.</summary>
    /// <param name="invocation">The call: the arguments in, the result and a diagnostic out.</param>
    /// <returns>The status.</returns>
    public abstract XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation);
}
