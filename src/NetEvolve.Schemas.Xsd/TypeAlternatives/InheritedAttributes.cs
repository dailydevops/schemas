namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The inherited attributes on the validator stack (Structures 1.1, 3.3.4 and 3.12.4): the attributes whose declaration is
/// <c>inheritable</c> stay visible to the type alternative tests of every descendant.
/// </summary>
/// <remarks>
/// <para>
/// The validator calls <see cref="Enter"/> once per element when all attributes are known and <see cref="Leave"/> at its end tag.
/// A frame keeps what the element's children see: the attributes that the element inherited and does not shadow, then the
/// inheritable attributes of the element. An attribute of the element shadows an inherited one of the same expanded name whether
/// or not it is inheritable, so the nearest ancestor wins, and a non-inheritable attribute hides the farther one from the whole
/// subtree.
/// </para>
/// <para>
/// Decisions: only attributes that the document specifies take part, because an attribute that a default adds is no member of the
/// <c>[attributes]</c> of the element (the caller does not hand in defaulted attributes); the schema instance attributes take part
/// like any other attribute. The frames are arrays that grow geometrically and are reused; the only allocation of a run is the
/// value string of an inheritable attribute, which every descendant shares.
/// </para>
/// <para>
/// Limitation: the caller derives <see cref="TypeAlternativeAttribute.IsInheritable"/> from the attribute declaration, which has
/// no <c>inheritable</c> property until the validator is wired (XSD-78); <see cref="AttributeInheritance"/> reads it from the syntax.
/// </para>
/// </remarks>
internal sealed class InheritedAttributes
{
    private InheritedAttribute[] _entries = new InheritedAttribute[8];
    private int _count;
    private int[] _frameStarts = new int[8];
    private int[] _frameInherited = new int[8];
    private int _depth;

    /// <summary>Gets the number of open frames.</summary>
    internal int Depth => _depth;

    /// <summary>Gets the attributes that the innermost open element inherited, empty without an open frame.</summary>
    internal ReadOnlySpan<InheritedAttribute> Inherited =>
        _depth == 0
            ? default
            : new ReadOnlySpan<InheritedAttribute>(_entries, _frameStarts[_depth - 1], _frameInherited[_depth - 1]);

    /// <summary>Opens the frame of an element.</summary>
    /// <param name="attributes">The specified attributes of the element.</param>
    /// <param name="values">The buffer that holds the values of <paramref name="attributes"/>.</param>
    internal void Enter(ReadOnlySpan<TypeAlternativeAttribute> attributes, ReadOnlySpan<char> values)
    {
        var start = _count;
        if (_depth > 0)
        {
            for (var i = _frameStarts[_depth - 1]; i < start; i++)
            {
                var entry = _entries[i];
                if (!IsShadowed(entry.Name, attributes))
                {
                    Append(entry);
                }
            }
        }

        var inherited = _count - start;
        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            if (!attribute.IsInheritable)
            {
                continue;
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(attribute.Start, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(attribute.Length, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(attribute.Start + attribute.Length, values.Length);

            Append(new InheritedAttribute(attribute.Name, values.Slice(attribute.Start, attribute.Length).ToString()));
        }

        if (_depth == _frameStarts.Length)
        {
            Array.Resize(ref _frameStarts, _depth * 2);
            Array.Resize(ref _frameInherited, _depth * 2);
        }

        _frameStarts[_depth] = start;
        _frameInherited[_depth] = inherited;
        _depth++;
    }

    /// <summary>Closes the frame of the innermost open element.</summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    internal void Leave()
    {
        if (_depth == 0)
        {
            throw new InvalidOperationException("Leave requires an open frame.");
        }

        _depth--;
        var start = _frameStarts[_depth];
        Array.Clear(_entries, start, _count - start);
        _count = start;
    }

    private void Append(in InheritedAttribute entry)
    {
        if (_count == _entries.Length)
        {
            Array.Resize(ref _entries, _count * 2);
        }

        _entries[_count++] = entry;
    }

    private static bool IsShadowed(XPathQName name, ReadOnlySpan<TypeAlternativeAttribute> attributes)
    {
        for (var i = 0; i < attributes.Length; i++)
        {
            if (attributes[i].Name == name)
            {
                return true;
            }
        }

        return false;
    }
}
