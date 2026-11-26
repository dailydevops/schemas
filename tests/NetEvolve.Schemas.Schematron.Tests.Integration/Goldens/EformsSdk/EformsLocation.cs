namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Brings the locations of two SVRL reports to one form, so that the same node compares equal.
/// </summary>
/// <remarks>
/// <para>
/// The engine writes the path of a node as <c>/*[local-name()='N' and namespace-uri()='U'][1]</c>; the reference of the SDK writes
/// <c>/prefix:N</c> with the prefix of the rule set, the root element without prefix, and the position only where siblings of the
/// same name exist. The normal form is the form of the reference with every position <c>[1]</c> removed, which names the same node on
/// both sides.
/// </para>
/// <para>
/// The reference leaves the location empty when the context is an attribute, so the location of an engine path that ends in an
/// attribute is dropped too.
/// </para>
/// </remarks>
internal static partial class EformsLocation
{
    /// <summary>Normalizes a location.</summary>
    /// <param name="location">The location of a finding.</param>
    /// <param name="prefixes">The namespace prefixes by namespace URI, as the report declares them.</param>
    /// <returns>The normal form.</returns>
    public static string Normalize(string location, IReadOnlyDictionary<string, string> prefixes)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(prefixes);
        if (AttributeStep().IsMatch(location))
        {
            return string.Empty;
        }

        var first = true;
        var converted = ElementStep()
            .Replace(location, match =>
            {
                var name = match.Groups[1].Value;
                var prefix = !first && prefixes.TryGetValue(match.Groups[2].Value, out var p) ? p + ":" : string.Empty;
                first = false;
                return new StringBuilder("/").Append(prefix).Append(name).Append(match.Groups[3].Value).ToString();
            });
        return FirstPosition().Replace(converted, string.Empty);
    }

    [GeneratedRegex(@"/@\*\[local-name\(\)='[^']*' and namespace-uri\(\)='[^']*'\]$", RegexOptions.CultureInvariant)]
    private static partial Regex AttributeStep();

    [GeneratedRegex(
        @"/\*\[local-name\(\)='([^']*)' and namespace-uri\(\)='([^']*)'\](\[\d+\])?",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex ElementStep();

    [GeneratedRegex(@"\[1\](?=/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex FirstPosition();
}
