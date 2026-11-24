# Regular expression functions

`fn:matches`, `fn:replace`, and `fn:tokenize` of the Functions and Operators, section 7.6 (XPath 2.0 arities), on top of the function
registry (`Functions/Binding`) and the regex operations of the Xml package (`NetEvolve.Schemas.Xml.Regex.Operations`). One
registration file (`Registration.cs`) stages the group into the default catalog; one sealed class per function. Everything in this
folder is internal: the group adds no public API.

| Function         | Arity | Notes                                                                                                |
| ---------------- | ----- | ---------------------------------------------------------------------------------------------------- |
| `fn:matches`     | 2..3  | a pattern that matches the zero-length string is allowed                                             |
| `fn:replace`     | 3..4  | `$N`, `\$`, and `\\` in the replacement string; FORX0003 before FORX0004; FORX0004 also without match |
| `fn:tokenize`    | 2..3  | empty first, last, and adjacent tokens; the empty input is the empty sequence; no one-argument form  |

The flags are `s`, `m`, `i`, and `x`, each any number of times and in any order.

| Error      | Raised when                                                                                |
| ---------- | ------------------------------------------------------------------------------------------ |
| `FORX0001` | a flag letter is not `s`, `m`, `i`, or `x` (also `q`, see the limitations); checked first  |
| `FORX0002` | the pattern is not a regular expression of the XPath dialect                               |
| `FORX0003` | `fn:replace` or `fn:tokenize` with a pattern that matches the zero-length string           |
| `FORX0004` | the replacement string has a lone `$` or a lone backslash                                  |
| `XP0007`   | engine code: the pattern exceeds a limit of the regex engine, or a search reached its step ceiling |

## The match-segment iterator

`MatchSegmentEnumerator` divides an input span at the matches of a pattern into alternating segments in input order: the part before
the first match, each match, the part between two matches, and the part after the last. A match segment carries the bounds of its
capture groups (`TryGetGroup`, `GetGroupText`); a non-matching segment is never empty. It is a `ref struct` over a
`ReadOnlySpan<char>` on top of `RegexMatchEnumerator`, rents one small array per iteration, and allocates nothing per segment.

All three functions read it. `fn:tokenize` reads the tokens from the bounds of the matches, because the empty parts are not segments.
`fn:analyze-string` (XP-64) and `xsl:analyze-string` (SCH-69) reuse it, where the segments are the elements `match` and `non-match`
and the groups are the `group` elements.

## Compiled patterns

`PatternCache` compiles a pattern with its flags once per pattern string instance. The literal pattern of a compiled expression is one
string that every evaluation passes, so it is compiled once per compiled expression; the entry is held weakly by the string, so it
lives as long as the expression and the cache needs no eviction. A pattern that an evaluation computes is a new string and is
compiled again. Each set of flags has its own entry, so `i` and `ii` share one.

## Limitations

- The flag `q` of the later Functions and Operators editions is an invalid flag (FORX0001), as in the Xml package (#100 AP-13, #404).
- The name escapes `\i` and `\c` use the name characters of XML 1.0 Second Edition, the edition of XSD 1.0 that Functions and
  Operators 1.0 cites. XPath 3.x, which cites the Fifth Edition, is not enabled yet.
- The cache cannot tell a literal from a computed string, because a function call has no per-call-site state; a computed pattern
  that is the same string instance, such as a variable, is cached too.
- A step ceiling of the regex engine ends a function with the engine code `XP0007`, although the specification defines an answer;
  so does a pattern whose program exceeds the limits of the engine (counted repeats, capture state).
- A capture group inside a loop keeps the text of the last iteration in which it took part (limitation of the Xml package).
- `fn:replace` builds its result under the string length limit of the evaluation context (XP0006); `fn:tokenize` is bounded by the
  sequence size limit (XP0004).
- `fn:matches` runs the search that records capture groups, which is slower than a search for the extent alone.
- One-argument `fn:tokenize` is XP-71 (#334); `xsl:analyze-string` is SCH-69.
- `fn:analyze-string` is implemented (XP-64, #332; XPath 3.0 gated). Limitation: an empty group at the end of an enclosing group is
  emitted as a sibling (#1022).
