namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The XSD version tokens of the suite that this runner selects by.</summary>
internal static class XstsVersions
{
    public const string V10 = "1.0";
    public const string V11 = "1.1";

    public static bool IsVersion(string token) =>
        string.Equals(token, V10, StringComparison.Ordinal) || string.Equals(token, V11, StringComparison.Ordinal);
}
