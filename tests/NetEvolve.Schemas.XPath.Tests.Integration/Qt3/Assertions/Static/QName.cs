namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

using System.Runtime.InteropServices;

/// <summary>An expanded qualified name; the empty namespace means no namespace.</summary>
/// <param name="Namespace">The namespace URI.</param>
/// <param name="LocalName">The local name.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct QName(string Namespace, string LocalName)
{
    /// <summary>The namespace of the error codes of the XPath and XQuery specifications.</summary>
    public const string ErrorNamespace = "http://www.w3.org/2005/xqt-errors";

    /// <summary>Parses the code of an expected QT3 error: an NCName (error namespace) or <c>Q{uri}local</c>.</summary>
    /// <param name="code">The code; <c>*</c> is handled by the caller.</param>
    /// <returns>The expanded name.</returns>
    public static QName ParseErrorCode(string code)
    {
        if (code.StartsWith("Q{", StringComparison.Ordinal))
        {
            var length = code.AsSpan(2).IndexOf('}');
            if (length >= 0)
            {
                return new QName(code.Substring(2, length), code[(length + 3)..]);
            }
        }
        return new QName(ErrorNamespace, code);
    }
}
