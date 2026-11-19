namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

internal sealed partial class DtdSubsetParser
{
    // Production 45; `<!ELEMENT` is consumed.
    private bool ParseElement()
    {
        if (!RequireSpace() || !ScanName(out var name) || !RequireSpace())
        {
            return false;
        }

        DtdElementDeclaration declaration;
        if (Match("EMPTY"))
        {
            declaration = new DtdElementDeclaration(name, DtdContentKind.Empty, null, _inExternal);
        }
        else if (Match("ANY"))
        {
            declaration = new DtdElementDeclaration(name, DtdContentKind.Any, null, _inExternal);
        }
        else if (Current('('))
        {
            if (!ParseContent(out var kind, out var content))
            {
                return false;
            }

            declaration = new DtdElementDeclaration(name, kind, content, _inExternal);
        }
        else
        {
            return FailHere();
        }

        if (!ExpectClose() || !CountDeclaration())
        {
            return false;
        }

        _builder.AddElement(declaration);
        return true;
    }

    // Productions 47 and 51 at `(`: mixed content when `#PCDATA` follows, otherwise element content.
    private bool ParseContent(out DtdContentKind kind, out DtdContentParticle? content)
    {
        _pos++;
        _ = SkipSpaces();
        if (Match("#PCDATA"))
        {
            kind = DtdContentKind.Mixed;
            return ParseMixed(out content);
        }

        kind = DtdContentKind.Children;
        return ParseGroup(1, out content);
    }

    // Production 51 after `(` and `#PCDATA`; names need the `*` that follows the group.
    private bool ParseMixed(out DtdContentParticle? content)
    {
        content = null;
        var names = new List<DtdContentParticle>();
        while (true)
        {
            _ = SkipSpaces();
            if (Current(')'))
            {
                _pos++;
                break;
            }

            if (!Current('|'))
            {
                return FailHere();
            }

            _pos++;
            _ = SkipSpaces();
            if (!ScanName(out var name))
            {
                return false;
            }

            names.Add(new DtdContentParticle(DtdParticleKind.Name, name, DtdOccurrence.Once, null));
        }

        var star = Current('*');
        if (star)
        {
            _pos++;
        }
        else if (names.Count > 0)
        {
            return FailHere();
        }

        content = new DtdContentParticle(
            DtdParticleKind.Choice,
            null,
            star ? DtdOccurrence.ZeroOrMore : DtdOccurrence.Once,
            names
        );
        return true;
    }

    // Productions 47 to 50 after `(`; `depth` counts the open groups.
    private bool ParseGroup(int depth, out DtdContentParticle? group)
    {
        group = null;
        if (depth > _limits.MaxElementDepth)
        {
            return Fail(XmlErrorCodes.XML1102);
        }

        var items = new List<DtdContentParticle>();
        var separator = '\0';
        _ = SkipSpaces();
        while (true)
        {
            if (!ParseParticle(depth, out var item))
            {
                return false;
            }

            items.Add(item!);
            _ = SkipSpaces();
            if (Current(')'))
            {
                _pos++;
                break;
            }

            var c = _pos < _text.Length ? _text[_pos] : '\0';
            if (c is not (',' or '|') || (separator != '\0' && separator != c))
            {
                return FailHere();
            }

            separator = c;
            _pos++;
            _ = SkipSpaces();
        }

        group = new DtdContentParticle(
            separator == '|' ? DtdParticleKind.Choice : DtdParticleKind.Sequence,
            null,
            ScanOccurrence(),
            items
        );
        return true;
    }

    // Production 48: a name or a nested group, each with an occurrence indicator.
    private bool ParseParticle(int depth, out DtdContentParticle? particle)
    {
        if (Current('('))
        {
            _pos++;
            return ParseGroup(depth + 1, out particle);
        }

        particle = null;
        if (!ScanName(out var name))
        {
            return false;
        }

        particle = new DtdContentParticle(DtdParticleKind.Name, name, ScanOccurrence(), null);
        return true;
    }

    private DtdOccurrence ScanOccurrence()
    {
        var occurrence =
            Current('?') ? DtdOccurrence.Optional
            : Current('*') ? DtdOccurrence.ZeroOrMore
            : Current('+') ? DtdOccurrence.OneOrMore
            : DtdOccurrence.Once;
        if (occurrence != DtdOccurrence.Once)
        {
            _pos++;
        }

        return occurrence;
    }
}
