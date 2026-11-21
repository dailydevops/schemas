namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Xml;

/// <summary>Resolves local files only, so a schema location with another scheme never reaches the network.</summary>
internal sealed class LocalFileResolver : XmlResolver
{
    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        ArgumentNullException.ThrowIfNull(absoluteUri);
        return absoluteUri.IsFile
            ? File.OpenRead(absoluteUri.LocalPath)
            : throw new NotSupportedException($"Only local files are read, not '{absoluteUri.Scheme}' locations.");
    }
}
