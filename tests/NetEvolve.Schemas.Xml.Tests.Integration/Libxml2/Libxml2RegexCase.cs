namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

/// <summary>One expectation of a libxml2 regexp file: a pattern that must or must not compile, or an input that must or must not match.</summary>
/// <param name="Id">The case id, <c>file/pattern</c> or <c>file/pattern/input</c> with one-based indexes.</param>
/// <param name="TestSet">The file name below <c>test/regexp</c>.</param>
/// <param name="Pattern">The pattern.</param>
/// <param name="Input">The input, or <see langword="null"/> for a compile expectation.</param>
/// <param name="Expected"><see langword="true"/> when the pattern must compile, or the input must match.</param>
internal sealed record Libxml2RegexCase(string Id, string TestSet, string Pattern, string? Input, bool Expected);
