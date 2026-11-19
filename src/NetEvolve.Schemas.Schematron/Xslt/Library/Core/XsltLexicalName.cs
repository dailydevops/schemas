namespace NetEvolve.Schemas.Schematron.Xslt.Library.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// Expands the lexical QName argument of <c>system-property</c> and <c>function-available</c> against the namespaces of the static
/// context (XSLT 2.0, 5.1), and builds the dynamic error of a name that does not expand. The names are checked as the QName datatype
/// of the Xml package checks them, so a leading or trailing space makes a name invalid.
/// </summary>
internal static class XsltLexicalName
{
    /// <summary>Expands a lexical QName.</summary>
    /// <param name="statics">The static context whose namespace declarations are in scope.</param>
    /// <param name="lexical">The lexical QName.</param>
    /// <param name="unprefixedNamespace">The namespace of an unprefixed name, <see langword="null"/> for no namespace.</param>
    /// <param name="name">The expanded name when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the text is not a QName or its prefix is not bound.</returns>
    internal static bool TryExpand(
        XPathStaticContext statics,
        string lexical,
        string? unprefixedNamespace,
        out XPathQName name
    )
    {
        Argument.ThrowIfNull(statics);
        Argument.ThrowIfNull(lexical);

        var parsed = XsdQName.Parse(lexical.AsSpan(), new Namespaces(statics, unprefixedNamespace));
        name = parsed.Success
            ? new XPathQName(parsed.Value.NamespaceName ?? string.Empty, parsed.Value.LocalName)
            : default;
        return parsed.Success;
    }

    /// <summary>Reports a name that does not expand as a non-recoverable dynamic error of the call.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="code">The XSLT error code: XTDE1390 for <c>system-property</c>, XTDE1400 for <c>function-available</c>.</param>
    /// <param name="lexical">The text of the argument.</param>
    /// <returns><see cref="XPathEvaluationStatus.Error"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="code"/> or <paramref name="lexical"/> is <see langword="null"/>.</exception>
    internal static XPathEvaluationStatus Reject(ref XPathFunctionCall invocation, string code, string lexical)
    {
        Argument.ThrowIfNull(code);
        Argument.ThrowIfNull(lexical);

        invocation.Diagnostic = new XPathDiagnostic(
            new XPathErrorCode(code),
            XPathErrorKind.Dynamic,
            $"The value '{lexical}' is not a lexical QName, or no namespace declaration is in scope for its prefix.",
            -1,
            0
        );
        return XPathEvaluationStatus.Error;
    }

    private sealed class Namespaces : INamespaceContext
    {
        private readonly XPathStaticContext _statics;
        private readonly string? _unprefixed;

        internal Namespaces(XPathStaticContext statics, string? unprefixed)
        {
            _statics = statics;
            _unprefixed = unprefixed;
        }

        public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
        {
            if (prefix.IsEmpty)
            {
                namespaceName = _unprefixed;
                return namespaceName is not null;
            }

            return _statics.TryResolveNamespace(prefix.ToString(), out namespaceName);
        }
    }
}
