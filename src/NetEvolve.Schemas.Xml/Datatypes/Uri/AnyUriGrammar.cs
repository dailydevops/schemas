namespace NetEvolve.Schemas.Xml.Datatypes.Uri;

/// <summary>How strictly the lexical form of <c>anyURI</c> is checked.</summary>
public enum AnyUriGrammar
{
    /// <summary>Every string of XML <c>Char</c> is accepted; no URI or IRI grammar is applied.</summary>
    Lax = 0,

    /// <summary>The form must match the grammar of the <see cref="AnyUriBasis"/>. Recorded only: no strict checker exists yet (#411, #412).</summary>
    Strict = 1,
}
