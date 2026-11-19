namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>One atomized sort key value, classified for the comparison (XSLT 2.0 section 13.1.2).</summary>
internal readonly struct SortValue
{
    private SortValue(SortCategory category, XdmItem item, string? text)
    {
        Category = category;
        Item = item;
        Text = text;
    }

    /// <summary>Gets the kind of comparison the value takes part in.</summary>
    internal SortCategory Category { get; }

    /// <summary>Gets the number or the boolean.</summary>
    internal XdmItem Item { get; }

    /// <summary>Gets the string of a text value.</summary>
    internal string? Text { get; }

    /// <summary>Gets the value of a key that is the empty sequence: equal to every other empty key and less than any other value.</summary>
    internal static SortValue Empty => default;

    /// <summary>Classifies an atomized key item.</summary>
    /// <param name="item">The item: a node, whose string value is the key, or an atomic value.</param>
    /// <param name="asText">Whether <c>data-type="text"</c> converts every value to a string.</param>
    /// <param name="value">The value when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the item has no string form under <paramref name="asText"/> (limitation of <see cref="AtomicText"/>).</returns>
    internal static bool TryCreate(in XdmItem item, bool asText, out SortValue value)
    {
        if (item.TryGetNode(out var node))
        {
            value = new SortValue(SortCategory.Text, default, node.StringValue());
            return true;
        }

        if (asText)
        {
            var converted = AtomicText.TryGet(in item, out var text);
            value = converted ? new SortValue(SortCategory.Text, default, text) : default;
            return converted;
        }

        value = item.Kind switch
        {
            XdmItemKind.XsString => new SortValue(SortCategory.Text, default, Read(in item, XdmItemKind.XsString)),
            XdmItemKind.UntypedAtomic => new SortValue(
                SortCategory.Text,
                default,
                Read(in item, XdmItemKind.UntypedAtomic)
            ),
            XdmItemKind.AnyUri => new SortValue(SortCategory.Text, default, Read(in item, XdmItemKind.AnyUri)),
            XdmItemKind.Boolean => new SortValue(SortCategory.Boolean, item, null),
            XdmItemKind.XsInteger or XdmItemKind.XsDecimal or XdmItemKind.XsDouble or XdmItemKind.XsFloat =>
                new SortValue(SortCategory.Number, item, null),
            _ => new SortValue(SortCategory.Other, item, null),
        };
        return true;
    }

    private static string Read(in XdmItem item, XdmItemKind kind)
    {
        string? text;
        if (kind == XdmItemKind.XsString)
        {
            _ = item.TryGetString(out text);
        }
        else if (kind == XdmItemKind.UntypedAtomic)
        {
            _ = item.TryGetUntypedAtomic(out text);
        }
        else
        {
            _ = item.TryGetAnyUri(out text);
        }

        return text ?? string.Empty;
    }
}
