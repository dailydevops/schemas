namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// An attribute declaration (Structures section 3.2). Immutable; the factories take the resolved type and report a violation of
/// Attribute Declaration Properties Correct as a located issue.
/// </summary>
/// <remarks>
/// Limitations: that a value constraint is valid for the type, and that no attribute has an ID type with a value constraint
/// (a-props-correct 3 and 4), is not checked because the simple type definition cannot validate a lexical value yet.
/// </remarks>
internal sealed class AttributeDeclaration : IXsdComponent
{
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    private AttributeDeclaration(
        XPathQName name,
        DocumentLocation location,
        bool isGlobal,
        SimpleTypeDefinition typeDefinition,
        AttributeValueConstraint valueConstraint,
        bool isInheritable
    )
    {
        Name = name;
        Location = location;
        IsGlobal = isGlobal;
        TypeDefinition = typeDefinition;
        ValueConstraint = valueConstraint;
        IsInheritable = isInheritable;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.AttributeDeclaration;

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets a value indicating whether the declaration is a child of the schema element.</summary>
    public bool IsGlobal { get; }

    /// <summary>Gets the type definition.</summary>
    public SimpleTypeDefinition TypeDefinition { get; }

    /// <summary>Gets the value constraint, <see langword="default"/> for none.</summary>
    public AttributeValueConstraint ValueConstraint { get; }

    /// <summary>Gets a value indicating whether the attribute is visible to the type alternative tests of the descendants (XSD 1.1, <c>inheritable</c>).</summary>
    public bool IsInheritable { get; }

    /// <summary>Declares a global attribute, whose name is always in the target namespace.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="targetNamespace">The target namespace, empty for none.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="typeDefinition">The resolved type.</param>
    /// <param name="valueConstraint">The value constraint.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="isInheritable">Whether the attribute is inheritable (XSD 1.1).</param>
    /// <returns>The declaration, <see langword="null"/> after an issue was reported.</returns>
    internal static AttributeDeclaration? Global(
        string localName,
        string targetNamespace,
        DocumentLocation location,
        SimpleTypeDefinition typeDefinition,
        AttributeValueConstraint valueConstraint,
        XsdIssueReporter reporter,
        bool isInheritable = false
    ) => Create(localName, targetNamespace, true, location, typeDefinition, valueConstraint, reporter, isInheritable);

    /// <summary>Declares a local attribute; its form decides the namespace of the name.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="targetNamespace">The target namespace, empty for none.</param>
    /// <param name="form">The form.</param>
    /// <param name="location">The location of the declaration.</param>
    /// <param name="typeDefinition">The resolved type.</param>
    /// <param name="valueConstraint">The value constraint.</param>
    /// <param name="reporter">The reporter.</param>
    /// <param name="isInheritable">Whether the attribute is inheritable (XSD 1.1).</param>
    /// <returns>The declaration, <see langword="null"/> after an issue was reported.</returns>
    internal static AttributeDeclaration? Local(
        string localName,
        string targetNamespace,
        AttributeForm form,
        DocumentLocation location,
        SimpleTypeDefinition typeDefinition,
        AttributeValueConstraint valueConstraint,
        XsdIssueReporter reporter,
        bool isInheritable = false
    )
    {
        ArgumentNullException.ThrowIfNull(targetNamespace);

        return Create(
            localName,
            form == AttributeForm.Qualified ? targetNamespace : string.Empty,
            false,
            location,
            typeDefinition,
            valueConstraint,
            reporter,
            isInheritable
        );
    }

    private static AttributeDeclaration? Create(
        string localName,
        string namespaceName,
        bool isGlobal,
        DocumentLocation location,
        SimpleTypeDefinition typeDefinition,
        AttributeValueConstraint valueConstraint,
        XsdIssueReporter reporter,
        bool isInheritable
    )
    {
        ArgumentNullException.ThrowIfNullOrEmpty(localName);
        ArgumentNullException.ThrowIfNull(namespaceName);
        ArgumentNullException.ThrowIfNull(typeDefinition);
        ArgumentNullException.ThrowIfNull(reporter);

        if (string.Equals(localName, "xmlns", StringComparison.Ordinal))
        {
            reporter.Report(XsdRuleCodes.NoXmlns, location, "An attribute must not be named xmlns.");
            return null;
        }

        if (string.Equals(namespaceName, XsiNamespace, StringComparison.Ordinal))
        {
            reporter.Report(
                XsdRuleCodes.NoXsi,
                location,
                "An attribute must not be in the XML Schema instance namespace."
            );
            return null;
        }

        return new(
            new XPathQName(namespaceName, localName),
            location,
            isGlobal,
            typeDefinition,
            valueConstraint,
            isInheritable
        );
    }
}
