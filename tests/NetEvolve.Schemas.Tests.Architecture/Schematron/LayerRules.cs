namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using NetEvolve.Schemas.Tests.Architecture.Xml;

/// <summary>
/// Factories for the namespace layering rules of the Schematron and Xsd packages. The first-level namespaces of a package
/// are its layers; the rules are shared by both packages and live here because only the two package folders of the
/// architecture tests are owned by this work package.
/// </summary>
internal static class LayerRules
{
    /// <summary>
    /// A layer that may depend only on the listed layers: every other first-level namespace of the package is forbidden,
    /// including namespaces that later work packages add.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="from">The first-level namespace of the layer.</param>
    /// <param name="allowed">The first-level namespaces the layer may use.</param>
    internal static LayerRule Only(string name, string from, params string[] allowed) => new(name, input =>
        {
            var forbidden = FirstLevelNamespaces(input)
                .Where(ns => !string.Equals(ns, from, StringComparison.Ordinal) && !allowed.Contains(ns))
                .ToArray();

            return Find(input, from, forbidden);
        });

    /// <summary>
    /// Every layer of the package except the exempt ones must not depend on the forbidden layer.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="forbidden">The first-level namespace that is off limits.</param>
    /// <param name="exempt">The first-level namespaces that are allowed to use it.</param>
    internal static LayerRule Never(string name, string forbidden, params string[] exempt) =>
        new(name, input =>
            [
                .. FirstLevelNamespaces(input)
                    .Where(ns => !string.Equals(ns, forbidden, StringComparison.Ordinal) && !exempt.Contains(ns))
                    .SelectMany(ns => Find(input, ns, [forbidden])),
            ]
        );

    // The first-level namespaces below the root, in a stable order.
    private static string[] FirstLevelNamespaces(LayerInput input)
    {
        var prefix = input.Root + ".";

        return
        [
            .. input
                .Assembly.GetTypes()
                .Select(type => type.Namespace)
                .OfType<string>()
                .Where(ns => ns.StartsWith(prefix, StringComparison.Ordinal))
                .Select(ns => ns[prefix.Length..].Split('.')[0])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(segment => segment, StringComparer.Ordinal),
        ];
    }

    // An empty forbidden list would pass vacuously, and a layer without types fails loudly inside FindDependencies.
    private static IReadOnlyList<string> Find(LayerInput input, string from, string[] forbidden) =>
        XmlLayering.FindDependencies(
            input.Assembly,
            $"{input.Root}.{from}",
            forbidden.Select(ns => $"{input.Root}.{ns}")
        );
}
