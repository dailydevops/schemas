# Integer formatting

`fn:format-integer` (Functions and Operators 3.1, 4.6), in two and three arguments, for XPath 3.0 and 3.1. `Registration.cs` stages the
function into the default catalog; `IntegerPicture` reads the picture string (reusable by the date and time formatting) and
`IntegerFormatter` writes the number. Values of any width are supported. The output does not depend on the culture of the operating system.

Sequences: decimal digit patterns (mandatory digits of any Unicode digit family, optional `#`, grouping separators regular or irregular),
`a` and `A`, `i` and `I`, `w`, `W`, and `Ww` (English). The modifier `o` gives the English ordinal for decimal digits and words.
An invalid picture is `FODF1310`; the empty sequence gives the zero-length string.

## Limitations

- Words and ordinals are English only: every requested language falls back to English, which the specification permits (DEF-70, #82).
- Any other numbering sequence token falls back to `1`, which the specification permits.
- The variation in parentheses of the modifier is checked and ignored; `a` and `t` (alphabetic, traditional) have no effect.
- A value a sequence cannot show is written as decimal digits: zero as a letter or roman numeral, a roman numeral above 3999, and words
  above the decillions.
- Alphabetic and roman sequences have no ordinal form in English, so `o` has no effect on them.
- The QT3 test set `fn/format-integer` stays in the baseline of the XPath 3.0 tier until the runner compiles XPath 3.0 queries.
