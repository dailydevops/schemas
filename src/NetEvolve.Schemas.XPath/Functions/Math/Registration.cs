namespace NetEvolve.Schemas.XPath.Functions.Math;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the math group: its module initializer stages <see cref="Register"/> into the default catalog (#123, AP8,
/// option B), so no shared list names the group. The group holds the 14 functions of the <c>math</c> namespace, available from XPath
/// 3.0 on.
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

        builder.Add(new MathFunction("pi", 0, false, static (_, _) => System.Math.PI));
        builder.Add(new MathFunction("exp", 1, true, static (x, _) => System.Math.Exp(x)));
        builder.Add(new MathFunction("exp10", 1, true, static (x, _) => System.Math.Pow(10, x)));
        builder.Add(new MathFunction("log", 1, true, static (x, _) => System.Math.Log(x)));
        builder.Add(new MathFunction("log10", 1, true, static (x, _) => System.Math.Log10(x)));
        builder.Add(new MathFunction("pow", 2, true, static (x, y) => System.Math.Pow(x, y)));
        builder.Add(new MathFunction("sqrt", 1, true, static (x, _) => System.Math.Sqrt(x)));
        builder.Add(new MathFunction("sin", 1, true, static (x, _) => System.Math.Sin(x)));
        builder.Add(new MathFunction("cos", 1, true, static (x, _) => System.Math.Cos(x)));
        builder.Add(new MathFunction("tan", 1, true, static (x, _) => System.Math.Tan(x)));
        builder.Add(new MathFunction("asin", 1, true, static (x, _) => System.Math.Asin(x)));
        builder.Add(new MathFunction("acos", 1, true, static (x, _) => System.Math.Acos(x)));
        builder.Add(new MathFunction("atan", 1, true, static (x, _) => System.Math.Atan(x)));
        builder.Add(new MathFunction("atan2", 2, false, static (y, x) => System.Math.Atan2(y, x)));
    }
}
