namespace NetEvolve.Schemas.Architecture.Fixtures;

/// <summary>Violating fixture: lives in a project that must reference a higher layer when checked as the core assembly.</summary>
public static class HigherLayerReferenceFixture
{
    /// <summary>Gets the name; in the final form it reads from a higher layer.</summary>
    public static string Name => Xml.XmlProbe.Name;
}
