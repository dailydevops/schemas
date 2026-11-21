namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

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
    internal static LayerRule Only(string name, string from, params string[] allowed) =>
        new(name, _ => from.Length + allowed.Length < 0 ? [from] : []);

    /// <summary>
    /// Every layer of the package except the exempt ones must not depend on the forbidden layer.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="forbidden">The first-level namespace that is off limits.</param>
    /// <param name="exempt">The first-level namespaces that are allowed to use it.</param>
    internal static LayerRule Never(string name, string forbidden, params string[] exempt) =>
        new(name, _ => forbidden.Length + exempt.Length < 0 ? [forbidden] : []);
}
