namespace NetEvolve.Schemas.Xml.Writer;

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

public sealed partial class XmlWriter
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    /// <summary>One pending namespace declaration or attribute of the current start tag; the characters live in the shared buffer.</summary>
    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        public bool IsDecl;
        public bool Drop;
        public XmlName Name;
        public int NameStart;
        public int NameLen;
        public int ValStart;
        public int ValLen;
    }

    private void FixDeclarations(int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!_entries[i].IsDecl)
            {
                continue;
            }

            var prefix = _entries[i].Name.Prefix;
            var uri = _entries[i].Name.NamespaceUri;
            var found = TryLookup(prefix.Value, out var current);
            if ((found && current == uri.Value) || (!found && prefix.IsEmpty && uri.IsEmpty))
            {
                _entries[i].Drop = true;
            }
            else
            {
                AddBinding(prefix.Value, uri.Value);
            }
        }
    }

    // Returns the name with the prefix to write; adds a declaration entry when the name needs one.
    private XmlName FixName(XmlName name, bool attribute, int mark)
    {
        var prefix = name.Prefix;
        var uri = name.NamespaceUri;
        if (uri.IsEmpty)
        {
            if (!attribute && TryLookup(0, out var defaultUri) && defaultUri != 0)
            {
                Declare(default, default);
            }

            return name;
        }

        if (string.Equals(Str(uri), XmlNamespace, StringComparison.Ordinal))
        {
            return new XmlName(InternChecked("xml"), name.Local, uri);
        }

        if (!(attribute && prefix.IsEmpty))
        {
            var found = TryLookup(prefix.Value, out var bound);
            var effective = found ? bound : (prefix.IsEmpty ? 0 : -1);
            if (effective == uri.Value)
            {
                return name;
            }

            if (effective == -1 || (prefix.IsEmpty && effective == 0))
            {
                Declare(prefix, uri);
                return name;
            }
        }

        if (TryFindPrefix(uri.Value, out var reuse))
        {
            return new XmlName(new XmlSymbol(reuse), name.Local, uri);
        }

        if (!attribute && prefix.IsEmpty && !BoundSince(0, mark))
        {
            Declare(prefix, uri);
            return name;
        }

        var fresh = FreshPrefix();
        Declare(fresh, uri);

        return new XmlName(fresh, name.Local, uri);
    }

    private void Declare(XmlSymbol prefix, XmlSymbol uri)
    {
        AddBinding(prefix.Value, uri.Value);
        AddEntry(new Entry { IsDecl = true, Name = new XmlName(prefix, default, uri) });
    }

    private XmlSymbol FreshPrefix()
    {
        for (var k = 1; ; k++)
        {
            var symbol = InternChecked("ns" + k.ToString(CultureInfo.InvariantCulture));
            if (!TryLookup(symbol.Value, out _))
            {
                return symbol;
            }
        }
    }

    private XmlSymbol InternChecked(string text)
    {
        var status = _names.TryIntern(text.AsSpan(), out var symbol);

        return status switch
        {
            XmlNameTableStatus.Ok => symbol,
            XmlNameTableStatus.Frozen => throw Error(XmlErrorCodes.XML1116),
            _ => throw Error(XmlErrorCodes.XML1111),
        };
    }

    private bool TryLookup(int prefix, out int uri)
    {
        for (var i = _bindCount - 1; i >= 0; i--)
        {
            if (_bindPrefix[i] == prefix)
            {
                uri = _bindUri[i];
                return true;
            }
        }

        uri = 0;
        return false;
    }

    // Finds a non-empty prefix whose current binding is the URI.
    private bool TryFindPrefix(int uri, out int prefix)
    {
        for (var i = _bindCount - 1; i >= 0; i--)
        {
            var candidate = _bindPrefix[i];
            if (_bindUri[i] == uri && candidate != 0 && TryLookup(candidate, out var current) && current == uri)
            {
                prefix = candidate;
                return true;
            }
        }

        prefix = 0;
        return false;
    }

    private bool BoundSince(int prefix, int mark)
    {
        for (var i = mark; i < _bindCount; i++)
        {
            if (_bindPrefix[i] == prefix)
            {
                return true;
            }
        }

        return false;
    }

    private void AddBinding(int prefix, int uri)
    {
        if (_bindCount == _bindPrefix.Length)
        {
            Array.Resize(ref _bindPrefix, _bindCount * 2);
            Array.Resize(ref _bindUri, _bindCount * 2);
        }

        _bindPrefix[_bindCount] = prefix;
        _bindUri[_bindCount] = uri;
        _bindCount++;
    }

    private void BuildQualifiedNames()
    {
        for (var i = 0; i < _entryCount; i++)
        {
            if (_entries[i].Drop)
            {
                continue;
            }

            var name = _entries[i].Name;
            _entries[i].NameStart = _chars.Length;
            if (_entries[i].IsDecl)
            {
                _chars.Append("xmlns".AsSpan());
                if (!name.Prefix.IsEmpty)
                {
                    _chars.Append(':');
                    _chars.Append(Str(name.Prefix).AsSpan());
                }

                _entries[i].NameLen = _chars.Length - _entries[i].NameStart;
                _entries[i].ValStart = _chars.Length;
                _chars.Append(Str(name.NamespaceUri).AsSpan());
                _entries[i].ValLen = _chars.Length - _entries[i].ValStart;
            }
            else
            {
                if (!name.Prefix.IsEmpty)
                {
                    _chars.Append(Str(name.Prefix).AsSpan());
                    _chars.Append(':');
                }

                _chars.Append(Str(name.Local).AsSpan());
                _entries[i].NameLen = _chars.Length - _entries[i].NameStart;
            }
        }
    }

    // ponytail: insertion sort, quadratic; attribute counts per element are small, switch to a heap sort if limits allow thousands.
    private void SortEntries()
    {
        var live = 0;
        for (var i = 0; i < _entryCount; i++)
        {
            if (!_entries[i].Drop)
            {
                _entries[live++] = _entries[i];
            }
        }

        _entryCount = live;
        var span = _chars.WrittenSpan;
        for (var i = 1; i < live; i++)
        {
            var current = _entries[i];
            var key = span.Slice(current.NameStart, current.NameLen);
            var j = i - 1;
            while (j >= 0 && CompareCodePoints(span.Slice(_entries[j].NameStart, _entries[j].NameLen), key) > 0)
            {
                _entries[j + 1] = _entries[j];
                j--;
            }

            _entries[j + 1] = current;
        }
    }

    private static int CompareCodePoints(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        int i = 0,
            j = 0;
        while (i < left.Length && j < right.Length)
        {
            var a = NextCodePoint(left, ref i);
            var b = NextCodePoint(right, ref j);
            if (a != b)
            {
                return a < b ? -1 : 1;
            }
        }

        return i < left.Length ? 1
            : j < right.Length ? -1
            : 0;
    }

    private static int NextCodePoint(ReadOnlySpan<char> text, ref int index)
    {
        var c = text[index++];
        if (char.IsHighSurrogate(c) && index < text.Length && char.IsLowSurrogate(text[index]))
        {
            return char.ConvertToUtf32(c, text[index++]);
        }

        return c;
    }
}
