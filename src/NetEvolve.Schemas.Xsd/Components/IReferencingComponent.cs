namespace NetEvolve.Schemas.Xsd.Components;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A component whose by-name references the resolver binds exactly once.</summary>
internal interface IReferencingComponent : IXsdComponent
{
    /// <summary>Gets the number of references.</summary>
    int ReferenceCount { get; }

    /// <summary>Gets a reference.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The reference.</returns>
    XsdReference GetReference(int index);

    /// <summary>Receives the target of a reference.</summary>
    /// <param name="index">The index of the reference.</param>
    /// <param name="target">The resolved component.</param>
    void Bind(int index, IXsdComponent target);
}
