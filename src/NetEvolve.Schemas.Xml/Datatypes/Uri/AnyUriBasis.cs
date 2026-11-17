namespace NetEvolve.Schemas.Xml.Datatypes.Uri;

/// <summary>The specification that defines the lexical space of <c>anyURI</c> in an XSD version.</summary>
public enum AnyUriBasis
{
    /// <summary>XSD 1.0: URI references per RFC 2396 as amended by RFC 2732, after the XLink escaping algorithm.</summary>
    Rfc2396 = 0,

    /// <summary>XSD 1.1: IRI references per RFC 3987.</summary>
    Rfc3987 = 1,
}
