namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Wires the references between the registered components exactly once, then lets the set freeze.</summary>
internal interface IComponentResolver
{
    /// <summary>Resolves every reference of every component in the registry.</summary>
    /// <param name="registry">The registry holding the built components.</param>
    /// <param name="reporter">The reporter for unresolved references and cycles.</param>
    void Resolve(IComponentRegistry registry, XsdIssueReporter reporter);
}
