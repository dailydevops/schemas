# Boolean and numeric functions

The boolean and numeric functions of the XPath 2.0 Functions and Operators (6.4 Functions on Numeric Values, section 9 Functions
and Operators on Boolean Values, and `fn:number` of section 2) on top of the function registry (`Functions/Binding`). One
registration file (`Registration.cs`) stages the group into the default catalog; one sealed class per function.

| Function                                      | Arity | Result type                                    | Notes                                                                                 |
| --------------------------------------------- | ----- | ---------------------------------------------- | ------------------------------------------------------------------------------------- |
| `fn:abs`, `fn:ceiling`, `fn:floor`            | 1     | the type of the argument                       | an `xs:untypedAtomic` value is read as `xs:double`; the empty sequence stays empty    |
| `fn:round`                                    | 1     | the type of the argument                       | a tie goes toward positive infinity: 2.5 gives 3, -2.5 gives -2                       |
| `fn:round-half-to-even`                       | 1..2  | the type of the argument                       | a tie goes to the even neighbor; the precision is the number of fraction digits kept  |
| `fn:number`                                   | 0..1  | `xs:double`                                    | NaN for the empty sequence and for a value that cannot be converted; no argument: `.` |
| `fn:true`, `fn:false`                          | 0     | `xs:boolean`                                   |                                                                                       |
| `fn:boolean`, `fn:not`                        | 1     | `xs:boolean`                                   | wrappers over the effective boolean value of `XPathAtomization`                       |

- An `xs:integer` and an `xs:decimal` are exact at any width: an integer beyond 64 bits stays an `xs:integer` (an item of the decimal
  family that carries the `xs:integer` type) through `fn:abs` and `fn:round-half-to-even` with a negative precision.
- A floating-point zero keeps the sign of the argument: `fn:round(-0.3e0)`, `fn:ceiling(-0.3e0)`, and `fn:floor(-0e0)` give negative
  zero, and `fn:abs(-0e0)` gives positive zero. NaN and the infinities are their own result.
- `fn:round` rounds a double by its exact fraction (the floor of the value, then the tie test), not by `Math.Floor(value + 0.5)`,
  which is wrong for 0.49999999999999994. A float is rounded as a double and narrowed again, which is exact.
- `fn:round-half-to-even` with a float or a double casts to `xs:decimal` through the exact binary value, rounds, and casts back, as
  the specification states: 0.125 rounded to two digits is 0.12, and the float nearest to -0.05 rounded to one digit is -0.1.
- The functions allocate nothing for the 64-bit integer, float, and double paths, nor for an argument that the
  function leaves as it is; a decimal that changes and an integer beyond 64 bits allocate, as the data model documents.

## Limitations

- `fn:round` with a precision is a function of Functions and Operators 3.0 and is not registered in any language version; XPath 2.0
  has `fn:round-half-to-even` only. The function is tracked in #727, labeled `state:onhold` and `state:help wanted`.
- `fn:number` reports a lexical form beyond the digit cap of the datatype layer (4,096 digits) as FOCA0001, as the function
  conversion does (#621, #660), because it is an implementation limit and not an invalid form; the specification gives a number
  or an infinity.
- A precision of an `xs:integer` beyond 64 bits is saturated to the 64-bit range by `fn:round-half-to-even`, which keeps every digit for a
  positive precision and clears every digit for a negative one (#719).
- The arguments of derived integer types (`xs:int`, `xs:long`, and so on) are cast to derived types now, but the
  QT3 cases that build them stay excluded in `Qt3NumericSelection` until #746 selects them.
- The XPath 1.0 compatibility mode is not applied (XP-20).
