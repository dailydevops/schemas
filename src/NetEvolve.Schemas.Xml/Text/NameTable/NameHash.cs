namespace NetEvolve.Schemas.Xml.Text.NameTable;

using System;

/// <summary>Computes the hash of a name; the production table uses FNV-1a, tests inject a weak hash to force collisions.</summary>
/// <param name="text">The characters of the name.</param>
/// <returns>The hash.</returns>
internal delegate int NameHash(ReadOnlySpan<char> text);
