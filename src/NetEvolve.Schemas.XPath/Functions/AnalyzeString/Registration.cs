namespace NetEvolve.Schemas.XPath.Functions.AnalyzeString;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Regex;

/// <summary>
/// The registration file of the <c>fn:analyze-string</c> group: its module initializer stages <see cref="Register"/> into the default
/// catalog (#123, AP8, option B). The function is available from XPath 3.0 on and shares the pattern cache of the regular expression
/// functions.
/// </summary>
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

        builder.Add(new AnalyzeStringFunction(PatternCache.Shared));
    }
}
