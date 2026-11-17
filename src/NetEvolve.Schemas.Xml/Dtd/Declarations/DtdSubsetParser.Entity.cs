namespace NetEvolve.Schemas.Xml.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

internal sealed partial class DtdSubsetParser
{
    // Productions 70 to 76; `<!ENTITY` is consumed.
    private bool ParseEntity()
    {
        if (!RequireSpace())
        {
            return false;
        }

        var isParameter = Current('%');
        if (isParameter)
        {
            _pos++;
            if (!RequireSpace())
            {
                return false;
            }
        }

        if (
            !ScanName(out var name)
            || !RequireSpace()
            || !ParseEntityDefinition(name, isParameter, out var declaration)
        )
        {
            return false;
        }

        if (!ExpectClose())
        {
            return false;
        }

        _builder.AddEntity(declaration!);
        return true;
    }

    // Productions 73 and 74: an entity value, or an external identifier with an NDATA declaration for general entities.
    private bool ParseEntityDefinition(string name, bool isParameter, out DtdEntityDeclaration? declaration)
    {
        declaration = null;
        if (Current('"') || Current('\''))
        {
            if (!ScanEntityValue(out var value))
            {
                return false;
            }

            declaration = new DtdEntityDeclaration(name, isParameter, value, null, null, null);
            return true;
        }

        if (!ScanExternalId(false, out var publicId, out var systemId))
        {
            return false;
        }

        string? notation = null;
        var afterId = _pos;
        if (!isParameter && SkipSpaces() && Match("NDATA"))
        {
            if (!RequireSpace() || !ScanName(out notation))
            {
                return false;
            }
        }
        else
        {
            _pos = afterId;
        }

        declaration = new DtdEntityDeclaration(name, isParameter, null, publicId, systemId, notation);
        return true;
    }

    // Production 82; `<!NOTATION` is consumed.
    private bool ParseNotation()
    {
        if (
            !RequireSpace()
            || !ScanName(out var name)
            || !RequireSpace()
            || !ScanExternalId(true, out var publicId, out var systemId)
            || !ExpectClose()
        )
        {
            return false;
        }

        _builder.AddNotation(new XmlNotation(name, systemId, publicId));
        return true;
    }
}
