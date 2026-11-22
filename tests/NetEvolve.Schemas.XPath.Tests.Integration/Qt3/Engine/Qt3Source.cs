namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

/// <summary>A <c>source</c> element: a document bound to the context item (<c>.</c>) or to a variable (<c>$name</c>).</summary>
/// <param name="Role">The role, <c>.</c> or <c>$name</c>.</param>
/// <param name="File">The absolute path of the document.</param>
/// <param name="Uri">The <c>uri</c> attribute, the document URI the tests expect.</param>
internal sealed record Qt3Source(string Role, string File, string? Uri);
