# Document, text, and ID functions

The functions that reach documents and text resources (`fn:doc`, `fn:doc-available`, `fn:collection`, `fn:unparsed-text`,
`fn:unparsed-text-available`, `fn:unparsed-text-lines`) and the ID functions (`fn:id`, `fn:idref`) on top of the function registry
(`Functions/Binding`). One registration file (`Registration.cs`) stages the group into the default catalog. **Every access goes through the
resolver of the evaluation context, which denies by default**: a context without a resolver, and `DenyAllResourceResolver`, refuse every
URI, so nothing is read from a file system or a network.

| Function                      | Arity | Versions                              | Result type                | Notes                                                                                    |
| ----------------------------- | ----- | ------------------------------------- | -------------------------- | ---------------------------------------------------------------------------------------- |
| `fn:doc`                      | 1     | 2.0, 3.0, 3.1                         | `document-node()?`         | pooled per evaluation context; FODC0002, FODC0005                                        |
| `fn:doc-available`            | 1     | 2.0, 3.0, 3.1                         | `xs:boolean`               | `false` for every condition of FODC0002; FODC0005 for a reference that is no URI         |
| `fn:collection`               | 0..1  | 2.0, 3.0, 3.1                         | `node()*`                  | FODC0002 for every call (DEF-49, #61)                                                    |
| `fn:unparsed-text`            | 1..2  | 3.0, 3.1; 2.0 with the XSLT 2.0 host  | `xs:string?`               | FOUT1170, FOUT1190, FOUT1200                                                             |
| `fn:unparsed-text-available`  | 1..2  | 3.0, 3.1; 2.0 with the XSLT 2.0 host  | `xs:boolean`               | never raises an error                                                                    |
| `fn:unparsed-text-lines`      | 1..2  | 3.0, 3.1                              | `xs:string*`               | a line ends at LF, CR, or CR LF only                                                     |
| `fn:id`                       | 1..2  | 2.0, 3.0, 3.1                         | `element()*`               | attributes of DTD type ID; document order, no duplicates, the first element of a duplicate |
| `fn:idref`                    | 1..2  | 2.0, 3.0, 3.1                         | `node()*`                  | attributes of DTD type IDREF and IDREFS                                                  |

- A reference is resolved against the static base URI by RFC 3986, 5.2 (`UriReference` of the core), and the resolver gets a
  `ResourceRequest` of kind `XPathDocument` or `XPathText` with the reference as written, the base URI, and the absolute URI. A reference
  that is relative against no absolute base never reaches the resolver.
- One pool per evaluation context: the same absolute URI yields the identical document node (`is` is true), and the resolver is asked once.
  The decoded text of the text functions is pooled by URI and encoding, so the available function and the function itself agree. A failed
  load is pooled too. All requests of a context count against `ValidationLimits.MaxExternalResources`.
- The text of a resource is decoded strictly (no replacement characters) and must consist of XML characters. Without an encoding a byte order
  mark of UTF-16 decides, and UTF-8 is the default; the mark is dropped.
- `fn:id` and `fn:idref` read the tree once into an index that is kept with the tree.

## Limitations

- A call reaches the evaluation context only, which carries no static context, so the functions hold the static base URI and the XML reader
  options themselves, as `fn:static-base-uri` does (#752, #806). The functions of the default catalog hold neither: a relative reference
  is unresolvable (FODC0002 and FOUT1170), and a document is loaded with the secure reader defaults, so a document type declaration needs a
  function built over `XmlReaderOptions.Default.WithDtdProcessing(true)`. Binding the functions to the schema base URI is SCH-63.
- The pool lives beside the evaluation context (the context is outside the work package), as long as the context, and survives `Reset`. The
  size of a resource and the number of requests follow `ValidationLimits.Default`, because the evaluation limits carry neither.
- A fragment identifier and a character that no URI may contain make a reference invalid (FODC0005, and FOUT1170 for the text functions); the
  fragment is not resolved against the document.
- `fn:collection` and `fn:uri-collection` are DEF-49 (#61): the call raises FODC0002 also where the specification allows FODC0004.
- The text functions know the encodings `UTF-8`, `UTF-16`, `UTF-16LE`, `UTF-16BE`, `ISO-8859-1`, and `US-ASCII` (any case); every other
  name is FOUT1190. `UTF-16` without a byte order mark is big endian. No encoding is inferred from a media type. FOUT1200 is raised for a
  character that is no XML character only; bytes that the encoding does not allow are FOUT1190.
- IDs are the attributes that the attribute declarations of the document type declaration type as ID, IDREF, or IDREFS. ID typing through
  `xml:id` is DEF-67 (#79); no element is an ID or IDREF through a schema type. A value is matched as written, so a DTD ID with a colon
  matches although the data model wants an `xs:NCName`. FODC0001 is never raised, because every tree is rooted in a document node.
  `fn:element-with-id` is DEF-79 (#91).
- The XSLT `document()` function is SCH-63; the HTTP resolver is DEF-53 (#65); `fn:json-doc` is DEF-44 (#56).
