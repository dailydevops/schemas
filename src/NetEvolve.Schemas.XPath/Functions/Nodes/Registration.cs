namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the node and context group: its module initializer stages <see cref="Register"/> into the default catalog
/// (#123, AP8, option B), so no shared list names the group. The group holds the name functions, <c>root</c>, <c>lang</c>, the
/// context functions <c>position</c> and <c>last</c>, the accessors <c>fn:string</c> and <c>fn:data</c>, and <c>generate-id</c>.
/// </summary>
/// <remarks>
/// Limitation: the design names the folders <c>Functions/Node</c>, <c>Functions/Accessor</c>, and <c>Functions/Context</c>; the work
/// package owns <c>Functions/Nodes</c>, so the three groups share this registration file. <c>generate-id</c> is registered twice
/// with disjoint version masks: for XPath 3.0 and 3.1 without a host flag, and for XPath 2.0 under <see cref="XPathHostRequirements.Xslt20Host"/>.
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

        builder.Add(new NameFunction());
        builder.Add(new LocalNameFunction());
        builder.Add(new NamespaceUriFunction());
        builder.Add(new RootFunction());
        builder.Add(new LangFunction());
        builder.Add(new PositionFunction());
        builder.Add(new LastFunction());
        builder.Add(new StringFunction());
        builder.Add(new DataFunction());
        builder.Add(new GenerateIdFunction());
        builder.Add(new Xslt20GenerateIdFunction());
        builder.Add(new KeyFunction());
    }
}
