namespace NetEvolve.Schemas.XPath.Formatting.Dates;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the date formatting group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds <c>format-date</c>, <c>format-dateTime</c>, and
/// <c>format-time</c>, each with two and with five arguments.
/// </summary>
/// <remarks>
/// Every function is registered twice with disjoint version masks, like <c>format-number</c>: for XPath 3.0 and 3.1 without a host
/// flag, and for XPath 2.0 under <see cref="XPathHostRequirements.Xslt20Host"/>. In plain XPath 2.0 the functions are not found (XPST0017).
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

        foreach (var kind in new[] { XsdDateTimeKind.DateTime, XsdDateTimeKind.Date, XsdDateTimeKind.Time })
        {
            foreach (var arity in new[] { 2, 5 })
            {
                builder.Add(new FormatDateTimeFunction(kind, arity, false));
                builder.Add(new FormatDateTimeFunction(kind, arity, true));
            }
        }
    }
}
