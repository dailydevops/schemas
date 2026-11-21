namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

/// <summary>A shared primitive that exists once in the Xml and XPath packages and must not be duplicated above them.</summary>
/// <param name="Name">The name of the seam.</param>
/// <param name="Implements">Tells whether a type implements the seam interface of the primitive.</param>
/// <param name="IsNamedLikeIt">Tells whether a simple type name follows the naming convention of the primitive.</param>
internal sealed record SeamKind(string Name, Func<Type, bool> Implements, Func<string, bool> IsNamedLikeIt);
