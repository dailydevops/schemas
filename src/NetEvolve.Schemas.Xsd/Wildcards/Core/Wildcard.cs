namespace NetEvolve.Schemas.Xsd.Wildcards.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// A wildcard component (Structures 3.10): a namespace constraint and how matched items are assessed. Immutable.
/// </summary>
/// <remarks>
/// Limitations: the operations accept only <see cref="Wildcard"/> operands, the one implementation of the seam; the
/// result of a union or an intersection takes the <see cref="ProcessContents"/> and the location of the first operand,
/// because Structures 1.0 defines the set operations on the namespace constraints only and leaves the rest to the
/// caller; XSD 1.1 <c>notNamespace</c> and <c>notQName</c> are the <c>NegatedWildcard</c> of <c>Wildcards/Negation</c> (#306).
/// </remarks>
internal sealed class Wildcard : IWildcard
{
    private Wildcard(NamespaceConstraint constraint, XsdProcessContents processContents, DocumentLocation location)
    {
        Constraint = constraint;
        ProcessContents = processContents;
        Location = location;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Wildcard;

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public XsdProcessContents ProcessContents { get; }

    /// <summary>Gets the namespace constraint.</summary>
    public NamespaceConstraint Constraint { get; }

    /// <summary>Creates a wildcard.</summary>
    /// <param name="constraint">The namespace constraint.</param>
    /// <param name="processContents">How matched items are assessed.</param>
    /// <param name="location">The location of the declaring syntax.</param>
    /// <returns>The wildcard.</returns>
    public static Wildcard Create(
        NamespaceConstraint constraint,
        XsdProcessContents processContents,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(constraint);

        return new(constraint, processContents, location);
    }

    /// <summary>Creates a wildcard from the <see langword="namespace"/> and <c>processContents</c> attributes of <c>any</c> or <c>anyAttribute</c>.</summary>
    /// <param name="namespaceValue">The <see langword="namespace"/> attribute, <see langword="null"/> for <c>##any</c>.</param>
    /// <param name="processContentsValue">The <c>processContents</c> attribute, <see langword="null"/> for <c>strict</c>.</param>
    /// <param name="targetNamespace">The target namespace of the schema document, empty for none.</param>
    /// <param name="location">The location of the declaring syntax.</param>
    /// <param name="wildcard">The wildcard when both values are valid.</param>
    /// <returns><see langword="true"/> when both values are valid.</returns>
    public static bool TryCreate(
        string? namespaceValue,
        string? processContentsValue,
        string targetNamespace,
        DocumentLocation location,
        out Wildcard? wildcard
    )
    {
        wildcard = null;
        var process = XsdProcessContents.Strict;
        if (processContentsValue is not null && !TryParseProcessContents(processContentsValue, out process))
        {
            return false;
        }

        if (!NamespaceConstraint.TryParse(namespaceValue, targetNamespace, out var constraint))
        {
            return false;
        }

        wildcard = new(constraint!, process, location);
        return true;
    }

    /// <inheritdoc />
    public bool Allows(XPathQName name) => Constraint.Allows(name.NamespaceName);

    /// <inheritdoc />
    public bool IsSubsetOf(IWildcard other) => Constraint.IsSubsetOf(Cast(other).Constraint);

    /// <inheritdoc />
    public IWildcard? Union(IWildcard other)
    {
        var union = Constraint.Union(Cast(other).Constraint);
        return union is null ? null : new Wildcard(union, ProcessContents, Location);
    }

    /// <inheritdoc />
    public IWildcard? Intersect(IWildcard other)
    {
        var intersection = Constraint.Intersect(Cast(other).Constraint);
        return intersection is null ? null : new Wildcard(intersection, ProcessContents, Location);
    }

    /// <summary>Computes the union and reports Attribute Wildcard Union (<c>cos-aw-union</c>) when it is not expressible.</summary>
    /// <param name="other">The other wildcard.</param>
    /// <param name="reporter">The issue reporter.</param>
    /// <returns>The union, or <see langword="null"/> when it is not expressible.</returns>
    public IWildcard? Union(IWildcard other, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var union = Union(other);
        if (union is null)
        {
            Report(reporter, XsdRuleCodes.CosAwUnion, "union", Cast(other));
        }

        return union;
    }

    /// <summary>Computes the intersection and reports Attribute Wildcard Intersection (<c>cos-aw-intersect</c>) when it is not expressible.</summary>
    /// <param name="other">The other wildcard.</param>
    /// <param name="reporter">The issue reporter.</param>
    /// <returns>The intersection, or <see langword="null"/> when it is not expressible.</returns>
    public IWildcard? Intersect(IWildcard other, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var intersection = Intersect(other);
        if (intersection is null)
        {
            Report(reporter, XsdRuleCodes.CosAwIntersect, "intersection", Cast(other));
        }

        return intersection;
    }

    private static Wildcard Cast(IWildcard other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return (Wildcard)other;
    }

    private static bool TryParseProcessContents(string value, out XsdProcessContents process)
    {
        process = XsdProcessContents.Strict;
        switch (value)
        {
            case "strict":
                return true;
            case "lax":
                process = XsdProcessContents.Lax;
                return true;
            case "skip":
                process = XsdProcessContents.Skip;
                return true;
            default:
                return false;
        }
    }

    private void Report(XsdIssueReporter reporter, DiagnosticCode code, string operation, Wildcard other)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                reporter.Report(
                    rule,
                    Location,
                    "The "
                        + operation
                        + " of the wildcard namespace constraints "
                        + Constraint
                        + " and "
                        + other.Constraint
                        + " is not expressible."
                );
                return;
            }
        }
    }
}
