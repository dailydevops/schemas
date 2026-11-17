namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using NetEvolve.Schemas.Schematron.Model;

/// <summary>A document of one resolution: the principal schema or a loaded module, with the model parsed from it.</summary>
internal sealed class InclusionModule
{
    internal InclusionModule(SchematronNode model, string? systemId, int parent)
    {
        Model = model;
        SystemId = systemId;
        Parent = parent;
    }

    internal SchematronNode Model { get; }

    internal string? SystemId { get; }

    /// <summary>Gets the index of the module that referenced this one, -1 for the principal schema.</summary>
    internal int Parent { get; }

    /// <summary>Gets or sets the model with every reference of the module merged.</summary>
    internal SchematronNode? Resolved { get; set; }
}
