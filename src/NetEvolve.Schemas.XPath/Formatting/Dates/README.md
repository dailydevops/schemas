# Date and time formatting

`fn:format-date`, `fn:format-dateTime`, and `fn:format-time` (Functions and Operators 3.1, 9.8), each in two and in five arguments, for
XPath 3.0 and 3.1 and, with the XSLT 2.0 host flag, for XPath 2.0 (error codes `XTDE1340` and `XTDE1350`). `Registration.cs` stages the
functions into the default catalog; `DatePicture` reads the picture string (literal text, `[[` and `]]`, component specifiers,
presentation modifiers, width modifiers), `DateFormatter` writes the components. Integer presentation (digit patterns of any Unicode
digit family, roman, alphabetic, English words, ordinals) is the component of `fn:format-integer` (`IntegerPicture` and `IntegerFormatter`).
The output does not depend on the culture or the time zone data of the operating system.

Components: `Y M D d F W w H h P m s f Z z C E`. A component that the type does not have is `FOFD1350`; a malformed picture or calendar is
`FOFD1340`. Names of months and days and the AM/PM marker are English tables of this library, abbreviated to three letters by a maximum width.

## Limitations

- English and the Gregorian calendar only (DEF-71, #83 and DEF-72, #84). A requested language other than English starts the result with
  `[Language: en]`, a requested calendar other than `AD`, `CE`, or `ISO` starts it with `[Calendar: AD]`, as the specification prescribes.
- The week of the year, the week of the month, and the day of the week follow the ISO 8601 rules (Monday is the first day) in every calendar.
- The place argument is accepted and not used: no time zone database is read, so a time zone name (`[ZN]`) is the offset as `GMT+hh:mm`.
- The year is written without its sign; use `[E]` for the era (`AD`, `BC`).
- A digit pattern with two or more digit signs and no width modifier limits the year and the fractional seconds to that many digits.
- The minimum width does not pad names, and the maximum width does not shorten non-decimal numbers or time zones.
- The QT3 test sets `fn/format-date`, `fn/format-dateTime`, and `fn/format-time` stay in the baselines of the XPath 3.0 and 3.1 tiers until the
  runner compiles XPath 3.0 queries; cases that need a time zone database or other languages stay excluded.
