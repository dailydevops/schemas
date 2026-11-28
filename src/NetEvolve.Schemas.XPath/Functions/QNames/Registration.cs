namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the QName and URI group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds the QName functions of section 11 of Functions and
/// Operators, the URI functions <c>resolve-uri</c>, <c>encode-for-uri</c>, and <c>iri-to-uri</c>, and the base URI functions
/// <c>base-uri</c> and <c>static-base-uri</c>.
/// </summary>
/// <remarks>
/// Limitation: the work package owns <c>Functions/QNames</c> only, so the URI functions live in this folder; the design names a
/// <c>Functions/Uri</c> group for them.
/// </remarks>
internal static class Registration
{
    // Decision (#123, AP8, option B): a library registers its function groups through a module initializer, so CA2255 is suppressed in this file, once, and not in any other.
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Stage() => FunctionCatalog.Default.Stage(Register);
#pragma warning restore CA2255

    /// <summary>Adds the functions of the group.</summary>
    /// <param name="builder">The catalog builder.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    internal static void Register(FunctionCatalogBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(new ResolveQNameFunction());
        builder.Add(new ExpandedQNameFunction());
        builder.Add(new PrefixFromQNameFunction());
        builder.Add(new LocalNameFromQNameFunction());
        builder.Add(new NamespaceUriFromQNameFunction());
        builder.Add(new NamespaceUriForPrefixFunction());
        builder.Add(new InScopePrefixesFunction());
        builder.Add(new ResolveUriFunction());
        builder.Add(new BaseUriFunction());
        builder.Add(new StaticBaseUriFunction());
        builder.Add(new UriEscapingFunction("encode-for-uri", UriEscaping.EncodeForUri));
        builder.Add(new UriEscapingFunction("iri-to-uri", UriEscaping.IriToUri));
    }
}
