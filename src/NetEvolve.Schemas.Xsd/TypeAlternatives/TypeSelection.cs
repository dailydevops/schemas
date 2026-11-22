namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The type that a type table selects for an element.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct TypeSelection
{
    internal TypeSelection(IXsdComponent type, TypeAlternative? alternative)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        Alternative = alternative;
    }

    /// <summary>Gets the selected type: the type of the first alternative whose test is true, else of the default alternative, else the declared type.</summary>
    internal IXsdComponent Type { get; }

    /// <summary>Gets the alternative that was selected, <see langword="null"/> when the declared type applies.</summary>
    internal TypeAlternative? Alternative { get; }

    /// <summary>Gets a value indicating whether the selected type is <c>xs:error</c>, which has no valid instance.</summary>
    internal bool IsError => IsErrorType(Type);

    /// <summary>Determines whether a type is the built-in type <c>xs:error</c>.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> for <c>xs:error</c>.</returns>
    internal static bool IsErrorType(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.Kind == XsdComponentKind.SimpleType
            && string.Equals(type.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && string.Equals(type.Name.LocalName, "error", StringComparison.Ordinal);
    }

    /// <summary>Makes the element invalid when the selected type is <c>xs:error</c>.</summary>
    /// <param name="element">The name of the element.</param>
    /// <param name="location">The location of the element.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="false"/> when an issue was reported because the type is <c>xs:error</c>.</returns>
    /// <remarks>The rule is Element Locally Valid (Type); the clause is the rule id, because <c>xs:error</c> has no clause of its own.</remarks>
    internal bool Validate(XPathQName element, DocumentLocation location, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        if (!IsError)
        {
            return true;
        }

        reporter.Report(
            XsdRuleCodes.CvcType,
            location,
            "The type table of element '" + element.LocalName + "' selected xs:error, which has no valid instance."
        );
        return false;
    }
}
