namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The built-in datatypes of XSD 1.0 and XSD 1.1, assembled from the registrations listed in <c>BuiltInDatatypes.Sources.cs</c>.</summary>
public static partial class BuiltInDatatypes
{
    private static readonly Lazy<DatatypeRegistry> V10 = new(static () => new(XsdVersion.V10, Collect()));
    private static readonly Lazy<DatatypeRegistry> V11 = new(static () => new(XsdVersion.V11, Collect()));

    /// <summary>Gets the registry of a version; the same instance on every call.</summary>
    /// <param name="version">The XSD version.</param>
    /// <returns>The registry.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined version.</exception>
    public static DatatypeRegistry For(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        return version == XsdVersion.V10 ? V10.Value : V11.Value;
    }

    private static List<DatatypeRegistration> Collect()
    {
        var all = new List<DatatypeRegistration>();
        foreach (var source in Sources())
        {
            all.AddRange(source);
        }

        return all;
    }
}
