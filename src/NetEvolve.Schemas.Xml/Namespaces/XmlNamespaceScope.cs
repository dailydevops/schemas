namespace NetEvolve.Schemas.Xml.Namespaces;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The namespace processing layer: a scope stack of prefix and default namespace declarations that resolves raw
/// qualified names to <see cref="XmlName"/> values and enforces the namespace constraints of Namespaces in XML 1.0
/// (Third Edition).
/// </summary>
/// <remarks>
/// Per start tag the caller calls <see cref="PushElement"/>, then <see cref="Declare"/> for every attribute for which
/// <see cref="IsDeclaration"/> is true (all declarations first, because they may follow the names that use them), then
/// <see cref="ResolveElement"/> and <see cref="ResolveAttribute"/> for the remaining attributes, and
/// <see cref="PopElement"/> at the end tag. Every method returns <see langword="null"/> or the first
/// <see cref="XmlErrorCode"/> found; the caller reports it with the position. Limitations: Namespaces in XML 1.1 prefix
/// undeclaring is not supported (<c>xmlns:p=""</c> is <c>XML3005</c>); two declarations of one prefix in one start tag are
/// a duplicate attribute, which the reader reports, so the later one silently wins here; namespace names are not checked
/// as URI references. Not thread-safe. The name table must contain, or accept, the <c>xml</c> prefix and its namespace
/// name when the scope is created.
/// </remarks>
internal sealed class XmlNamespaceScope
{
    private const string XmlPrefix = "xml";
    private const string XmlnsPrefix = "xmlns";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    private readonly XmlNameTable _names;
    private readonly XmlSymbol _xmlPrefix;
    private XmlNamespaceBinding[] _bindings = new XmlNamespaceBinding[8];
    private int _count;
    private int[] _starts = new int[8];
    private int _depth;
    private XmlName[] _attributes = new XmlName[8];
    private int _attributeCount;

    /// <summary>Initializes a new instance of the <see cref="XmlNamespaceScope"/> class with the implicit <c>xml</c> binding.</summary>
    /// <param name="names">The table that interns every prefix, local name, and namespace URI.</param>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The table is frozen without, or too full for, the <c>xml</c> names.</exception>
    internal XmlNamespaceScope(XmlNameTable names)
    {
        ArgumentNullException.ThrowIfNull(names);
        _names = names;
        _xmlPrefix = names.Intern(XmlPrefix.AsSpan());
        _bindings[_count++] = new XmlNamespaceBinding(_xmlPrefix, names.Intern(XmlNamespace.AsSpan()));
    }

    /// <summary>Gets the number of open elements.</summary>
    internal int Depth => _depth;

    /// <summary>Gets the number of in-scope bindings: the visible, non-empty ones, the implicit <c>xml</c> binding included.</summary>
    internal int InScopeCount => CopyInScope(Span<XmlNamespaceBinding>.Empty, countOnly: true);

    /// <summary>Determines whether an attribute name is a namespace declaration: <c>xmlns</c> or <c>xmlns:</c> plus a name.</summary>
    /// <param name="attributeName">The raw attribute name.</param>
    /// <returns><see langword="true"/> for a declaration.</returns>
    internal static bool IsDeclaration(ReadOnlySpan<char> attributeName) =>
        attributeName.SequenceEqual(XmlnsPrefix.AsSpan())
        || (
            attributeName.Length > XmlnsPrefix.Length
            && attributeName[XmlnsPrefix.Length] == ':'
            && attributeName.StartsWith(XmlnsPrefix.AsSpan())
        );

    /// <summary>Opens the scope of a start tag.</summary>
    internal void PushElement()
    {
        if (_depth == _starts.Length)
        {
            Array.Resize(ref _starts, _depth * 2);
        }

        _starts[_depth++] = _count;
        _attributeCount = 0;
    }

    /// <summary>Closes the scope of the innermost open element, ending its declarations.</summary>
    /// <exception cref="InvalidOperationException">No element is open.</exception>
    internal void PopElement()
    {
        if (_depth == 0)
        {
            throw new InvalidOperationException("No element is open.");
        }

        _count = _starts[--_depth];
    }

