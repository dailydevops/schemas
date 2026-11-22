namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// Selects the type of an element from its type table (Structures 1.1, 3.12.4): the first alternative whose test is true wins, then
/// the default alternative, then the declared type. A test sees a tiny tree that holds the element, its attributes, and the
/// attributes that it inherited, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The element is the root of the tree and has no parent, no child, and no sibling, so a test that reads a child sees the empty
/// sequence and a test that reads an ancestor finds none. Nothing is annotated, so every value is untyped. The tree is built only
/// when the table has an alternative with a test, once for all its tests.
/// </para>
/// <para>
/// Decisions: a test that raises a dynamic error or a type error is false and does not make the instance invalid (the type table
/// differs from an assertion in this); a test whose value has no effective boolean value (a sequence of two atomic values) is such an
/// error. A test that did not compile never matches. The schema instance attributes are visible, defaulted attributes are not.
/// </para>
/// <para>
/// Limitations: <c>xsi:type</c>, which replaces the selection, is XSD-55 (#308), and the wiring into the validator, where the
/// selected type and the validity are read, is XSD-78. An instance is not thread-safe: use one per validation run, and dispose it.
/// </para>
/// </remarks>
internal sealed class TypeAlternativeEvaluator : IDisposable
{
    /// <summary>Selects the type of an element.</summary>
    /// <param name="table">The type table of the element declaration.</param>
    /// <param name="declaredType">The type of the element declaration, which applies when no alternative matches.</param>
    /// <param name="element">The expanded name of the element.</param>
    /// <param name="attributes">The specified attributes of the element.</param>
    /// <param name="values">The buffer that holds the values of <paramref name="attributes"/>.</param>
    /// <param name="inherited">The inherited attributes, whose innermost frame is the one of the element.</param>
    /// <returns>The selection.</returns>
    internal TypeSelection Select(
        TypeTable table,
        IXsdComponent declaredType,
        XPathQName element,
        ReadOnlySpan<TypeAlternativeAttribute> attributes,
        ReadOnlySpan<char> values,
        InheritedAttributes inherited
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public void Dispose() { }
}
