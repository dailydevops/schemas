namespace NetEvolve.Schemas.XPath.Collations;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the collation group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds <c>fn:compare</c> and <c>fn:codepoint-equal</c>.
/// </summary>
/// <remarks>
/// Limitations: the codepoint collation is the only collation that ships; every other collation URI is FOCH0002 (#123 AP19, DEF-43 (#55),
/// DEF-76 (#88)). The string and sequence functions that take a collation argument still check it for the codepoint URI only
/// (<c>CollationArgument</c>); they do not consult the registry yet (#752).
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

        builder.Add(new CompareFunction());
        builder.Add(new CodepointEqualFunction());
    }
}
