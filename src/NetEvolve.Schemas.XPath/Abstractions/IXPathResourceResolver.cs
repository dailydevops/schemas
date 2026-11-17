namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.Resources;

/// <summary>
/// The resolver for the external resources of <c>fn:doc</c>, <c>fn:collection</c>, and <c>fn:unparsed-text</c>: the core
/// resolver, marked for XPath. It adds no member, so every core resolver can serve XPath through one adapter. No resolver
/// denies access (FODC0002); the library ships no file system or network resolver for XPath.
/// </summary>
public interface IXPathResourceResolver : IResourceResolver;
