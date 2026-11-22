namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the document group: its module initializer stages <see cref="Register"/> into the default catalog (#123,
/// AP8, option B), so no shared list names the group. The group holds <c>fn:doc</c>, <c>fn:doc-available</c>, and <c>fn:collection</c>.
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
        Argument.ThrowIfNull(builder);

        builder.Add(new DocFunction());
        builder.Add(new DocAvailableFunction());
        builder.Add(new CollectionFunction());
    }
}
