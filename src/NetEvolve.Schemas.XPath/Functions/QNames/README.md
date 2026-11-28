# QName and URI functions

The QName functions (section 11 of the XPath 2.0 Functions and Operators), the URI functions (`fn:resolve-uri`, `fn:encode-for-uri`,
`fn:iri-to-uri`, `fn:escape-html-uri`), and the base URI functions (`fn:base-uri`, `fn:static-base-uri`) on top of the function registry
(`Functions/Binding`). One registration file (`Registration.cs`) stages the group into the default catalog; one sealed class per function.

| Function                       | Arity | Result type                         | Notes                                                                                        |
| ------------------------------ | ----- | ----------------------------------- | -------------------------------------------------------------------------------------------- |
| `fn:resolve-QName`             | 2     | `xs:QName?`                         | the prefix is resolved in the in-scope namespaces of the element; FONS0004, FOCA0002         |
| `fn:QName`                     | 2     | `xs:QName`                          | an empty namespace and `()` both mean no namespace; a prefix needs one (FOCA0002)            |
| `fn:prefix-from-QName`         | 1     | `xs:NCName?`                        | empty for a name without a prefix                                                            |
| `fn:local-name-from-QName`     | 1     | `xs:NCName?`                        |                                                                                              |
| `fn:namespace-uri-from-QName`  | 1     | `xs:anyURI?`                        | zero-length for a name in no namespace                                                       |
| `fn:namespace-uri-for-prefix`  | 2     | `xs:anyURI?`                        | an empty prefix or `()` asks for the default namespace; an unbound prefix gives `()`         |
| `fn:in-scope-prefixes`         | 1     | `xs:string*`                        | the default namespace is the zero-length string; `xml` is always in scope, listed once       |
| `fn:resolve-uri`               | 1..2  | `xs:anyURI?`                        | RFC 3986, 5.2; FORG0002, FONS0005                                                            |
| `fn:base-uri`                  | 0..1  | `xs:anyURI?`                        | the original string of the base URI of the node; no argument: the context item               |
| `fn:static-base-uri`           | 0     | `xs:anyURI?`                        | the static base URI the function holds; the default catalog holds none                        |
| `fn:encode-for-uri`            | 1     | `xs:string`                         | everything but letters, digits, `-`, `_`, `.`, `~` as upper-case `%XX` of the UTF-8 octets   |
| `fn:iri-to-uri`                | 1     | `xs:string`                         | only control characters, space, `<>"{}\|\^` and the backquote, and non-ASCII characters      |
| `fn:escape-html-uri`           | 1     | `xs:string`                         | everything beyond printable ASCII (U+0020 to U+007E) as upper-case `%XX` of the UTF-8 octets |

- `fn:encode-for-uri`, `fn:iri-to-uri`, and `fn:escape-html-uri` return the argument itself, not a copy, when nothing needs escaping; otherwise the result is
  built in a stack buffer that grows into a pooled array, so only the result string is allocated.
- The in-scope namespaces of an element are read through its namespace nodes (`XmlNode.FirstNamespace`); the namespace axis of
  the language stays out (DEF-42, #54).
- `fn:resolve-uri` reuses the reference resolution of `NetEvolve.Schemas` (`UriReference`), which does not use `System.Uri`, so the
  result is the same on every target framework.

## Limitations

- The function conversion has no `xs:QName` and no `element()` item type (the function conversion waits for sequence types as parameter types, #613): a
  QName parameter is declared `xs:anyAtomicType?` and an element parameter `node()`, and the function raises XPTY0004 for an atomic
  value that is no QName and for a node that is no element.
- A call reaches the evaluation context only, which carries no static context, so `fn:static-base-uri` and the one-argument form of
  `fn:resolve-uri` use a static base URI that the function holds, as `fn:compare` does (#806, #752). The functions of the default catalog
  hold none: `fn:static-base-uri()` is the empty sequence, and `fn:resolve-uri` with one relative reference is FONS0005. A host registers
  functions built over its own `Uri`; a relative `Uri` counts as none.
- `fn:resolve-uri` refuses (FORG0002) a character that no URI may contain, because the XML escaping of the resolution would change the
  string the caller wrote; a character beyond ASCII is escaped as UTF-8 octets, as `fn:iri-to-uri` does. A base with a fragment or with
  such a character is FORG0002 first, also for an absolute reference, which is then returned as it is; a base that is no absolute URI
  matters for a relative reference only.
- The lexical QName of `fn:resolve-QName` and `fn:QName` is whitespace-collapsed as the cast to `xs:QName` does.
- A lone surrogate in the argument of `fn:encode-for-uri`, `fn:iri-to-uri`, or `fn:escape-html-uri` is escaped as U+FFFD (`%EF%BF%BD`).
- `fn:namespace-uri-for-prefix`, `fn:in-scope-prefixes`, and `fn:resolve-QName` answer from the namespace nodes of the tree; an element of
  a tree without namespace nodes still has the `xml` binding.
- The work package owns `Functions/QNames` only, so the URI functions live in this folder; the design names a `Functions/Uri` group for them.
- Casting to `xs:QName` and `xs:anyURI` is XP-59, the retrieval of the resource behind a URI is XP-60, and `eq` on `xs:QName` awaits the
  QName value type (#545); a QT3 case that compares two QNames is excluded in `Qt3QNameSelection` with that reason.
