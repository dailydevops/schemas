namespace NetEvolve.Schemas.Xml.Datatypes.Uri;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The registration of the built-in <c>anyURI</c> type.</summary>
internal static class UriDatatypes
{
    internal static IReadOnlyList<DatatypeRegistration> Registrations { get; } =
    [new("anyURI", XsdVersion.V10, static r => new AnyUriDescriptor(r.Version))];
}