    /// <summary>Adds the namespace declaration of an attribute to the current scope after checking its constraints.</summary>
    /// <param name="attributeName">The raw name, <c>xmlns</c> or <c>xmlns:prefix</c>; see <see cref="IsDeclaration"/>.</param>
    /// <param name="value">The attribute value, the namespace name.</param>
    /// <param name="binding">The interned binding, <see langword="default"/> on error.</param>
    /// <param name="isName"><see langword="true"/> when the caller already checked <paramref name="attributeName"/> against production Name.</param>
    /// <returns><see langword="null"/>, or <c>XML3002</c>, <c>XML3004</c>, <c>XML3005</c>, <c>XML1111</c>, <c>XML1116</c>.</returns>
    internal XmlErrorCode? Declare(
        ReadOnlySpan<char> attributeName,
        ReadOnlySpan<char> value,
        out XmlNamespaceBinding binding,
        bool isName = false
    )
    {
        binding = default;
        var prefix = ReadOnlySpan<char>.Empty;
        if (attributeName.Length > XmlnsPrefix.Length)
        {
            prefix = attributeName.Slice(XmlnsPrefix.Length + 1);
            if (!(isName ? IsTrustedLocalName(prefix) : IsNCName(prefix)))
            {
                return XmlErrorCodes.XML3004;
            }

            if (value.IsEmpty)
            {
                return XmlErrorCodes.XML3005;
            }

            if (prefix.SequenceEqual(XmlnsPrefix.AsSpan()))
            {
                return XmlErrorCodes.XML3002;
            }
        }

        var isXml = prefix.SequenceEqual(XmlPrefix.AsSpan());
        if (value.SequenceEqual(XmlnsNamespace.AsSpan()) || value.SequenceEqual(XmlNamespace.AsSpan()) != isXml)
        {
            return XmlErrorCodes.XML3002;
        }

        var failure = Intern(prefix, out var prefixSymbol);
        if (failure is not null)
        {
            return failure;
        }

        failure = Intern(value, out var uri);
        if (failure is not null)
        {
            return failure;
        }

        binding = new XmlNamespaceBinding(prefixSymbol, uri);
        if (_count == _bindings.Length)
        {
            Array.Resize(ref _bindings, _count * 2);
        }

        _bindings[_count++] = binding;
        return null;
    }

    /// <summary>Resolves the raw name of an element; an unprefixed name takes the default namespace.</summary>
    /// <param name="qualifiedName">The raw name.</param>
    /// <param name="name">The expanded name, <see langword="default"/> on error.</param>
    /// <param name="isName"><see langword="true"/> when the caller already checked <paramref name="qualifiedName"/> against production Name.</param>
    /// <returns><see langword="null"/>, or <c>XML3001</c>, <c>XML3002</c>, <c>XML3004</c>, <c>XML1111</c>, <c>XML1116</c>.</returns>
    internal XmlErrorCode? ResolveElement(ReadOnlySpan<char> qualifiedName, out XmlName name, bool isName = false) =>
        Resolve(qualifiedName, isAttribute: false, isName, out name);

    /// <summary>Resolves the raw name of an attribute and checks it is unique in the current start tag; an unprefixed name has no namespace.</summary>
    /// <param name="qualifiedName">The raw name, not a namespace declaration.</param>
    /// <param name="name">The expanded name, <see langword="default"/> on error.</param>
    /// <param name="isName"><see langword="true"/> when the caller already checked <paramref name="qualifiedName"/> against production Name.</param>
    /// <returns><see langword="null"/>, or <c>XML3001</c>, <c>XML3002</c>, <c>XML3003</c>, <c>XML3004</c>, <c>XML1111</c>, <c>XML1116</c>.</returns>
    internal XmlErrorCode? ResolveAttribute(ReadOnlySpan<char> qualifiedName, out XmlName name, bool isName = false)
    {
        var error = Resolve(qualifiedName, isAttribute: true, isName, out name);
        if (error is not null)
        {
            return error;
        }

        // ponytail: linear scan, quadratic in the attributes of one element, which the reader's attribute limit bounds.
        for (var i = 0; i < _attributeCount; i++)
        {
            if (_attributes[i].Local == name.Local && _attributes[i].NamespaceUri == name.NamespaceUri)
            {
                name = default;
                return XmlErrorCodes.XML3003;
            }
        }

        if (_attributeCount == _attributes.Length)
        {
            Array.Resize(ref _attributes, _attributeCount * 2);
        }

        _attributes[_attributeCount++] = name;
        return null;
    }

