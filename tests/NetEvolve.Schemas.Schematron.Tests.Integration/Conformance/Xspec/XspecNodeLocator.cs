namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>Selects the node an XPath location names in the instance document, to compare two locations by node identity.</summary>
internal sealed class XspecNodeLocator
{
    /// <summary>Loads the instance document.</summary>
    /// <param name="instance">The encoded instance document.</param>
    /// <returns>The locator.</returns>
    public static XspecNodeLocator Load(ReadOnlyMemory<byte> instance) =>
        instance.IsEmpty ? throw new NotImplementedException() : new XspecNodeLocator();

    /// <summary>Determines whether two location expressions select the same single node.</summary>
    /// <param name="left">The first expression.</param>
    /// <param name="leftNamespaces">The namespace bindings of the first expression.</param>
    /// <param name="right">The second expression.</param>
    /// <param name="rightNamespaces">The namespace bindings of the second expression.</param>
    /// <param name="problem">Why an expression does not select exactly one node; <see langword="null"/> otherwise.</param>
    /// <returns><see langword="true"/> when both select the same node.</returns>
    public bool Same(
        string left,
        IReadOnlyDictionary<string, string> leftNamespaces,
        string right,
        IReadOnlyDictionary<string, string> rightNamespaces,
        out string? problem
    ) => throw new NotImplementedException();
}
