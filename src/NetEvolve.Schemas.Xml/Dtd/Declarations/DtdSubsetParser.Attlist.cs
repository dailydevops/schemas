namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Dtd.Model;

internal sealed partial class DtdSubsetParser
{
    // Production 52; `<!ATTLIST` is consumed.
    private bool ParseAttlist()
    {
        if (!RequireSpace() || !ScanName(out var element))
        {
            return false;
        }

        while (true)
        {
            var separated = SkipSpaces();
            if (Current('>'))
            {
                _pos++;
                return true;
            }

            if (!separated)
            {
                return FailHere();
            }

            if (!ParseAttributeDefinition(element))
            {
                return false;
            }
        }
    }

    // Production 53.
    private bool ParseAttributeDefinition(string element)
    {
        if (!ScanName(out var name) || !RequireSpace() || !ParseAttributeType(out var type, out var values))
        {
            return false;
        }

        if (!RequireSpace())
        {
            return false;
        }

        DtdDefaultKind kind;
        string? value = null;
        if (Match("#REQUIRED"))
        {
            kind = DtdDefaultKind.Required;
        }
        else if (Match("#IMPLIED"))
        {
            kind = DtdDefaultKind.Implied;
        }
        else
        {
            kind = Match("#FIXED") ? DtdDefaultKind.Fixed : DtdDefaultKind.Default;
            if ((kind == DtdDefaultKind.Fixed && !RequireSpace()) || !ScanAttributeValue(out value))
            {
                return false;
            }
        }

        _builder.AddAttribute(element, new DtdAttributeDefinition(name, type, values, kind, value));
        return true;
    }

    // Production 54: the longer keyword is tried before its prefix.
    private bool ParseAttributeType(out DtdAttributeType type, out List<string>? values)
    {
        values = null;
        type = DtdAttributeType.Cdata;
        if (Match("CDATA"))
        {
            return true;
        }

        if (Match("NOTATION"))
        {
            type = DtdAttributeType.Notation;
            if (!RequireSpace())
            {
                return false;
            }

            return Current('(') ? ScanChoice(notation: true, out values) : FailHere();
        }

        if (Current('('))
        {
            type = DtdAttributeType.Enumeration;
            return ScanChoice(notation: false, out values);
        }

        if (Match("IDREFS"))
        {
            type = DtdAttributeType.IdRefs;
        }
        else if (Match("IDREF"))
        {
            type = DtdAttributeType.IdRef;
        }
        else if (Match("ID"))
        {
            type = DtdAttributeType.Id;
        }
        else if (Match("ENTITIES"))
        {
            type = DtdAttributeType.Entities;
        }
        else if (Match("ENTITY"))
        {
            type = DtdAttributeType.Entity;
        }
        else if (Match("NMTOKENS"))
        {
            type = DtdAttributeType.NmTokens;
        }
        else if (Match("NMTOKEN"))
        {
            type = DtdAttributeType.NmToken;
        }
        else
        {
            return FailHere();
        }

        return true;
    }

    // Productions 58 and 59: `(` Name or Nmtoken, `|` separated, `)`.
    private bool ScanChoice(bool notation, out List<string>? values)
    {
        values = null;
        var list = new List<string>();
        _pos++;
        _ = SkipSpaces();
        while (true)
        {
            if (!(notation ? ScanName(out var item) : ScanNmtoken(out item)))
            {
                return false;
            }

            list.Add(item);
            _ = SkipSpaces();
            if (Current(')'))
            {
                _pos++;
                values = list;
                return true;
            }

            if (!Current('|'))
            {
                return FailHere();
            }

            _pos++;
            _ = SkipSpaces();
        }
    }
}
