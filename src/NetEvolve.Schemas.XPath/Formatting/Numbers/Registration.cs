namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the number formatting group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds <c>format-number</c>.
/// </summary>
/// <remarks>
/// <c>format-number</c> is registered twice with disjoint version masks, like <c>generate-id</c>: for XPath 3.0 and 3.1 without a host
/// flag, and for XPath 2.0 under <see cref="XPathHostRequirements.Xslt20Host"/>. In plain XPath 2.0 the function is not found (XPST0017).
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

        builder.Add(FormatNumberFunction.ForXPath3());
        builder.Add(FormatNumberFunction.ForXslt20());
    }
}