    /// <summary>Copies the in-scope bindings, innermost first, for the document model and the writer.</summary>
    /// <param name="destination">At least <see cref="InScopeCount"/> elements.</param>
    /// <returns>The number of bindings copied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is shorter than <see cref="InScopeCount"/>.</exception>
    internal int CopyInScope(Span<XmlNamespaceBinding> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, InScopeCount);
        return CopyInScope(destination, countOnly: false);
    }

    private int CopyInScope(Span<XmlNamespaceBinding> destination, bool countOnly)
    {
        var written = 0;
        for (var i = _count - 1; i >= 0; i--)
        {
            var candidate = _bindings[i];
            if (candidate.Uri.IsEmpty || IsShadowed(i))
            {
                continue;
            }

            if (!countOnly)
            {
                destination[written] = candidate;
            }

            written++;
        }

        return written;
    }

    private bool IsShadowed(int index)
    {
        for (var j = _count - 1; j > index; j--)
        {
            if (_bindings[j].Prefix == _bindings[index].Prefix)
            {
                return true;
            }
        }

        return false;
    }

    private XmlErrorCode? Resolve(ReadOnlySpan<char> qualifiedName, bool isAttribute, bool isName, out XmlName name)
    {
        name = default;
        var colon = qualifiedName.IndexOf(':');
        var prefix = colon < 0 ? ReadOnlySpan<char>.Empty : qualifiedName.Slice(0, colon);
        var local = qualifiedName.Slice(colon + 1);
        if (
            isName
                ? colon >= 0 && (colon == 0 || !IsTrustedLocalName(local))
                : (colon >= 0 && !IsNCName(prefix)) || !IsNCName(local)
        )
        {
            return XmlErrorCodes.XML3004;
        }

        var prefixSymbol = default(XmlSymbol);
        var uri = default(XmlSymbol);
        if (colon >= 0)
        {
            if (prefix.SequenceEqual(XmlnsPrefix.AsSpan()))
            {
                return XmlErrorCodes.XML3002;
            }

            // A declared prefix is always interned, so a miss means undeclared.
            if (!_names.TryGet(prefix, out prefixSymbol) || !TryLookup(prefixSymbol, out uri))
            {
                return XmlErrorCodes.XML3001;
            }
        }
        else if (!isAttribute)
        {
            _ = TryLookup(default, out uri);
        }

        var failure = Intern(local, out var localSymbol);
        if (failure is not null)
        {
            return failure;
        }

        name = new XmlName(prefixSymbol, localSymbol, uri);
        return null;
    }

    private bool TryLookup(XmlSymbol prefix, out XmlSymbol uri)
    {
        for (var i = _count - 1; i >= 0; i--)
        {
            if (_bindings[i].Prefix == prefix)
            {
                uri = _bindings[i].Uri;
                return !uri.IsEmpty || prefix.IsEmpty;
            }
        }

        uri = default;
        return prefix.IsEmpty;
    }

    private XmlErrorCode? Intern(ReadOnlySpan<char> text, out XmlSymbol symbol) =>
        _names.TryIntern(text, out symbol) switch
        {
            XmlNameTableStatus.Ok => null,
            XmlNameTableStatus.Frozen => XmlErrorCodes.XML1116,
            _ => XmlErrorCodes.XML1111,
        };

    // NCName: a Name without a colon (Namespaces in XML 1.0, production 4).
    private static bool IsNCName(ReadOnlySpan<char> text) =>
        !text.IsEmpty && text.IndexOf(':') < 0 && XmlCharClass.NameLength(text) == text.Length;

    // The part after the first colon of a text that already matches production Name: only the colon-free rule and the
    // name start rule of its first character are left. A high surrogate starts a supplementary code point, which the
    // Name check accepted as a name character and which is also a name start character.
    private static bool IsTrustedLocalName(ReadOnlySpan<char> local) =>
        !local.IsEmpty
        && local.IndexOf(':') < 0
        && (XmlCharClass.IsNameStartChar(local[0]) || char.IsHighSurrogate(local[0]));
}
