namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

/// <summary>
/// The parts of a QT3 environment an XPath 2.0 engine without schema import uses. Limitation: <c>schema</c>, <c>collection</c>,
/// <c>resource</c>, <c>collation</c>, <c>decimal-format</c>, <c>function-library</c> and <c>context-item</c> are ignored, and
/// <c>fn:doc</c>, which would read the <c>uri</c> of a source, is not implemented yet (XP-60).
/// </summary>
/// <param name="Sources">The sources.</param>
/// <param name="Params">The parameters.</param>
/// <param name="Namespaces">The namespace bindings.</param>
/// <param name="StaticBaseUri">The static base URI; <see langword="null"/> when the environment gives none or <c>#UNDEFINED</c>.</param>
internal sealed record Qt3Environment(
    IReadOnlyList<Qt3Source> Sources,
    IReadOnlyList<Qt3Param> Params,
    IReadOnlyList<Qt3Namespace> Namespaces,
    string? StaticBaseUri
)
{
    /// <summary>Gets the environment of a case without one: no context item, no variable, no binding.</summary>
    public static Qt3Environment Empty { get; } = new([], [], [], null);
}
