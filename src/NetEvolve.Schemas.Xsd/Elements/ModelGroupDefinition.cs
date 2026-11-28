namespace NetEvolve.Schemas.Xsd.Elements;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A named model group definition (Structures section 3.7). Group references inside it are structural, so the resolver reports
/// a circular definition (<c>mg-props-correct.2</c>); element references and type references are not.
/// </summary>
internal sealed class ModelGroupDefinition : IReferencingComponent
{
    private ModelGroupDefinition(XPathQName name, DocumentLocation location, ModelGroup modelGroup)
    {
        Name = name;
        Location = location;
        ModelGroup = modelGroup;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.ModelGroupDefinition;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the model group.</summary>
    public ModelGroup ModelGroup { get; }

    /// <inheritdoc />
    public int ReferenceCount => ModelGroup.ReferenceCount;

    /// <inheritdoc />
    public XsdReference GetReference(int index) => ModelGroup.GetReference(index);

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target) => ModelGroup.Bind(index, target);

    /// <summary>Creates a definition.</summary>
    /// <param name="name">The expanded name in the target namespace.</param>
    /// <param name="location">The location of the definition.</param>
    /// <param name="modelGroup">The model group.</param>
    /// <returns>The definition.</returns>
    internal static ModelGroupDefinition Create(XPathQName name, DocumentLocation location, ModelGroup modelGroup)
    {
        ArgumentNullException.ThrowIfNull(modelGroup);

        return new(name, location, modelGroup);
    }
}
