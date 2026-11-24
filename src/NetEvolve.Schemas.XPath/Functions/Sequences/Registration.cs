namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Functions.Binding;

/// <summary>
/// The registration file of the sequence and aggregate group: its module initializer stages <see cref="Register"/> into the default
/// catalog (#123, AP8, option B), so no shared list names the group. The group holds the cardinality functions, the aggregates, the
/// functions that compare and select values (<c>fn:distinct-values</c>, <c>fn:index-of</c>, <c>fn:deep-equal</c>), and the functions
/// that cut and rearrange a sequence.
/// </summary>
/// <remarks>
/// Limitation: the design names the folder <c>Functions/Sequence</c>; the work package owns <c>Functions/Sequences</c>. The equality
/// of dates, times, and durations in <c>fn:distinct-values</c>, <c>fn:index-of</c>, and <c>fn:deep-equal</c> is left to #732, and the
/// collations other than the codepoint collation to XP-25 (#267).
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

        builder.Add(new EmptyFunction());
        builder.Add(new ExistsFunction());
        builder.Add(new CountFunction());
        builder.Add(new ZeroOrOneFunction());
        builder.Add(new OneOrMoreFunction());
        builder.Add(new ExactlyOneFunction());
        builder.Add(new SumFunction());
        builder.Add(new AverageFunction());
        builder.Add(new MinFunction());
        builder.Add(new MaxFunction());
        builder.Add(new DistinctValuesFunction());
        builder.Add(new IndexOfFunction());
        builder.Add(new DeepEqualFunction());
        builder.Add(new SubsequenceFunction());
        builder.Add(new InsertBeforeFunction());
        builder.Add(new RemoveFunction());
        builder.Add(new ReverseFunction());
        builder.Add(new UnorderedFunction());
        builder.Add(new ErrorFunction());
        builder.Add(new TraceFunction());
    }
}
