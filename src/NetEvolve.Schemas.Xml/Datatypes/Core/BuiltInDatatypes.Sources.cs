namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System.Collections.Generic;

public static partial class BuiltInDatatypes
{
    // One line per type package: each package exposes its registrations from its own static class. This is the only file of
    // the datatype core that the type packages edit (recorded exception to path ownership, issue #161).
    private static IReadOnlyList<DatatypeRegistration>[] Sources() => [SpecialDatatypes.Registrations];
}
