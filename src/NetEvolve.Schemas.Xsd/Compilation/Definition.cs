namespace NetEvolve.Schemas.Xsd.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>A top-level definition of a schema document that is built when something first needs it.</summary>
internal sealed class Definition
{
    internal Definition(SchemaSyntaxNode node, UnitScope unit, XPathQName name)
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(unit);

        Node = node;
        Unit = unit;
        Name = name;
    }

    /// <summary>Gets the syntax.</summary>
    public SchemaSyntaxNode Node { get; }

    /// <summary>Gets the unit that holds the syntax.</summary>
    public UnitScope Unit { get; }

    /// <summary>Gets the expanded name, <see langword="default"/> when the syntax has no name.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets or sets the component once it is built.</summary>
    public IXsdComponent? Component { get; set; }

    /// <summary>Gets or sets a value indicating whether the definition is on the path of the construction, so a reference to it is a cycle.</summary>
    public bool IsBuilding { get; set; }

    /// <summary>Gets or sets a value indicating whether the build ended with an issue and left no component.</summary>
    public bool IsFailed { get; set; }

    /// <summary>Gets or sets a value indicating whether the cycle through this definition was reported.</summary>
    public bool IsCycleReported { get; set; }
}
