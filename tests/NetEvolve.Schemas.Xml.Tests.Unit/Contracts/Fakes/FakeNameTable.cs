namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A dictionary-backed name table with a name limit and a freeze switch.</summary>
internal sealed class FakeNameTable : XmlNameTable
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal) { [string.Empty] = 0 };
    private readonly List<string> _strings = [string.Empty];
    private readonly int _maxNames;
    private bool _frozen;

    public FakeNameTable(int maxNames = int.MaxValue) => _maxNames = maxNames;

    public override int Count => _strings.Count;

    public override bool IsFrozen => _frozen;

    public override XmlNameTableStatus TryIntern(ReadOnlySpan<char> text, out XmlSymbol symbol)
    {
        var key = text.ToString();
        if (_ids.TryGetValue(key, out var id))
        {
            symbol = new XmlSymbol(id);
            return XmlNameTableStatus.Ok;
        }

        symbol = default;
        if (_frozen)
        {
            return XmlNameTableStatus.Frozen;
        }

        if (_strings.Count >= _maxNames)
        {
            return XmlNameTableStatus.LimitExceeded;
        }

        _ids[key] = _strings.Count;
        _strings.Add(key);
        symbol = new XmlSymbol(_strings.Count - 1);
        return XmlNameTableStatus.Ok;
    }

    public override bool TryGet(ReadOnlySpan<char> text, out XmlSymbol symbol)
    {
        if (_ids.TryGetValue(text.ToString(), out var id))
        {
            symbol = new XmlSymbol(id);
            return true;
        }

        symbol = default;
        return false;
    }

    public override string GetString(XmlSymbol symbol) =>
        TryGetString(symbol, out var text) ? text! : throw new ArgumentOutOfRangeException(nameof(symbol));

    public override bool TryGetString(XmlSymbol symbol, out string? text)
    {
        if (symbol.Value < _strings.Count)
        {
            text = _strings[symbol.Value];
            return true;
        }

        text = null;
        return false;
    }

    public override void Freeze() => _frozen = true;
}
