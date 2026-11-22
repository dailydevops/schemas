#pragma warning disable CA1716 // Folder-mandated namespace (NE0002) collides with the VB keyword
namespace NetEvolve.Schemas.Benchmarks.Shared;

using System.Text;

/// <summary>Deterministic input data shared by all benchmark classes.</summary>
internal static class InputData
{
    /// <summary>A flat XML document with <paramref name="elementCount"/> child elements.</summary>
    public static string Document(int elementCount)
    {
        var sb = new StringBuilder("<root>");
        for (var i = 0; i < elementCount; i++)
        {
            _ = sb.Append("<item id=\"").Append(i).Append("\">value").Append(i).Append("</item>");
        }
        return sb.Append("</root>").ToString();
    }
}
