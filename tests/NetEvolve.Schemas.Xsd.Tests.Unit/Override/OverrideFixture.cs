namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Finds nodes of the loaded units, so a test can assert which node a unit holds.</summary>
internal static class OverrideFixture
{
    /// <summary>Gets a child of an <see langword="override"/> element of the document of a unit.</summary>
    /// <param name="unit">The unit.</param>
    /// <param name="overrideIndex">The zero-based index among the <see langword="override"/> elements of the document.</param>
    /// <param name="childIndex">The zero-based index of the child.</param>
    /// <returns>The child.</returns>
    public static SchemaSyntaxNode OverrideChild(SchemaUnit unit, int overrideIndex, int childIndex)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var seen = 0;
        for (var i = 0; i < unit.Root.ChildCount; i++)
        {
            var child = unit.Root.GetChildNode(i);
            if (string.Equals(child.Name.LocalName, "override", StringComparison.Ordinal) && seen++ == overrideIndex)
            {
                return child.GetChildNode(childIndex);
            }
        }

        throw new InvalidOperationException("The unit has no such override element.");
    }

    /// <summary>Finds the component of a unit by its <c>name</c> attribute.</summary>
    /// <param name="unit">The unit.</param>
    /// <param name="name">The name.</param>
    /// <returns>The component.</returns>
    public static SchemaSyntaxNode Component(SchemaUnit unit, string name)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(name);

        foreach (var component in unit.Components)
        {
            if (string.Equals(component.FindAttribute("name"), name, StringComparison.Ordinal))
            {
                return component;
            }
        }

        throw new InvalidOperationException("The unit has no component " + name + ".");
    }
}
