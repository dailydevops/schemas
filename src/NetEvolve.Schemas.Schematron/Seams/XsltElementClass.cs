namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>The answer of one registry lookup for an element (design #150 EP-9).</summary>
internal enum XsltElementClass : byte
{
    /// <summary>An XSLT element that is registered for the requested position.</summary>
    Registered = 0,

    /// <summary>An XSLT element that is registered, but not for the requested position.</summary>
    Misplaced = 1,

    /// <summary>An XSLT element that is not registered.</summary>
    Unregistered = 2,

    /// <summary>An element outside the XSLT namespace, such as a literal result element.</summary>
    NotXslt = 3,
}
