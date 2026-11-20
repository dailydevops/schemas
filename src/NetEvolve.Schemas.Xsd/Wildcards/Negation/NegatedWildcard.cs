namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// A wildcard component of XSD 1.1 (Structures 3.10): a <see cref="NameConstraint"/> with <c>notNamespace</c>,
/// <c>notQName</c>, <c>##defined</c>, and <c>##definedSibling</c>, and how matched items are assessed. Immutable.
/// </summary>
/// <remarks>
/// <para>
/// Decision on #306: <c>##defined</c> and <c>##definedSibling</c> need the declarations of the schema and the element
/// particles of the content model, which <see cref="Allows"/> does not receive. A wildcard is therefore created without
/// them and <see cref="WithContext"/> returns a copy bound to them; an unbound wildcard disallows no name for a keyword.
/// The seam <see cref="IWildcard"/> stays as it is, and the matcher keeps asking <see cref="Allows"/> only.
/// </para>
/// <para>
/// Limitations: the operations accept this type and the wildcard of XSD 1.0 (<see cref="Wildcard"/>) as the other operand,
/// whose <see langword="not"/> excludes the absent namespace too; the wildcard of XSD 1.0 accepts no operand of this type, so in
/// XSD 1.1 every wildcard of a schema must be of this type. Like the wildcard of XSD 1.0, the result of a union or an
/// intersection takes the <see cref="ProcessContents"/> and the location of the first operand; it takes the declared names
/// of the first operand that has them. The union and the intersection are always expressible (Structures 3.10.6.3, 3.10.6.4).
/// </para>
/// </remarks>
internal sealed class NegatedWildcard : IWildcard
{
    private NegatedWildcard(
        NameConstraint constraint,
        XsdProcessContents processContents,
        DocumentLocation location,
        IDeclaredNames? defined,
        IDeclaredNames? siblings
    )
    {
        Constraint = constraint;
        ProcessContents = processContents;
        Location = location;
        DefinedNames = defined;
        SiblingNames = siblings;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.Wildcard;

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public XsdProcessContents ProcessContents { get; }

    /// <summary>Gets the namespace constraint and the disallowed names.</summary>
    public NameConstraint Constraint { get; }

    /// <summary>Gets the declared names that <c>##defined</c> refers to, <see langword="null"/> when unbound.</summary>
    public IDeclaredNames? DefinedNames { get; }

    /// <summary>Gets the sibling names that <c>##definedSibling</c> refers to, <see langword="null"/> when unbound.</summary>
    public IDeclaredNames? SiblingNames { get; }

    /// <summary>Creates a wildcard that is not bound to declared names.</summary>
    /// <param name="constraint">The namespace constraint and disallowed names.</param>
    /// <param name="processContents">How matched items are assessed.</param>
    /// <param name="location">The location of the declaring syntax.</param>
    /// <returns>The wildcard.</returns>
    public static NegatedWildcard Create(
        NameConstraint constraint,
        XsdProcessContents processContents,
        DocumentLocation location
    )
    {
        Argument.ThrowIfNull(constraint);
        Argument.ThrowIfLessThan((int)processContents, (int)XsdProcessContents.Strict);
        Argument.ThrowIfGreaterThan((int)processContents, (int)XsdProcessContents.Skip);

        return new(constraint, processContents, location, null, null);
    }

    /// <summary>
    /// Creates a wildcard from an <c>any</c> or <c>anyAttribute</c> element: its <see langword="namespace"/> or <c>notNamespace</c>,
    /// <c>notQName</c>, and <c>processContents</c>. A value that is not valid, both <see langword="namespace"/> and
    /// <c>notNamespace</c>, and <c>##definedSibling</c> on <c>anyAttribute</c> are reported as <c>src-wildcard</c>.
    /// </summary>
    /// <param name="node">The element.</param>
    /// <param name="scope">The namespace scope of the document, which resolves the QNames.</param>
    /// <param name="attributeWildcard"><see langword="true"/> for <c>anyAttribute</c>.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="wildcard">The wildcard when the values are valid.</param>
    /// <returns><see langword="false"/> after an issue was reported.</returns>
    public static bool TryCreate(
        SchemaSyntaxNode node,
        SchemaNamespaceScope scope,
        bool attributeWildcard,
        XsdIssueReporter reporter,
        out NegatedWildcard? wildcard
    )
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(reporter);

        wildcard = null;
        var namespaces = node.FindAttribute("namespace");
        var notNamespaces = node.FindAttribute("notNamespace");
        if (namespaces is not null && notNamespaces is not null)
        {
            return Reject(reporter, node, "A wildcard must not have both a namespace and a notNamespace attribute.");
        }

        // The processContents attribute is read by the wildcard of XSD 1.0, whose namespace is not used here.
        if (
            !Wildcard.TryCreate(
                "##any",
                node.FindAttribute("processContents"),
                scope.TargetNamespace,
                node.Location,
                out var process
            )
        )
        {
            return Reject(reporter, node, "The processContents of the wildcard is invalid.");
        }

        // A notNamespace is a list of namespaces only: ##any and ##other are not allowed in it.
        var isNot = notNamespaces is not null;
        if (
            !NamespaceConstraint.TryParse(notNamespaces ?? namespaces, scope.TargetNamespace, out var parsed)
            || (isNot && parsed!.Variety != NamespaceConstraintVariety.Enumeration)
        )
        {
            return Reject(reporter, node, "The namespace of the wildcard is invalid.");
        }

        var names = new List<XPathQName>();
        var defined = false;
        var sibling = false;
        if (
            node.FindAttribute("notQName") is { } qnames
            && !TryReadNames(node, scope, qnames, attributeWildcard, names, ref defined, ref sibling)
        )
        {
            return Reject(reporter, node, "The notQName of the wildcard is invalid.");
        }

        var spaces = NameConstraint.FromCore(parsed!);
        var negation = isNot || spaces.IsNegation;
        if (!NamesAreAllowed(reporter, node, NameConstraint.Create(negation, spaces.Namespaces, []), names))
        {
            return false;
        }

        var constraint = NameConstraint.Create(negation, spaces.Namespaces, names.ToArray(), defined, sibling);
        wildcard = new(constraint, process!.ProcessContents, node.Location, null, null);
        return true;
    }

