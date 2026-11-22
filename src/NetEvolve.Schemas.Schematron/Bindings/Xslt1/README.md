# Xslt1

The default query bindings of Schematron: a schema without `queryBinding` and the names `xslt`, `xslt1`, and `xpath` evaluate their
expressions in XPath 1.0 compatibility mode (ISO/IEC 19757-3:2016, Annex C; XPath 2.0, Appendix I). The names `xpath2`, `xslt2`, and
`xslt3` keep the XPath 2.0 contract (design #137 AP-25).

| File                    | Role                                                                                                                          |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `Registration.cs`       | The registration file: creates the bindings `xpath`, `xslt`, and `xslt1`. `QueryBindingRegistry.Default` adds them to its list. |
| `XPath10QueryBinding.cs`| The binding. `CreateStaticContext` builds the static context of an expression scope with `XPath10Compatibility` turned on.   |

The mode is a flag of the static context of the XPath package (XP-20, #298). Its lowerings read the flag, so the first item of a node
set is used where a single value is expected, a node set compared with a number is compared by `fn:number`, and a single boolean
operand turns the other operand into its effective boolean value.

## Limitations

- **Evaluation is the one of `XPathQueryBinding`** (SCH-55, #311): the binding compiles and runs on the public API of the XPath package with
  the mode on. The stylesheet functions, keys, and decimal formats of an XSLT stylesheet are not wired (#832, #628, #250).
- **`xslt1` is XPath 1.0 compatibility mode, not XSLT 1.0.** The binding runs on the XPath 2.0 engine; the XSLT 1.0 differences to XSLT 2.0
  (for example the first-item rule of `xsl:value-of`) are not applied (design #150).
- **The namespace axis is not supported** in any mode (`XPST0010`, DEF-42, #54).
- **The incompatibilities of Appendix I remain**, as listed in `NetEvolve.Schemas.XPath/Compatibility/README.md`.
- **Rule contexts keep XPath 2.0 semantics.** `Patterns/PatternMatcher` builds its own static context without the mode; it is outside the
  owned paths of this folder.

## Tests

`tests/NetEvolve.Schemas.Schematron.Tests.Unit/Bindings/Xslt1/`: the names resolve to the binding, the static context carries the mode,
the namespaces, the variables, and the base URI, the first-item and number-comparison rules hold under it and fail without it, and the
guards.
