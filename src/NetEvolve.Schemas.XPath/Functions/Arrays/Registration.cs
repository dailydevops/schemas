namespace NetEvolve.Schemas.XPath.Functions.Arrays;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The registration file of the array group: its module initializer stages <see cref="Register"/> into the default catalog (#123, AP8,
/// option B). The group holds the 18 functions of the <c>array</c> namespace, available from XPath 3.1 on.
/// </summary>
internal static class Registration
{
    private static readonly XPathParameterType One = new(XPathItemType.Item, Occurrence.ExactlyOne);
    private static readonly XPathParameterType Items = new(XPathItemType.Item, Occurrence.ZeroOrMore);
    private static readonly XPathParameterType Position = new(XPathItemType.Integer, Occurrence.ExactlyOne);
    private static readonly XPathParameterType Positions = new(XPathItemType.Integer, Occurrence.ZeroOrMore);
    private static readonly XPathParameterType Collation = new(XPathItemType.String, Occurrence.ZeroOrOne);

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

        builder.Add(new ArrayFunction("size", 1, [One], ArrayBodies.Size));
        builder.Add(new ArrayFunction("get", 2, [One, Position], ArrayBodies.Get));
        builder.Add(new ArrayFunction("put", 3, [One, Position, Items], ArrayBodies.Put));
        builder.Add(new ArrayFunction("append", 2, [One, Items], ArrayBodies.Append));
        builder.Add(new ArrayFunction("subarray", 2, [One, Position, Position], ArrayBodies.Subarray));
        builder.Add(new ArrayFunction("remove", 2, [One, Positions], ArrayBodies.Remove));
        builder.Add(new ArrayFunction("insert-before", 3, [One, Position, Items], ArrayBodies.InsertBefore));
        builder.Add(new ArrayFunction("head", 1, [One], ArrayBodies.Head));
        builder.Add(new ArrayFunction("tail", 1, [One], ArrayBodies.Tail));
        builder.Add(new ArrayFunction("reverse", 1, [One], ArrayBodies.Reverse));
        builder.Add(new ArrayFunction("join", 1, [Items], ArrayBodies.Join));
        builder.Add(new ArrayFunction("for-each", 2, [One, One], ArrayBodies.ForEach));
        builder.Add(new ArrayFunction("filter", 2, [One, One], ArrayBodies.Filter));
        builder.Add(new ArrayFunction("fold-left", 3, [One, Items, One], ArrayBodies.FoldLeft));
        builder.Add(new ArrayFunction("fold-right", 3, [One, Items, One], ArrayBodies.FoldRight));
        builder.Add(new ArrayFunction("for-each-pair", 3, [One, One, One], ArrayBodies.ForEachPair));
        builder.Add(new ArrayFunction("sort", 1, [One, Collation, One], ArrayBodies.Sort));
        builder.Add(new ArrayFunction("flatten", 1, [Items], ArrayBodies.Flatten));
    }
}
