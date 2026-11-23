#pragma warning disable IDE0130, MA0048, CS1591
namespace System;

// Classic extension methods (not C# 14 extension blocks): inaccessible blocks of referenced test assemblies would make calls ambiguous.
// Call as c.IsAsciiLetter(); the BCL char.IsAscii* statics exist from net7.0 on.
internal static class CharPolyfills
{
    public static bool IsAsciiLetter(this char c) =>
#if NET7_0_OR_GREATER
        char.IsAsciiLetter(c);
#else
        (uint)((c | 0x20) - 'a') <= 'z' - 'a';
#endif

    public static bool IsAsciiDigit(this char c) =>
#if NET7_0_OR_GREATER
        char.IsAsciiDigit(c);
#else
        c is >= '0' and <= '9';
#endif

    public static bool IsAsciiLetterOrDigit(this char c) => c.IsAsciiLetter() || c.IsAsciiDigit();

    public static bool IsAsciiHexDigitLower(this char c) =>
#if NET7_0_OR_GREATER
        char.IsAsciiHexDigitLower(c);
#else
        c.IsAsciiDigit() || c is >= 'a' and <= 'f';
#endif
}
