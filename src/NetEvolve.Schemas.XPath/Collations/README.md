# Collations

The collation registry of the XPath 2.0 engine (Functions and Operators 1.0, section 7.3, Equality and Comparison of Strings), and
the two functions that compare strings: `fn:compare` (two and three arguments) and `fn:codepoint-equal`. One registration file
(`Registration.cs`) stages the group into the default function catalog.

## Only the codepoint collation ships

The Unicode codepoint collation, `http://www.w3.org/2005/xpath-functions/collation/codepoint`, is the only built-in collation. It
orders strings by Unicode code point, not by UTF-16 code unit: a supplementary character follows U+FFFD, which a comparison of the
code units gets the other way round. No operating system culture data is used, so the result is the same on every target framework
(`netstandard2.0`, `net10.0`, `net11.0`), and a comparison allocates nothing.

Every other collation URI is the error `FOCH0002`, never a silent fallback. This covers an unknown absolute URI, the Unicode
Collation Algorithm collations (DEF-43, #55), and the HTML ASCII case-insensitive collation (DEF-76, #88).

| Type                | Role                                                                                                       |
| ------------------- | ---------------------------------------------------------------------------------------------------------- |
| `Collation`         | A collation: `Compare`, `AreEqual`, and the substring matching `StartsWith`, `EndsWith`, and `TryFind`.    |
| `CodepointCollation`| The one built-in collation; a match never starts or ends inside a surrogate pair.                          |
| `CollationRegistry` | Maps a collation URI to a collation; immutable, `With` returns a new registry; `Resolve` raises `FOCH0002`. |
| `CompareFunction`, `CodepointEqualFunction`, `DefaultCollationFunction` | `fn:compare`, `fn:codepoint-equal`, and `fn:default-collation`. |

## Resolution of a URI

An absolute URI (it starts with a scheme) is looked up as written. A relative URI is resolved against the static base URI first, and
then looked up. A relative URI without an absolute static base URI finds no collation. `fn:codepoint-equal` always uses the codepoint
collation and takes no collation argument.

## Registering a collation of the host

`CollationRegistry.Default.With(collation)` returns a registry that also knows the collation, found by its absolute URI; a collation
registered under the URI of another one replaces it. A function that takes a collation argument is built over a registry:
`new CompareFunction(registry, staticBaseUri)` is a function of the `fn` namespace that a host registers in its own layer, which
overrides the built-in one (the layering of the function registry).

## Limitations

- Only the codepoint collation ships; other collations raise `FOCH0002`.
- A function call reaches the evaluation context only, which carries neither the static base URI nor the host collations. The
  `fn:compare` of the default catalog therefore uses the built-in registry and no static base URI, so a relative collation URI is
  `FOCH0002` there. The static context reaches a call with the work on the static and dynamic contexts (XP-08, #187) and the public
  API (#297); the follow-up is #752.
- The registry is internal: the public API is frozen (#168, #297), so a registration point for callers outside this assembly and the
  packages with `InternalsVisibleTo` is not exposed yet.
- The functions with a collation argument of the string and sequence groups (`fn:contains`, `fn:starts-with`, `fn:ends-with`,
  `fn:substring-before`, `fn:substring-after`, `fn:index-of`, `fn:distinct-values`, `fn:deep-equal`, `fn:min`, `fn:max`) still check
  the argument for the codepoint URI only (`CollationArgument`); they do not consult the registry yet (#752).
- The default collation of the static context is XP-08 (#187); without a collation argument the codepoint collation applies.
- An unpaired surrogate counts as the code point of its own code unit; a valid `xs:string` holds none.
