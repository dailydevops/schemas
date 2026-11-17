namespace NetEvolve.Schemas.Xsd.Contracts;

using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Builds the components of one kind from syntax; references stay unresolved until the resolver runs.</summary>
internal interface IComponentBuilder
{
    /// <summary>Gets the kind of component this builder creates.</summary>
    XsdComponentKind Kind { get; }

    /// <summary>Builds a component and registers it with <paramref name="registry"/> before returning it.</summary>
    /// <param name="element">The syntax of the declaration.</param>
    /// <param name="registry">The registry that receives the component.</param>
    /// <param name="reporter">The reporter for representation constraint violations.</param>
    /// <returns>The component with unresolved references.</returns>
    IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter);
}
