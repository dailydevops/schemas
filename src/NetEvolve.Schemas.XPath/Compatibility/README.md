# Compatibility

XPath 1.0 compatibility mode on top of the XPath 2.0 engine (XPath 2.0, Appendix I, and the rules of 3.1.5, 3.4, and 3.5.2 for
`XPathStaticContext.XPath10Compatibility`). The flag lives in the static context; the lowerings read it while an expression is
compiled and create the nodes of this folder instead of the nodes of the mode without it. The default Schematron query bindings
`xslt` and `xpath` depend on this mode (SCH-16).

| Type                                | Rule                                                                                                                                                     |
| ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CompatibilityArithmeticNode`       | 3.4: an empty operand gives NaN, the first item is used, and a boolean, string, decimal, float, or untyped operand becomes a double by `fn:number`.      |
| `CompatibilityUnaryNode`            | The same operand rules for unary `+` and `-`.                                                                                                            |
| `CompatibilityGeneralComparisonNode`| 3.5.2: a single boolean operand turns the other operand into its effective boolean value; the relational operators and any pair with a number use `fn:number`. |
| `CompatibilityFunctionConversion`   | 3.1.5: the first item for a single-item parameter, `fn:string` for `xs:string`, `fn:number` for `xs:double`, applied before the function conversion rules. |
| `CompatibilityCallNode`             | A function call that applies `CompatibilityFunctionConversion` to each argument, then `XPathFunction.Call`.                                              |
| `CompatibilityOperands`             | The shared conversion by `fn:number` (the cast of `NumberFunction`).                                                                                     |

## Limitations

- **The namespace axis** is required by XPath 1.0 and by 3.2.1.1 in compatibility mode, but it is not supported: the step is the static
  error `XPST0010` in every mode (DEF-42, #54; approved in #123).
- **The incompatibilities of Appendix I.1 remain**, because the mode follows XPath 2.0: a number with a leading `+`, in exponential
  notation, or written `INF` is a number, and `Infinity` is NaN; a number is converted to a string by the XPath 2.0 rules (`INF`, and scientific
  notation outside 1E-6 to 1E+6); a boolean compared with a number by `<` or `>` is not compared as two numbers
  (`true() > number('0.5')` is false). The differential tests list each of these with its reason.
- **Numeric parameters other than `xs:double` keep the XPath 2.0 rules** (3.1.5 names `xs:string` and `xs:double` only): `fn:floor('3.5')` is
  `XPTY0004`, an untyped value that is not a number is `FORG0001`, and the empty sequence gives the empty sequence. The XPath 1.0 results
  are not restored.
- **A relational comparison runs `fn:number` once per pair**, not once per item, so two long sequences repeat the conversion; converting each
  item once is the upgrade path.
- **The lowering of a function call is not part of this package.** It belongs to the public API (XP-19, #297), which must create
  `CompatibilityCallNode` for a resolved `XPathFunction` when `XPath10Compatibility` is on. Until then the tests assemble the call lowering
  themselves.
- **`id()` is not part of the library yet** (#302), so the corpus of the differential tests does not use it.

## Tests

- `tests/NetEvolve.Schemas.XPath.Tests.Unit/Compatibility/`: one group per rule of Appendix I (first item, number conversion in arithmetic,
  general comparisons, function conversion), each test asserting the result with the mode and the differing result without it; the namespace
  axis; the guards.
- `tests/NetEvolve.Schemas.XPath.Tests.Integration/Compatibility/`: a fixed list of XPath 1.0 expressions over fixed documents. On every leg the
  results are asserted against the recorded results of `System.Xml.XPath` (`CompatibilityExpectations`, with the baseline runtime named in its
  header); on the designated leg (`net10.0`) the library is also compared with the live baseline. `ListedDifferences` holds every difference
  that follows from the specification, each with its reason; an unlisted difference fails, and so does a listed one that has gone. To record
  the results again after a change of the corpus, set `SCHEMAS_XPATH_COMPAT_RECORD` to a file path and run `CompatibilityBaselineTests.Record`.
