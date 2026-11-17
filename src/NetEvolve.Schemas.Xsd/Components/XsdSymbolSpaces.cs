namespace NetEvolve.Schemas.Xsd.Components;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Maps a component kind to its symbol space.</summary>
internal static class XsdSymbolSpaces
{
    /// <summary>The number of symbol spaces.</summary>
    internal const int Count = 7;

    /// <summary>Finds the symbol space of a kind.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="space">The space.</param>
    /// <returns><see langword="false"/> for kinds that are never named globally, such as particles and wildcards.</returns>
    internal static bool TryOf(XsdComponentKind kind, out XsdSymbolSpace space)
    {
        switch (kind)
        {
            case XsdComponentKind.SimpleType:
            case XsdComponentKind.ComplexType:
                space = XsdSymbolSpace.Types;
                return true;
            case XsdComponentKind.ElementDeclaration:
                space = XsdSymbolSpace.Elements;
                return true;
            case XsdComponentKind.AttributeDeclaration:
                space = XsdSymbolSpace.Attributes;
                return true;
            case XsdComponentKind.ModelGroupDefinition:
                space = XsdSymbolSpace.Groups;
                return true;
            case XsdComponentKind.AttributeGroup:
                space = XsdSymbolSpace.AttributeGroups;
                return true;
            case XsdComponentKind.IdentityConstraint:
                space = XsdSymbolSpace.IdentityConstraints;
                return true;
            case XsdComponentKind.Notation:
                space = XsdSymbolSpace.Notations;
                return true;
            default:
                space = default;
                return false;
        }
    }
}
