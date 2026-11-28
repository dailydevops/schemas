namespace NetEvolve.Schemas.XPath.Functions.Maps;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the functions of the <c>map</c> namespace: its module initializer stages <see cref="Register"/> into the
/// default catalog (#123, AP8, option B). The functions are available from XPath 3.1 on.
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

        builder.Add(new MapMergeFunction());
        builder.Add(new MapSizeFunction());
        builder.Add(new MapKeysFunction());
        builder.Add(new MapContainsFunction());
        builder.Add(new MapGetFunction());
        builder.Add(new MapPutFunction());
        builder.Add(new MapEntryFunction());
        builder.Add(new MapRemoveFunction());
        builder.Add(new MapForEachFunction());
        builder.Add(new MapFindFunction());
    }
}
