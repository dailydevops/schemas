namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
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
    private readonly XmlTreeBuilder _builder = new(trackPositions: false);
    private readonly XPathEvaluationContext _context = new();

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
    )
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(declaredType);
        ArgumentNullException.ThrowIfNull(inherited);

        if (table.Count > 0)
        {
            var root = BuildTree(element, attributes, values, inherited.Inherited);
            _context.Reset();
            _context.SetContextItem(XdmItem.FromNode(new XPathNode(root)));
            for (var i = 0; i < table.Count; i++)
            {
                if (IsTrue(table[i].Test))
                {
                    return Choose(table[i]);
                }
            }
        }

        return table.Default is { } fallback ? Choose(fallback) : new TypeSelection(declaredType, null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _builder.Dispose();
        _context.Dispose();
    }

    private static TypeSelection Choose(TypeAlternative alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative.TypeDefinition);

        return new TypeSelection(alternative.TypeDefinition!, alternative);
    }

    private XmlNode BuildTree(
        XPathQName element,
        ReadOnlySpan<TypeAlternativeAttribute> attributes,
        ReadOnlySpan<char> values,
        ReadOnlySpan<InheritedAttribute> inherited
    )
    {
        var names = _builder.Names;
        var name = Intern(names, element);
        _builder.StartElement(name, default);
        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            ArgumentOutOfRangeException.ThrowIfLessThan(attribute.Start, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(attribute.Length, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(attribute.Start + attribute.Length, values.Length);

            _builder.Attribute(Intern(names, attribute.Name), values.Slice(attribute.Start, attribute.Length));
        }

        for (var i = 0; i < inherited.Length; i++)
        {
            _builder.Attribute(Intern(names, inherited[i].Name), inherited[i].Value.AsSpan());
        }

        _builder.EndAttributes();
        _builder.EndElement(name);
        var tree = _builder.Complete();

        // Without a document node the first node of the fragment is the element.
        return new XmlNode(tree, 0);
    }

    private static XmlName Intern(XmlNameTable names, XPathQName name) =>
        names.Intern(default, name.LocalName.AsSpan(), name.NamespaceName.AsSpan());

    private bool IsTrue(XPathExpression? test)
    {
        if (test is null)
        {
            return false;
        }

        var result = test.TryEvaluate(_context);
        if (result.Status != XPathEvaluationStatus.Ok)
        {
            return false;
        }

        var value = result.Value;
        try
        {
            return XPathAtomization.EffectiveBooleanValue(in value);
        }
        catch (XPathException)
        {
            return false;
        }
    }
}
