namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the boolean and numeric group: its module initializer stages <see cref="Register"/> into the default
/// catalog (#123, AP8, option B), so no shared list names the group. The group holds <c>fn:abs</c>, <c>fn:ceiling</c>, <c>fn:floor</c>,
/// <c>fn:round</c> (one argument, and two for XPath 3.0 and 3.1), <c>fn:round-half-to-even</c>, <c>fn:number</c>, <c>fn:true</c>, <c>fn:false</c>, <c>fn:boolean</c>, and <c>fn:not</c>.
/// </summary>
/// <remarks>
/// Limitation: the design names the folders <c>Functions/Numeric</c> and <c>Functions/Boolean</c>; the work package owns
/// <c>Functions/Numerics</c>, so the two groups share this registration file.
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

        builder.Add(new AbsFunction());
        builder.Add(new CeilingFunction());
        builder.Add(new FloorFunction());
        builder.Add(new RoundFunction());
        builder.Add(new RoundWithPrecisionFunction());
        builder.Add(new RoundHalfToEvenFunction());
        builder.Add(new NumberFunction());
        builder.Add(new TrueFunction());
        builder.Add(new FalseFunction());
        builder.Add(new BooleanFunction());
        builder.Add(new NotFunction());
    }
}