    /// <summary>Gets the constraint of a wildcard of either implementation.</summary>
    /// <param name="wildcard">The wildcard: this type or the wildcard of XSD 1.0.</param>
    /// <returns>The constraint, which for the wildcard of XSD 1.0 is its converted namespace constraint.</returns>
    /// <exception cref="InvalidCastException"><paramref name="wildcard"/> is of another implementation of the seam.</exception>
    public static NameConstraint ConstraintOf(IWildcard wildcard)
    {
        Argument.ThrowIfNull(wildcard);

        return wildcard is NegatedWildcard negated
            ? negated.Constraint
            : NameConstraint.FromCore(((Wildcard)wildcard).Constraint);
    }

    /// <summary>Returns a copy bound to the names that <c>##defined</c> and <c>##definedSibling</c> refer to.</summary>
    /// <param name="defined">The declared names, <see langword="null"/> for none.</param>
    /// <param name="siblings">The names of the elements in the content model, <see langword="null"/> for none.</param>
    /// <returns>The copy.</returns>
    public NegatedWildcard WithContext(IDeclaredNames? defined, IDeclaredNames? siblings) =>
        new(Constraint, ProcessContents, Location, defined, siblings);

    /// <inheritdoc />
    public bool Allows(XPathQName name) => Constraint.Allows(name, DefinedNames, SiblingNames);

    /// <inheritdoc />
    public bool IsSubsetOf(IWildcard other) => Constraint.IsSubsetOf(ConstraintOf(other));

    /// <inheritdoc />
    public IWildcard? Union(IWildcard other) => Derive(Constraint.Union(ConstraintOf(other)), other);

    /// <inheritdoc />
    public IWildcard? Intersect(IWildcard other) => Derive(Constraint.Intersect(ConstraintOf(other)), other);

    // Wildcard Properties Correct, clause 4: the namespace of each disallowed name is allowed by the wildcard.
    private static bool NamesAreAllowed(
        XsdIssueReporter reporter,
        SchemaSyntaxNode node,
        NameConstraint spaces,
        List<XPathQName> names
    )
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (!spaces.AllowsNamespace(names[i].NamespaceName))
            {
                reporter.Report(
                    XsdRuleLookup.Find(XsdRuleCodes.WPropsCorrect),
                    node.Location,
                    "The notQName " + names[i] + " is in a namespace that the wildcard does not allow.",
                    "w-props-correct.4"
                );
                return false;
            }
        }

        return true;
    }

    private static bool Reject(XsdIssueReporter reporter, SchemaSyntaxNode node, string message)
    {
        reporter.Report(XsdRuleLookup.SrcWildcard, node.Location, message);
        return false;
    }

    private static bool TryReadNames(
        SchemaSyntaxNode node,
        SchemaNamespaceScope scope,
        string text,
        bool attributeWildcard,
        List<XPathQName> names,
        ref bool defined,
        ref bool sibling
    )
    {
        var cursor = new XsdListCursor(new TextRange(0, text.Length));
        while (cursor.TryNext(text.AsSpan(), out var item))
        {
            var token = text.Substring(item.Offset, item.Length);
            if (string.Equals(token, "##defined", StringComparison.Ordinal))
            {
                defined = true;
            }
            else if (string.Equals(token, "##definedSibling", StringComparison.Ordinal) && !attributeWildcard)
            {
                sibling = true;
            }
            else if (token.StartsWith("##", StringComparison.Ordinal) || !scope.TryResolve(node, token, out var name))
            {
                return false;
            }
            else
            {
                names.Add(name);
            }
        }

        return true;
    }

    private NegatedWildcard Derive(NameConstraint constraint, IWildcard other)
    {
        var near = other as NegatedWildcard;
        return new(
            constraint,
            ProcessContents,
            Location,
            DefinedNames ?? near?.DefinedNames,
            SiblingNames ?? near?.SiblingNames
        );
    }
}
