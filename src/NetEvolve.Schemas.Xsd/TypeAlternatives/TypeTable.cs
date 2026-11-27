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
/// The compilation pipeline reads the table from the <c>alternative</c> children of an element declaration, and the validator selects
/// the type from it.
/// </para>
/// </remarks>
internal sealed class TypeTable
{
    private readonly TypeAlternative[] _alternatives;

    private TypeTable(TypeAlternative[] alternatives, TypeAlternative? defaultAlternative)
    {
        _alternatives = alternatives;
        Default = defaultAlternative;
    }

    /// <summary>Gets the number of alternatives that have a test.</summary>
    public int Count => _alternatives.Length;

    /// <summary>Gets the alternative without a test, <see langword="null"/> when the type of the declaration is the default.</summary>
    public TypeAlternative? Default { get; }

    /// <summary>Gets an alternative that has a test.</summary>
    /// <param name="index">The index in document order.</param>
    /// <returns>The alternative.</returns>
    public TypeAlternative this[int index] => _alternatives[index];

    /// <summary>Creates the table of an element from its alternatives.</summary>
    /// <param name="alternatives">The alternatives in document order.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The table, <see langword="null"/> when there is no alternative.</returns>
    internal static TypeTable? Create(IReadOnlyList<TypeAlternative> alternatives, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(alternatives);
        ArgumentNullException.ThrowIfNull(reporter);

        if (alternatives.Count == 0)
        {
            return null;
        }

        var tested = new List<TypeAlternative>(alternatives.Count);
        TypeAlternative? defaultAlternative = null;
        for (var i = 0; i < alternatives.Count; i++)
        {
            var alternative = alternatives[i];
            ArgumentNullException.ThrowIfNull(alternative);

            if (!alternative.IsDefault)
            {
                tested.Add(alternative);
            }
            else if (i == alternatives.Count - 1)
            {
                defaultAlternative = alternative;
            }
            else
            {
                reporter.Report(
                    XsdRuleCodes.EPropsCorrect,
                    alternative.Location,
                    "Only the last type alternative may have no test."
                );
            }
        }

        return new TypeTable([.. tested], defaultAlternative);
    }

    /// <summary>Creates the table of an element from the <c>alternative</c> children of its syntax.</summary>
    /// <param name="element">The <c>element</c> element.</param>
    /// <param name="builder">The builder of the alternatives.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The table, <see langword="null"/> when the element has no <c>alternative</c> child.</returns>
    internal static TypeTable? FromSyntax(
        SchemaSyntaxNode element,
        TypeAlternativeBuilder builder,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(reporter);

        var alternatives = new List<TypeAlternative>();
        for (var i = 0; i < element.ChildCount; i++)
        {
            var child = element.GetChildNode(i);
            if (string.Equals(child.Name.LocalName, "alternative", System.StringComparison.Ordinal))
            {
                alternatives.Add(builder.BuildAlternative(child, reporter));
            }
        }

        return Create(alternatives, reporter);
    }

    /// <summary>
    /// Checks that the type of every alternative, the default one included, is <c>xs:error</c> or validly derived from the declared type.
    /// </summary>
    /// <param name="declaredType">The type of the element declaration.</param>
    /// <param name="derivation">The type derivation check.</param>
    /// <param name="reporter">The reporter.</param>
    /// <remarks>
    /// The question is Type Derivation OK with nothing blocked: restriction, extension, list, and union all count. An alternative
    /// whose type is not bound yet is skipped.
    /// </remarks>
    internal void Check(IXsdComponent declaredType, IDerivationCheck derivation, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(declaredType);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(reporter);

        for (var i = 0; i < _alternatives.Length; i++)
        {
            CheckOne(_alternatives[i], declaredType, derivation, reporter);
        }

        if (Default is not null)
        {
            CheckOne(Default, declaredType, derivation, reporter);
        }
    }

    private static void CheckOne(
        TypeAlternative alternative,
        IXsdComponent declaredType,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        var type = alternative.TypeDefinition;
        if (type is null || TypeSelection.IsErrorType(type))
        {
            return;
        }

        var result = derivation.Check(type, declaredType, XsdDerivationMethod.Substitution, XsdDerivationMethod.None);
        if (!result.IsValid)
        {
            reporter.Report(
                XsdRuleCodes.TaPropsCorrect,
                alternative.Location,
                "The type of the alternative is neither xs:error nor derived from the declared type ("
                    + result.ViolatedRule!.Id
                    + ")."
            );
        }
    }
}
