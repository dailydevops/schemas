namespace NetEvolve.Schemas.Xsd.IdentityConstraints.References;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The reuse of an identity constraint through the <see langword="ref"/> attribute of <c>unique</c>, <c>key</c>, or <c>keyref</c>
/// (XSD 1.1, Structures section 3.11.2). The reference is no component of its own: after <see cref="Resolve"/> the element
/// declaration holds the referenced <see cref="IdentityConstraintDefinition"/> itself, shared and not copied.
/// </summary>
/// <remarks>
/// Limitations: the referenced constraint keeps its selector and fields as written in its own schema document, so
/// <c>xpathDefaultNamespace</c> (XSD-85) of the referring element plays no role. The reference is registered nowhere; wiring it into the
/// compilation pipeline is XSD-78.
/// </remarks>
internal sealed class IdentityConstraintReference
{
    private IdentityConstraintReference(IdentityConstraintKind kind, XPathQName name, DocumentLocation location)
    {
        ConstraintKind = kind;
        Name = name;
        Location = location;
    }

    /// <summary>Gets the category the referenced constraint must have, the one of the referring element.</summary>
    public IdentityConstraintKind ConstraintKind { get; }

    /// <summary>Gets the expanded name of the referenced constraint.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the location of the referring element.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets the referenced constraint, <see langword="null"/> until <see cref="Resolve"/> succeeded.</summary>
    public IdentityConstraintDefinition? Target { get; private set; }

    /// <summary>Reads the reference of a <c>unique</c>, <c>key</c>, or <c>keyref</c> element that has the <see langword="ref"/> attribute.</summary>
    /// <param name="element">The element, which carries <see langword="ref"/>.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="version">The configured XSD version; <see langword="ref"/> is an error in <see cref="XsdVersion.V10"/>.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The reference, <see langword="null"/> after an issue was reported.</returns>
    internal static IdentityConstraintReference? Create(
        SchemaSyntaxNode element,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(reporter);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        var text = element.FindAttribute("ref");
        Argument.ThrowIfNull(text);

        if (version == XsdVersion.V10)
        {
            reporter.Report(
                XsdRuleCodes.Strict10IdentityConstraintRef,
                element.Location,
                "The XSD 1.1 attribute 'ref' on an identity constraint is not allowed in strict 1.0 mode."
            );
            return null;
        }

        if (element.FindAttribute("name") is not null)
        {
            Report(reporter, element, "src-identity-constraint.1", "An identity constraint has either ref or name.");
            return null;
        }

        if (
            element.FindAttribute("refer") is not null
            || element.CountChildren("selector") != 0
            || element.CountChildren("field") != 0
        )
        {
            Report(
                reporter,
                element,
                "src-identity-constraint.4",
                "An identity constraint with ref has neither refer, selector, nor field."
            );
            return null;
        }

        if (!scope.TryResolve(element, text!, out var name))
        {
            reporter.Report(
                XsdRuleCodes.SrcResolve,
                element.Location,
                "The prefix or the value of ref '" + text + "' is not valid."
            );
            return null;
        }

        var kind = element.Name.LocalName switch
        {
            "key" => IdentityConstraintKind.Key,
            "keyref" => IdentityConstraintKind.Keyref,
            _ => IdentityConstraintKind.Unique,
        };
        return new IdentityConstraintReference(kind, name, element.Location);
    }

    /// <summary>Looks the referenced constraint up and checks that it has the category of the referring element.</summary>
    /// <param name="registry">The registry holding the identity constraints of every schema document of the set.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="true"/> when <see cref="Target"/> is set.</returns>
    internal bool Resolve(IComponentRegistry registry, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(reporter);

        if (
            !registry.TryGet(XsdComponentKind.IdentityConstraint, Name, out var found)
            || found is not IdentityConstraintDefinition target
        )
        {
            reporter.Report(XsdRuleCodes.SrcResolve, Location, "The identity constraint " + Name + " is not declared.");
            return false;
        }

        if (target.ConstraintKind != ConstraintKind)
        {
            reporter.Report(
                XsdRuleLookup.Find(XsdRuleCodes.SrcIdentityConstraint),
                Location,
                "The identity constraint "
                    + Name
                    + " is a "
                    + target.ConstraintKind
                    + ", not a "
                    + ConstraintKind
                    + ".",
                "src-identity-constraint.5"
            );
            return false;
        }

        Target = target;
        return true;
    }

    private static void Report(XsdIssueReporter reporter, SchemaSyntaxNode element, string clause, string message) =>
        reporter.Report(XsdRuleLookup.Find(XsdRuleCodes.SrcIdentityConstraint), element.Location, message, clause);
}
