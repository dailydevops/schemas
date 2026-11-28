namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the regular expression group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds <c>fn:matches</c>, <c>fn:replace</c>, and
/// <c>fn:tokenize</c> of Functions and Operators 1.0, section 7.6, over one cache of compiled patterns.
/// </summary>
/// <remarks>
/// Limitations: only the arities of XPath 2.0 are registered, so <c>fn:tokenize</c> has no one-argument form (XP-71, #334);
/// <c>xsl:analyze-string</c> is SCH-69. <c>fn:analyze-string</c> is implemented (XP-64, #332; XPath 3.0 gated).
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

        var cache = PatternCache.Shared;
        builder.Add(new MatchesFunction(cache));
        builder.Add(new ReplaceFunction(cache));
        builder.Add(new TokenizeFunction(cache));
    }
}
