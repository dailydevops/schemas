namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;

/// <summary>One parameter of a stylesheet function: its name, its variable slot, and its <see langword="as"/> type.</summary>
internal sealed class XsltFunctionParameter
{
    internal XsltFunctionParameter(string name, int slot, XsltSequenceType? type, DocumentLocation location)
    {
        Argument.ThrowIfNull(name);
        Argument.ThrowIfLessThan(slot, 0);

        Name = name;
        Slot = slot;
        Type = type;
        Location = location;
    }

    /// <summary>Gets the name as written.</summary>
    internal string Name { get; }

    /// <summary>Gets the variable slot the argument is bound to.</summary>
    internal int Slot { get; }

    /// <summary>Gets the <see langword="as"/> type, or <see langword="null"/> when any value is accepted.</summary>
    internal XsltSequenceType? Type { get; }

    /// <summary>Gets the location of the <c>xsl:param</c> element.</summary>
    internal DocumentLocation Location { get; }
}
