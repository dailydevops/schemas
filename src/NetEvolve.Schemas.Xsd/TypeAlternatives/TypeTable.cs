namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The type table of an element declaration (Structures 1.1, 3.3.1): the alternatives that have a test in document order, and the
/// default alternative, which is the last one when it has no test. Without a match the type of the declaration applies.
/// </summary>
/// <remarks>
/// <para>
/// Decisions: an alternative without a test that is not the last one cannot be a member of the alternatives (every alternative
/// there needs a test, <c>e-props-correct</c>), so it is reported and left out; the alternatives after it stay. The type of an
/// alternative must be <c>xs:error</c> or derived from the declared type (<see cref="Check"/>).
/// </para>
/// <para>
/// Limitation: the table is not yet a property of the element declaration (XSD-78).
/// </para>
/// </remarks>
internal sealed class TypeTable
{
    /// <summary>Gets the number of alternatives that have a test.</summary>
    public int Count => throw new NotSupportedException();

    /// <summary>Gets the alternative without a test, <see langword="null"/> when the type of the declaration is the default.</summary>
    public TypeAlternative? Default => throw new NotSupportedException();

    /// <summary>Gets an alternative that has a test.</summary>
    /// <param name="index">The index in document order.</param>
    /// <returns>The alternative.</returns>
    public TypeAlternative this[int index] => throw new NotSupportedException();

    /// <summary>Creates the table of an element from its alternatives.</summary>
    /// <param name="alternatives">The alternatives in document order.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The table, <see langword="null"/> when there is no alternative.</returns>
    internal static TypeTable? Create(IReadOnlyList<TypeAlternative> alternatives, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Creates the table of an element from the <c>alternative</c> children of its syntax.</summary>
    /// <param name="element">The <c>element</c> element.</param>
    /// <param name="builder">The builder of the alternatives.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The table, <see langword="null"/> when the element has no <c>alternative</c> child.</returns>
    internal static TypeTable? FromSyntax(
        SchemaSyntaxNode element,
        TypeAlternativeBuilder builder,
        XsdIssueReporter reporter
    ) => throw new NotSupportedException();

    /// <summary>
    /// Checks that the type of every alternative, the default one included, is <c>xs:error</c> or validly derived from the declared type.
    /// </summary>
    /// <param name="declaredType">The type of the element declaration.</param>
    /// <param name="derivation">The type derivation check.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Check(IXsdComponent declaredType, IDerivationCheck derivation, XsdIssueReporter reporter) =>
        throw new NotSupportedException();
}
