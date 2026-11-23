namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the string group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds the functions of Functions and Operators 1.0, section 7,
/// that XPath 2.0 needs and that no other work package owns.
/// </summary>
/// <remarks>
/// Limitations: the design names the folder <c>Functions/String</c>; the work package owns <c>Functions/Strings</c>. The functions that
/// use regular expressions live in <c>Functions/Regex</c>. Left to other packages are <c>fn:compare</c>, <c>fn:codepoint-equal</c>, and the collation registry to XP-25 (#267),
/// <c>fn:encode-for-uri</c> and <c>fn:iri-to-uri</c> to XP-28 (#270), <c>fn:escape-html-uri</c> to DEF-84 (#96), the one-argument
/// <c>fn:string-join</c> and the 3.0 and 3.1 additions to XP-71 (#334), and the fully-normalized form of <c>fn:normalize-unicode</c> to
/// DEF-75 (#87). The collation arguments accept the codepoint collation only (DEF-43 (#55), DEF-76 (#88)).
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

        builder.Add(new ConcatFunction());
        builder.Add(new StringJoinFunction());
        builder.Add(new SubstringFunction());
        builder.Add(new StringLengthFunction());
        builder.Add(new NormalizeSpaceFunction());
        builder.Add(new TranslateFunction());
        builder.Add(new ContainsFunction());
        builder.Add(new StartsWithFunction());
        builder.Add(new EndsWithFunction());
        builder.Add(new SubstringBeforeFunction());
        builder.Add(new SubstringAfterFunction());
        builder.Add(new UpperCaseFunction());
        builder.Add(new LowerCaseFunction());
        builder.Add(new NormalizeUnicodeFunction());
        builder.Add(new CodepointsToStringFunction());
        builder.Add(new StringToCodepointsFunction());
    }
}
