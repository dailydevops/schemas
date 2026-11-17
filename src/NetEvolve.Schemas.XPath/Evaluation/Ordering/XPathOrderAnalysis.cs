namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Static ordering analysis: composes the properties of an axis step sequence, so that a step known to deliver document order without
/// duplicates skips the sort (XPath 2.0, 3.2: a path result is in document order without duplicates).
/// </summary>
/// <remarks>
/// Conservative by design: it never claims a property that can fail, and may ask for a sort that was not needed. Limitation: only the
/// axis of a step is analyzed, not its node test or predicates, which only filter and so keep every property.
/// </remarks>
internal static class XPathOrderAnalysis
{
    private const XPathOrderProperties Forward = XPathOrderProperties.Ordered | XPathOrderProperties.DuplicateFree;

    /// <summary>The properties of the sequence of one context node.</summary>
    public const XPathOrderProperties SingleNode =
        XPathOrderProperties.Single | Forward | XPathOrderProperties.NonNested;

    /// <summary>The properties of a sequence nothing is known about.</summary>
    public const XPathOrderProperties Unknown = XPathOrderProperties.None;

    /// <summary>Composes the properties after a step sequence.</summary>
    /// <param name="input">The properties of the sequence the first step starts from.</param>
    /// <param name="axes">The axes of the steps, first step first.</param>
    /// <returns>The properties of the result.</returns>
    /// <exception cref="XPathException">An axis is the namespace axis (XPST0010).</exception>
    public static XPathOrderProperties Compose(XPathOrderProperties input, ReadOnlySpan<Axis> axes)
    {
        var properties = input;
        foreach (var axis in axes)
        {
            properties = Step(properties, axis);
        }

        return properties;
    }

    /// <summary>Gets the properties after applying one axis step to every node of a sequence and concatenating the results.</summary>
    /// <param name="input">The properties of the sequence.</param>
    /// <param name="axis">The axis.</param>
    /// <returns>The properties of the result.</returns>
    /// <exception cref="XPathException">The axis is the namespace axis (XPST0010).</exception>
    public static XPathOrderProperties Step(XPathOrderProperties input, Axis axis)
    {
        var delivered = XPathAxes.GetProperties(axis);

        if (input.HasFlag(XPathOrderProperties.Single))
        {
            return StepFromSingle(axis, delivered);
        }

        if (axis == Axis.Self)
        {
            return input;
        }

        var duplicateFree = input.HasFlag(XPathOrderProperties.DuplicateFree);
        var forward = (input & Forward) == Forward;
        var nonNested = input.HasFlag(XPathOrderProperties.NonNested);

        switch (axis)
        {
            case Axis.Child:
                // One parent per node keeps duplicates out; the children of an ancestor enclose those of its descendant, so order needs non-nesting.
                return (duplicateFree ? XPathOrderProperties.DuplicateFree : XPathOrderProperties.None)
                    | (forward && nonNested ? XPathOrderProperties.Ordered : XPathOrderProperties.None)
                    | (nonNested ? XPathOrderProperties.NonNested : XPathOrderProperties.None);
            case Axis.Attribute:
                // The attributes of an element precede all of its descendants, so order holds even for nested input.
                return (duplicateFree ? XPathOrderProperties.DuplicateFree : XPathOrderProperties.None)
                    | (forward ? XPathOrderProperties.Ordered : XPathOrderProperties.None)
                    | XPathOrderProperties.NonNested;
            case Axis.Descendant:
            case Axis.DescendantOrSelf:
                // Disjoint subtrees neither overlap nor interleave.
                return duplicateFree && nonNested
                    ? XPathOrderProperties.DuplicateFree
                        | (forward ? XPathOrderProperties.Ordered : XPathOrderProperties.None)
                    : XPathOrderProperties.None;
            default:
                return XPathOrderProperties.None;
        }
    }

    /// <summary>Decides what a result with the given properties needs to be in document order without duplicates.</summary>
    /// <param name="properties">The properties of the result.</param>
    /// <returns>The requirement.</returns>
    public static XPathOrderRequirement GetRequirement(XPathOrderProperties properties)
    {
        if ((properties & Forward) == Forward)
        {
            return XPathOrderRequirement.None;
        }

        if (properties.HasFlag(XPathOrderProperties.Reverse) && properties.HasFlag(XPathOrderProperties.DuplicateFree))
        {
            return XPathOrderRequirement.Reverse;
        }

        return properties.HasFlag(XPathOrderProperties.Ordered)
            ? XPathOrderRequirement.Deduplicate
            : XPathOrderRequirement.Sort;
    }

    private static XPathOrderProperties StepFromSingle(Axis axis, XPathAxisProperties delivered)
    {
        var result =
            XPathOrderProperties.DuplicateFree
            | (
                delivered.HasFlag(XPathAxisProperties.Reverse)
                    ? XPathOrderProperties.Reverse
                    : XPathOrderProperties.Ordered
            );

        if (axis is Axis.Self or Axis.Parent)
        {
            result |= XPathOrderProperties.Single;
        }

        if (
            axis
            is Axis.Self
                or Axis.Parent
                or Axis.Child
                or Axis.Attribute
                or Axis.FollowingSibling
                or Axis.PrecedingSibling
        )
        {
            result |= XPathOrderProperties.NonNested;
        }

        return result;
    }
}
