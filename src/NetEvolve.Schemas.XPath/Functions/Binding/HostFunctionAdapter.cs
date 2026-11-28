namespace NetEvolve.Schemas.XPath.Functions.Binding;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// Puts a function of the public contract on the one call path. The contract declares no parameter types, so every parameter is
/// <c>item()*</c> and the function receives its arguments as evaluated.
/// </summary>
/// <remarks>Limitation: no conversion applies to such a function until the contract can declare parameter types (#586).</remarks>
internal sealed class HostFunctionAdapter : XPathFunction
{
    private readonly XPathHostFunction _host;

    /// <summary>Initializes a new instance of the <see cref="HostFunctionAdapter"/> class.</summary>
    /// <param name="host">The function of the public contract.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="host"/> is <see langword="null"/>.</exception>
    public HostFunctionAdapter(XPathHostFunction host)
        : base(Check(host).Name, host.MinArity, host.MaxArity, []) => _host = host;

    /// <inheritdoc />
    public override XPathVersionMask Versions => _host.Versions;

    /// <inheritdoc />
    public override XPathHostRequirements HostRequirements => _host.HostRequirements;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => _host.Invoke(ref invocation);

    private static XPathHostFunction Check(XPathHostFunction host)
    {
        ArgumentNullException.ThrowIfNull(host);

        return host;
    }
}
