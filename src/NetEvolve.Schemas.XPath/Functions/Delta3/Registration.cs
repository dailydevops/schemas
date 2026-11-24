namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the XPath 3.0 and 3.1 function delta: its module initializer stages <see cref="Register"/> into the default
/// catalog (#123, AP8, option B). Every function here is visible from XPath 3.0 on and unknown in XPath 2.0 mode.
/// </summary>
/// <remarks>
/// Limitations: the delta leaves out the two-argument <c>fn:round</c>, which has its own work package (#727). The environment of the
/// process is never exposed (<see cref="EnvironmentFunction"/>).
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

        builder.Add(new HeadFunction());
        builder.Add(new TailFunction());
        builder.Add(new NodeSelectionFunction(outermost: false));
        builder.Add(new NodeSelectionFunction(outermost: true));
        builder.Add(new PathFunction());
        builder.Add(new HasChildrenFunction());
        builder.Add(new StringJoinItemsFunction());
        builder.Add(new TokenizeWhitespaceFunction());
        builder.Add(new ContainsTokenFunction());
        builder.Add(new ParseXmlFunction());
        builder.Add(new ParseXmlFragmentFunction());
        builder.Add(new EnvironmentFunction(all: false));
        builder.Add(new EnvironmentFunction(all: true));
    }
}
