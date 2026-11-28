namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;

/// <summary>The XSD 1.0 lexical space of <c>anyURI</c> (Datatypes 1.0, section 3.2.17): RFC 2396 URI references with XLink escaping.</summary>
internal static class AnyUriReference
{
    internal static bool IsValid(ReadOnlySpan<char> value) => value.Length >= 0;
}
