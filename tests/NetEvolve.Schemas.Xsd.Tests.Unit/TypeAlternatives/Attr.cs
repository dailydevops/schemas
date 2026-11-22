namespace NetEvolve.Schemas.Xsd.Tests.Unit.TypeAlternatives;

/// <summary>One attribute of an instance element in a test.</summary>
internal readonly record struct Attr(string Namespace, string Name, string Value, bool Inheritable = false)
{
    public static Attr Of(string name, string value, bool inheritable = false) =>
        new(string.Empty, name, value, inheritable);
}
