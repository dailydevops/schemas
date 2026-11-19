namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System.Text;

/// <summary>One construct that must survive a cut inside it, in an encoding; the splits fall inside the marker.</summary>
public sealed record Boundary(string Name, string Xml, string Marker, Encoding Encoding, bool Bom = false)
{
    public override string ToString() => Name;
}
