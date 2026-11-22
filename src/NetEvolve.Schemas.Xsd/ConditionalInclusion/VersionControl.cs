namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

/// <summary>The version control attributes of Structures 1.1, 4.2.2, and the evaluation of their values.</summary>
/// <remarks>
/// Limitation: an attribute of the version control namespace that is not listed here is neither evaluated nor reported;
/// the specification only encourages a warning.
/// </remarks>
internal static class VersionControl
{
    /// <summary>The namespace of the version control attributes.</summary>
    internal const string Namespace = "http://www.w3.org/2007/XMLSchema-versioning";

    /// <summary>The local name of <c>vc:minVersion</c>.</summary>
    internal const string MinVersion = "minVersion";

    /// <summary>The local name of <c>vc:maxVersion</c>.</summary>
    internal const string MaxVersion = "maxVersion";

    /// <summary>The local name of <c>vc:typeAvailable</c>.</summary>
    internal const string TypeAvailable = "typeAvailable";

    /// <summary>The local name of <c>vc:typeUnavailable</c>.</summary>
    internal const string TypeUnavailable = "typeUnavailable";

    /// <summary>The local name of <c>vc:facetAvailable</c>.</summary>
    internal const string FacetAvailable = "facetAvailable";

    /// <summary>The local name of <c>vc:facetUnavailable</c>.</summary>
    internal const string FacetUnavailable = "facetUnavailable";

    /// <summary>Evaluates one attribute of the version control namespace.</summary>
    /// <param name="localName">The local name of the attribute.</param>
    /// <param name="value">The value as written.</param>
    /// <param name="context">The namespace declarations in scope at the element.</param>
    /// <returns>The outcome; <see cref="VersionControlOutcome.Keep"/> for a name that is not a version control attribute.</returns>
    internal static VersionControlOutcome Evaluate(
        string localName,
        ReadOnlySpan<char> value,
        INamespaceContext context
    )
    {
        ArgumentNullException.ThrowIfNull(localName);
        ArgumentNullException.ThrowIfNull(context);

        return localName switch
        {
            MinVersion => Compare(value, isMinimum: true),
            MaxVersion => Compare(value, isMinimum: false),
            TypeAvailable => Names(value, context, facets: false, mustBeAvailable: true),
            TypeUnavailable => Names(value, context, facets: false, mustBeAvailable: false),
            FacetAvailable => Names(value, context, facets: true, mustBeAvailable: true),
            FacetUnavailable => Names(value, context, facets: true, mustBeAvailable: false),
            _ => VersionControlOutcome.Keep,
        };
    }

    // The element is excluded when V is less than minVersion or V is greater than or equal to maxVersion.
    private static VersionControlOutcome Compare(ReadOnlySpan<char> value, bool isMinimum)
    {
        var parsed = XsdDecimal.Parse(Trim(value));
        if (!parsed.Success)
        {
            return VersionControlOutcome.Invalid;
        }

        var order = ProcessorCapabilities.Version.CompareTo(parsed.Value);
        return (isMinimum ? order < 0 : order >= 0) ? VersionControlOutcome.Exclude : VersionControlOutcome.Keep;
    }

    // typeAvailable and facetAvailable exclude when any item is not known; typeUnavailable and facetUnavailable exclude
    // when every item is known, so an empty list keeps the first pair and excludes the second.
    private static VersionControlOutcome Names(
        ReadOnlySpan<char> value,
        INamespaceContext context,
        bool facets,
        bool mustBeAvailable
    )
    {
        var rest = Trim(value);
        var anyUnknown = false;
        while (rest.Length > 0)
        {
            var end = 0;
            while (end < rest.Length && !IsSpace(rest[end]))
            {
                end++;
            }

            var name = XsdQName.Parse(rest.Slice(0, end), context);
            if (!name.Success)
            {
                return VersionControlOutcome.Invalid;
            }

            var namespaceName = name.Value.NamespaceName ?? string.Empty;
            var known = facets
                ? ProcessorCapabilities.IsFacetAvailable(namespaceName, name.Value.LocalName)
                : ProcessorCapabilities.IsTypeAvailable(namespaceName, name.Value.LocalName);
            anyUnknown |= !known;
            rest = Trim(rest.Slice(end));
        }

        return (mustBeAvailable ? anyUnknown : !anyUnknown)
            ? VersionControlOutcome.Exclude
            : VersionControlOutcome.Keep;
    }

    private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> value)
    {
        var start = 0;
        var end = value.Length;
        while (start < end && IsSpace(value[start]))
        {
            start++;
        }

        while (end > start && IsSpace(value[end - 1]))
        {
            end--;
        }

        return value.Slice(start, end - start);
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';
}
