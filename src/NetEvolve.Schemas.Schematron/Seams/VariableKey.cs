namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The name of a variable slot in <see cref="QueryStaticScope.VariableNames"/>: the local name when the variable has no namespace,
/// and <c>{namespace}local</c> when its prefixed name resolved to one, so that a reference compares by expanded QName.
/// </summary>
internal static class VariableKey
{
    /// <summary>Builds the key of an expanded name.</summary>
    /// <param name="namespaceName">The namespace name, empty for none.</param>
    /// <param name="local">The local name.</param>
    /// <returns>The key.</returns>
    internal static string Of(string namespaceName, string local) =>
        namespaceName.Length == 0 ? local : "{" + namespaceName + "}" + local;

    /// <summary>Reads a key back into the expanded name the XPath static context uses.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The expanded name.</returns>
    internal static XPathQName ToQName(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var end = key.AsSpan().LastIndexOf('}');
        return key.Length > 0 && key[0] == '{' && end > 0
            ? new XPathQName(key.Substring(1, end - 1), key.Substring(end + 1))
            : new XPathQName(string.Empty, key);
    }
}
