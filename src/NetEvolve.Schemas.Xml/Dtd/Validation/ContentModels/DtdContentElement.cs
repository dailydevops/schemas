namespace NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>An element type declaration as the content model step matches against it: the kind and what the kind needs.</summary>
internal sealed class DtdContentElement
{
    /// <summary>Initializes a new instance of the <see cref="DtdContentElement"/> class.</summary>
    /// <param name="name">The element type name.</param>
    /// <param name="kind">The kind of content.</param>
    /// <param name="model">The compiled model of element content, otherwise <see langword="null"/>.</param>
    /// <param name="allowedChildren">The name symbols that mixed content allows, otherwise <see langword="null"/>.</param>
    internal DtdContentElement(string name, DtdContentKind kind, DtdContentModel? model, HashSet<int>? allowedChildren)
    {
        Argument.ThrowIfNull(name);

        Name = name;
        Kind = kind;
        Model = model;
        AllowedChildren = allowedChildren;
    }

    /// <summary>Gets the element type name.</summary>
    internal string Name { get; }

    /// <summary>Gets the kind of content.</summary>
    internal DtdContentKind Kind { get; }

    /// <summary>Gets the compiled model of element content, <see langword="null"/> for the other kinds.</summary>
    internal DtdContentModel? Model { get; }

    /// <summary>Gets the name symbols that mixed content allows, <see langword="null"/> for the other kinds.</summary>
    internal HashSet<int>? AllowedChildren { get; }
}
