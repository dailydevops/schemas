namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>One built-in type: its name, the first XSD version that has it, and how to create it. A type package exposes these from its own file.</summary>
public sealed class DatatypeRegistration
{
    /// <summary>Initializes a new instance of the <see cref="DatatypeRegistration"/> class.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="firstVersion">The first version that contains the type; <see cref="XsdVersion.V11"/> hides it from the 1.0 registry.</param>
    /// <param name="create">Creates the descriptor; resolve dependencies through <see cref="DatatypeRegistry.Get"/>, in any order.</param>
    public DatatypeRegistration(
        string name,
        XsdVersion firstVersion,
        Func<DatatypeRegistry, IDatatypeDescriptor> create
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(create);
        Name = name;
        FirstVersion = firstVersion;
        Create = create;
    }

    /// <summary>Gets the name of the type.</summary>
    public string Name { get; }

    /// <summary>Gets the first XSD version that contains the type.</summary>
    public XsdVersion FirstVersion { get; }

    /// <summary>Gets the factory of the descriptor.</summary>
    public Func<DatatypeRegistry, IDatatypeDescriptor> Create { get; }
}
