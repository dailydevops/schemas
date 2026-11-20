# Number formatting

The decimal format model and the picture engine behind `format-number`: Functions and Operators 3.1 (4.7 Formatting numbers) and XSLT 2.0
(16.4 Number Formatting). One registration file (`Registration.cs`) stages the function into the default catalog; the Schematron XSLT stage 1
uses it, and the same engine serves XPath 3.0 and later.

## Two rule sets, two functions

`format-number` is not a function of XPath 2.0. The catalog holds two functions of the same name, with disjoint version masks, as `generate-id`
does:

| Function                 | Visible in                                 | Rule set          | Error codes         | Third argument |
| ------------------------ | ------------------------------------------ | ----------------- | ------------------- | -------------- |
| `FormatNumberFunction.ForXslt20` | XPath 2.0 with the XSLT 2.0 host flag (`XPathHostRequirements.Xslt20Host`) | XSLT 2.0 (16.4)   | `XTDE1280`, `XTDE1310` | `xs:string`    |
| `FormatNumberFunction.ForXPath3` | XPath 3.0 and 3.1, no flag                | F&O 3.1 (4.7)     | `FODF1280`, `FODF1310` | `xs:string?`   |

In plain XPath 2.0 without the flag the call is not bound, which the compiler reports as `XPST0017`.

| Rule                                   | XSLT 2.0 rules                                           | 3.1 rules                                                              |
| -------------------------------------- | -------------------------------------------------------- | ---------------------------------------------------------------------- |
| Digit signs of a picture               | only the zero digit, and the optional digit              | every digit of the family (`999` is `000`), and the optional digit    |
| Exponent separator                     | none: `e` is passive, rejected between two active characters (`0.0e0`), a literal at the edge (`0.0e`) | between two active characters it starts the exponent: `00.000e0` gives `12.346e2` |
| Regular grouping                       | positions N, 2N, 3N, and so on; one separator counts     | every multiple of the size up to the integer digits has a separator    |
| Grouping separator at the end / adjacent | allowed                                                | `FODF1310`                                                             |
| Minimum sizes                          | integer size 1 only when there is no zero digit and no decimal separator | adjusted so that a picture without a mandatory digit still shows one (`#.#` gives `.0` for zero) |

## Decimal format

`DecimalFormatModel` is the immutable, validated snapshot of an `XPathDecimalFormat`: decimal separator, grouping separator, exponent
separator, minus sign, percent, per-mille, zero digit, digit, pattern separator, infinity, and NaN. The characters that a picture reads have
to differ (and, under the 3.1 rules, differ from every digit of the family); the zero digit has to be a character of category Nd with the
digit value zero, checked against the Unicode version pinned by the Xml package.

The decimal formats come from the host slot `XPathEvaluationContext.Host.DecimalFormats` (#123, AP11):

- the two-argument form and an empty name (XPath 3) use the unnamed format: the provider is asked for `default(XPathQName)`, and the default
  format applies when the slot is empty or has none;
- a named format needs the slot (`XPDY0002` when it is empty) and has to be known to it (`FODF1280`, `XTDE1280`);
- an invalid format (clashing characters, a zero digit that is no zero) is reported as `FODF1310` or `XTDE1310` when a call uses it.

## Values

A float or double is formatted from the shortest decimal that round-trips, not from its exact binary value; a decimal or integer is exact at
any width. The value is rounded to the maximum number of fraction digits, a tie to the even neighbor. A percent or per-mille sign multiplies
a decimal exactly and a double or float in its own type, so an overflow is infinity. NaN and the empty sequence give the NaN string alone,
an infinity gives prefix, infinity string, and suffix of the sub-picture of its sign, and negative zero of a double or float takes the
negative sub-picture. The result is built in a `TextBuilder` (stack scratch space, then a pooled array); besides the result string, a call
allocates the digit string of the value and the analysis of the picture.

## Limitations

- The characters of a decimal format are UTF-16 code units, as in `XPathDecimalFormat`; a zero digit outside the Basic Multilingual Plane
  cannot be named.
- A prefixed decimal format name needs the statically known namespaces, which a call does not reach (#752). The function resolves a prefix
  with the namespace context it was built with (`ForXPath3(namespaces)`), and the one in the default catalog has none, so a prefixed name is
  `FODF1280` there. An unprefixed name and `Q{uri}local` (XPath 3) work.
- XPath 3.0 uses the 3.1 rules; there is no third rule set.
- NaN is the NaN property of the decimal format. The text of 4.7.5 names the pattern separator property, an editorial slip, because the same
  step reads the NaN symbol in XSLT 2.0.
- A mantissa that rounds up to the next power of ten is scaled again (`9.96` with `0.0e0` gives `1.0e1`); the text of 4.7.5 does not say.
- A picture with an exponent separator and no decimal separator and no mandatory digit in the mantissa (`#e0`) gets the adjusted fraction
  size of 4.7.4, and the decimal separator is written before the fraction digit, although the picture names none.
- Reading `xsl:decimal-format` declarations and wiring them into the slot is SCH-83; `fn:format-integer` is XP-35; date and time
  formatting is XP-63; the QT3 set `fn/format-number` joins the selection with XP-74.
