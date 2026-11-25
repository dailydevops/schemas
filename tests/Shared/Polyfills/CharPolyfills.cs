#pragma warning disable IDE0130, MA0048, CS1591
namespace System;

// Classic extension methods (not C# 14 extension blocks): inaccessible blocks of referenced test assemblies would make calls ambiguous.
// Call as c.IsAsciiLetter(); forwards to the BCL char.IsAscii* statics.
internal static class CharPolyfills
{
    public static bool IsAsciiLetter(this char c) =>
        char.IsAsciiLetter(c);

    public static bool IsAsciiDigit(this char c) =>
        char.IsAsciiDigit(c);

    public static bool IsAsciiLetterOrDigit(this char c) => c.IsAsciiLetter() || c.IsAsciiDigit();

    public static bool IsAsciiHexDigitLower(this char c) =>
        char.IsAsciiHexDigitLower(c);
}
