namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the document group: its module initializer stages <see cref="Register"/> into the default catalog (#123,
/// AP8, option B), so no shared list names the group. The group holds <c>fn:doc</c>, <c>fn:doc-available</c>, <c>fn:collection</c>, and
/// the text functions <c>fn:unparsed-text</c>, <c>fn:unparsed-text-available</c>, and <c>fn:unparsed-text-lines</c>, and <c>fn:id</c> and
/// <c>fn:idref</c>.
/// </summary>
/// <remarks>
/// <c>fn:unparsed-text</c> and <c>fn:unparsed-text-available</c> are registered twice with disjoint version masks: for XPath 3.0 and
/// 3.1 without a host flag, and for XPath 2.0 under <see cref="Api.XPathHostRequirements.Xslt20Host"/>. <c>fn:unparsed-text-lines</c> is
/// XPath 3.0 and later only.
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

        builder.Add(new DocFunction());
        builder.Add(new DocAvailableFunction());
        builder.Add(new CollectionFunction());
        builder.Add(new UnparsedTextFunction());
        builder.Add(new Xslt20UnparsedTextFunction());
        builder.Add(new UnparsedTextAvailableFunction());
        builder.Add(new Xslt20UnparsedTextAvailableFunction());
        builder.Add(new UnparsedTextLinesFunction());
        builder.Add(new IdFunction());
        builder.Add(new IdRefFunction());
    }
}
