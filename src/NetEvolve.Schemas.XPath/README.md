# NetEvolve.Schemas.XPath

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.XPath.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.XPath.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

An XPath engine for .NET that compiles an expression once and evaluates it against documents of `NetEvolve.Schemas.Xml`. It implements XPath 2.0 and XPath 1.0 compatibility mode, denies external resources by default, and bounds every evaluation. It is the expression engine behind `NetEvolve.Schemas.Schematron`, and it is usable on its own.

## Features

- Compiled, immutable expressions - compile once with `XPathExpression.Compile` or `TryCompile`, evaluate many times with `Evaluate` or `TryEvaluate`
- XPath 2.0 (Second Edition) - the language and the function library, measured against the W3C QT3 test suite, see [Conformance](#conformance)
- XPath 1.0 compatibility mode - a switch of the static context, see [XPath 1.0 compatibility mode](#xpath-10-compatibility-mode)
- Variables and host functions - bind values on the evaluation context and register your own functions on the static context
- Deny-by-default resources - `fn:doc` and the text functions reach nothing unless a resolver is set
- Bounded evaluation - limits for recursion depth, sequence size, range size, and string length, see [Evaluation limits](#evaluation-limits)
- XPath 3.0 and 3.1 in progress - parts of the syntax and function library exist behind a version gate that the public entry does not open yet, see [Limitations](#limitations)

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas.XPath
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas.XPath
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas.XPath" Version="x.y.z" />
```

No package has been released yet; the lines apply from the first release, and `x.y.z` stands for the released version.

## Quick Start

`XPathExpression.Compile` parses and compiles the expression and throws `XPathException` for a static error. An `XPathEvaluationContext` carries the context item, the variables, and the resolver of one evaluation; `Evaluate` returns the result as an `XdmSequence`. A document is an `XmlDocumentTree` of `NetEvolve.Schemas.Xml`, wrapped in an `XPathNode`.

The samples in this readme need these namespaces:

```
using System;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
```

```csharp
var order = new XPathNode(
    XmlDocumentLoader.Load("""<order><line price="10"/><line price="15"/></order>""".AsSpan()).Document
);
var expression = XPathExpression.Compile("sum(/order/line/@price)");

using var context = new XPathEvaluationContext();
context.SetContextItem(XdmItem.FromNode(order));
var result = expression.Evaluate(context);
```

Here `result` holds one item, the `xs:double` 25. Read an item with `TryGetDouble`, `TryGetInteger`, `TryGetString`, `TryGetBoolean`, or `TryGetNode`.

## Usage

### Compile without exceptions

`TryCompile` returns an `XPathCompileResult` and never throws for the expression: `Success` tells the outcome, `Expression` is the compiled expression or `null`, and `Diagnostic` holds the error code, the kind, the message, and the position.

```csharp
var compiled = XPathExpression.TryCompile("1 +");
```

Here `compiled.Success` is `false` and `compiled.Diagnostic.Code` is `XPST0003`. `TryCompile` accepts a `string` or a `ReadOnlySpan<char>`; `Compile` accepts a `string`.

### Variables

A variable is declared on the static context, with `WithVariable`, and bound on every evaluation context, with `Variables.Set`. Rebinding between evaluations is cheap; the compiled expression is not touched. `TryEvaluate` returns an `XPathResult` with a status instead of throwing for a dynamic error.

```csharp
var order = new XPathNode(
    XmlDocumentLoader.Load("""<order><line price="10"/><line price="15"/></order>""".AsSpan()).Document
);
var limit = new XPathQName(string.Empty, "limit");
var statics = XPathStaticContext.Default.ToBuilder().WithVariable(limit).Build();
var expression = XPathExpression.Compile("count(/order/line[@price > $limit])", statics);

using var context = new XPathEvaluationContext();
context.SetContextItem(XdmItem.FromNode(order));
context.Variables.Set(limit, XdmItem.FromInteger(12));
var result = expression.TryEvaluate(context);
```

Here `result.Status` is `Ok` and `result.Value` holds the integer 1. An unbound variable is the dynamic error `XPDY0002`.

### Host functions

A host function derives from `XPathHostFunction`: it names itself with an `XPathQName`, states its minimum and maximum arity, and reads its arguments from the `XPathFunctionCall`. It sets `Result` and returns `Ok`, or sets `Diagnostic` and returns `Error`. A function must be stateless or thread-safe, because compiled expressions are shared. `WithFunction` registers one function and `WithLibrary` registers an `XPathFunctionLibrary`; a later layer wins over an earlier one, and every host layer wins over the built-in library.

```csharp
internal sealed class DoubleFunction() : XPathHostFunction(new XPathQName("urn:example:app", "double"), 1, 1)
{
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.Count != 1 || !argument[0].TryGetInteger(out var value))
        {
            invocation.Diagnostic = XPathDiagnostic.Create(new XPathErrorCode("urn:example:app", "APP0001"));
            return XPathEvaluationStatus.Error;
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(value * 2));
        return XPathEvaluationStatus.Ok;
    }
}
```

```csharp
var statics = XPathStaticContext
    .Default.ToBuilder()
    .WithNamespace("app", "urn:example:app")
    .WithFunction(new DoubleFunction())
    .Build();
var expression = XPathExpression.Compile("app:double(21)", statics);

using var context = new XPathEvaluationContext();
var result = expression.TryEvaluate(context);
```

Here `result.Value` holds the integer 42. A call with an argument that is no single integer fails with the code `{urn:example:app}APP0001`. The limits of host functions are listed under [Limitations](#limitations).

### XPath 1.0 compatibility mode

`WithXPath10Compatibility(true)` on the static context compiles the expression in the compatibility mode of XPath 2.0 (Appendix I): the first item of a sequence is used where one item is expected, and operands and function arguments convert to numbers and strings by the XPath 1.0 rules. The Schematron query bindings `xslt` and `xpath` use this mode. The mode follows XPath 2.0, so the incompatibilities listed in Appendix I.1 remain; the [rules and the known differences](https://github.com/dailydevops/schemas/blob/main/src/NetEvolve.Schemas.XPath/Compatibility/README.md) are documented beside the code.

```csharp
var compatible = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(true).Build();
var expression = XPathExpression.Compile("'3' + 4", compatible);

using var context = new XPathEvaluationContext();
var result = expression.Evaluate(context);
```

Here `result` holds the `xs:double` 7; without the mode the same expression is the type error `XPTY0004`.

### Input kinds

| Input | How it reaches the engine |
| --- | --- |
| Expression | A `string` for `Compile`, a `string` or a `ReadOnlySpan<char>` for `TryCompile`. |
| Document | An `XmlDocumentTree` loaded by `XmlDocumentLoader` of `NetEvolve.Schemas.Xml` from characters, bytes, a sequence, or a stream, wrapped in an `XPathNode` and set as the context item. |
| Context item | Any `XdmItem`: a node or an atomic value (`FromInteger`, `FromString`, `FromBoolean`, `FromDouble`, `FromDecimal`, `FromDateTime`, `FromDuration`, `FromNode`), with an optional position and size. |
| Variables | `XPathVariables` of the evaluation context, keyed by `XPathQName`. |
| Resolver | An `IXPathResourceResolver` on the context, see [Security defaults](#security-defaults). |

## Configuration

### Static context

`XPathStaticContext` is immutable. `XPathStaticContext.Default` is the XPath 2.0 context with the standard function namespace as default; `ToBuilder` derives a variant. The builder sets the language version, the compatibility mode, the static base URI, namespace prefixes, the default element namespace, declared variables, and host functions or libraries.

### Security defaults

External resources are denied by default. An `XPathEvaluationContext` has no resolver, so `fn:doc`, `fn:doc-available`, and the text functions reach nothing: `fn:doc` raises `FODC0002`, and no file system or network is touched. `fn:collection` raises `FODC0002` for every call (DEF-49).

```csharp
var expression = XPathExpression.Compile("doc('https://example.com/orders.xml')");

using var context = new XPathEvaluationContext();
var result = expression.TryEvaluate(context);
```

Here `result.Status` is `Error` and `result.Diagnostic.Code` is `FODC0002`. To opt in, set `ResourceResolver` of the context to an `IXPathResourceResolver`, which is an `IResourceResolver` of the core package `NetEvolve.Schemas`. Every request counts against the limit of external resources below. A document type declaration in a loaded document is refused by the secure reader defaults.

### Evaluation limits

Every evaluation is bounded, so a hostile or faulty expression ends with an error instead of exhausting memory. The limits flow from the `ValidationLimits` of the core package (`ValidationLimits.Default`); a test asserts that the defaults in this table equal the defaults in code.

| Limit | Default | Source and error |
| --- | --- | --- |
| Recursion depth | 256 | `MaxDepth`: depth of an expression and of recursive evaluation. |
| Sequence size | 10,000,000 | `MaxEntityExpansion`: items of a sequence. |
| Range size | 10,000,000 | `MaxEntityExpansion`: the length of `a to b`. |
| String length | 10,000,000 | `MaxEntityExpansion`: characters of a constructed string. |
| External resources | 16 | `MaxExternalResources`: resolver requests of one evaluation context. |
| Resource size | 16 MiB | `MaxResourceSize`: bytes of one resource. |

The first four limits are held by the engine and are not configurable through the public API on main; the last two are the defaults of `ValidationLimits` and are applied to the resources of a context. An exceeded limit is an `XPathException` or a result with the status `Error`, never a crash.

### Benchmarks

The benchmark classes live in `benchmarks/NetEvolve.Schemas.Benchmarks/XPath`, and the recorded targets are in [`benchmarks/targets/xpath.json`](https://github.com/dailydevops/schemas/blob/main/benchmarks/targets/xpath.json). The gate that compares a run with the targets is described in the [benchmark targets readme](https://github.com/dailydevops/schemas/blob/main/benchmarks/targets/README.md). The figures are the targets of main at the time of writing: the recorded mean and the allocation per operation of one benchmark invocation. The ratio is the recorded mean divided by the mean of the `System.Xml` baseline of the same class, where a baseline exists.

| Benchmark | Mean | Allocated | Ratio to `System.Xml` |
| --- | ---: | ---: | ---: |
| `XPathLexBenchmarks.LexExpressions` | 6.2 us | 0 B | - |
| `XPathParseBenchmarks.ParseExpressions` | 26.9 us | 19,885 B | - |
| `XPathCompileBenchmarks.CompileCompatible` | 15.3 us | 20,306 B | 3.7 |
| `XPathCompileExtendedBenchmarks.CompileExtended` | 23.0 us | 17,413 B | - |
| `XPathAxisWalkBenchmarks.WalkDescendants` | 16.1 us | 0 B | 1.1 |
| `XPathPredicateBenchmarks.EvaluatePredicates` | 356.0 us | 8,226 B | 8.3 |
| `XPathComparisonBenchmarks.EvaluateComparisons` | 116.7 us | 18,786 B | 4.6 |
| `XPathComparisonExtendedBenchmarks.EvaluateExtended` | 608.6 us | 30,446 B | - |

The targets file does not record the machine, the runtime, or the BenchmarkDotNet version; the gate tolerates the noise of a shared runner (15 percent on a ratio, 5 percent on a mean), and the figures are no comparison across machines.

## Conformance

The engine is measured against the W3C QT3 test suite, pinned by commit `201a6e466940cdfc727f4babfedcde5332b9f578` in [`tests/conformance/suites/qt3.json`](https://github.com/dailydevops/schemas/blob/main/tests/conformance/suites/qt3.json). No part of the suite is committed; it is fetched by checksum. The rate is passed / (passed + failed), truncated to one decimal, as the [Conformance workflow](https://github.com/dailydevops/schemas/blob/main/.github/workflows/conformance.yml) publishes it in its job summary. A skipped case is a case that the runner does not decide, such as the serialization assertions; it never counts as passed and is not part of the rate.

### XPath 2.0

**14,325 of 14,550 decided cases pass, a rate of 98.5%** on `net10.0`, the leg that runs the suites by default. The other 225 are known failures, and 738 skipped cases do not count. The 963 baseline entries (225 failures and 738 skipped cases) are the files of `tests/conformance/baselines/qt3`, one file per test set (238 test sets), and the run fails on a regression and on a listed case that passes.

Reproduce the numbers on a clone: set `GITHUB_STEP_SUMMARY` to a file and run the conformance tests; the table of the rate per test set is appended to that file.

```bash
dotnet test --project tests/NetEvolve.Schemas.XPath.Tests.Integration --configuration Release --framework net10.0 --treenode-filter "/*/*Conformance*/*/*"
```

The XPath 2.0 selection excludes the cases that need XSD 1.1, XML 1.1, schema import, schema validation, static typing, the namespace axis, or a collation other than the codepoint collation. The selection rules are in [`Qt3Selector.cs`](https://github.com/dailydevops/schemas/blob/main/tests/NetEvolve.Schemas.XPath.Tests.Integration/Qt3/Runner/Qt3Selector.cs) and the [suite manifest notes](https://github.com/dailydevops/schemas/blob/main/tests/conformance/suites/README.md).

### Exclusions

Each deferred feature is its own issue.

| Feature | Deferral | Behavior |
| --- | --- | --- |
| Schema-aware processing | DEF-40 ([#52](https://github.com/dailydevops/schemas/issues/52)) | Schema import and validation are not supported. |
| Static typing feature | DEF-41 ([#53](https://github.com/dailydevops/schemas/issues/53)) | Types are checked at run time only. |
| Namespace axis | DEF-42 ([#54](https://github.com/dailydevops/schemas/issues/54)) | The step is the static error `XPST0010` in every mode. |
| Unicode Collation Algorithm collations | DEF-43 ([#55](https://github.com/dailydevops/schemas/issues/55)) | Only the codepoint collation is available. |
| `fn:collection` | DEF-49 ([#61](https://github.com/dailydevops/schemas/issues/61)) | Every call raises `FODC0002`. |
| `FULLY-NORMALIZED` normalization form | DEF-75 ([#87](https://github.com/dailydevops/schemas/issues/87)) | `fn:normalize-unicode` does not support the form. |

### XPath 3.0 and 3.1

The QT3 runner compiles every query as XPath 2.0, because the compile entry does not enable the later versions (a runner mode for XPath 3.0 and 3.1 is missing). The numbers of the two later selections are therefore the results of cases that an XPath 2.0 engine can decide, and they are not a statement of conformance to XPath 3.0 or 3.1. A case counts once, in the first tier that selects it, so the figures cover the cases that the earlier tier does not select. They are not published by the workflow yet; they were counted on 2026-11-24 with the pinned suite on `net10.0`.

| Tier | New cases | Pass | Fail | Skipped | Rate | Baseline |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| XPath 3.0 | 4,028 | 1,125 | 2,729 | 174 | 29.1% | 2,903 baseline entries in `tests/conformance/baselines/qt3-xp30` |
| XPath 3.1 | 1,713 | 236 | 1,222 | 255 | 16.1% | 1,477 baseline entries in `tests/conformance/baselines/qt3-xp31` |

The selection rules, the exclusion list of the later tiers, and the regeneration commands are in the [selections readme](https://github.com/dailydevops/schemas/blob/main/tests/NetEvolve.Schemas.XPath.Tests.Integration/Qt3/Selections/README.md). The conformance statement for XPath 3.0 and 3.1 is planned as XP-76.

## Limitations

- **XPath 3.0 and 3.1 are not enabled.** `XPathStaticContext.WithVersion` accepts `V30` and `V31`, but `Compile` and `TryCompile` reject them with the engine error `XP0001`; only XPath 2.0 compiles. The syntax of 3.0 ([#328](https://github.com/dailydevops/schemas/issues/328)), `let`, the simple map operator, and string concatenation ([#355](https://github.com/dailydevops/schemas/issues/355)), and the syntax of 3.1 for maps, arrays, lookup, and arrow ([#356](https://github.com/dailydevops/schemas/issues/356)) are tracked as parts of that work. The math functions ([#329](https://github.com/dailydevops/schemas/issues/329)), `fn:analyze-string` ([#332](https://github.com/dailydevops/schemas/issues/332)), and `fn:format-integer` ([#330](https://github.com/dailydevops/schemas/issues/330)) exist in the function library and are gated to the later versions, so they are not callable through the public entry yet. Function items, inline functions, dynamic calls, and partial application are in progress ([#353](https://github.com/dailydevops/schemas/issues/353)).
- **Host functions.** A host function declares no parameter types, so it receives its arguments as evaluated, without function conversion or atomization, until the contract can declare them ([#586](https://github.com/dailydevops/schemas/issues/586)). A function that needs a host slot (current item, current group, regex groups, decimal formats, keys) states it in `HostRequirements`, and the slots are set on `XPathEvaluationContext.Host`.
- **Documents and base URI.** The built-in document functions hold no static base URI, so a relative reference is unresolvable, and a document is loaded with the secure reader defaults. The [document functions readme](https://github.com/dailydevops/schemas/blob/main/src/NetEvolve.Schemas.XPath/Functions/Documents/README.md) lists the rest.
- **Schema-aware processing, static typing, the namespace axis, UCA collations, `fn:collection`, and the fully normalized form** are not supported, see [Exclusions](#exclusions).
- **Evaluation limits** are not configurable through the public API on main, see [Evaluation limits](#evaluation-limits).
- **Year range.** Dates and dateTimes accept a year of up to 17 digits, the range of the datatype layer ([#100](https://github.com/dailydevops/schemas/issues/100), [#301](https://github.com/dailydevops/schemas/issues/301)); Functions and Operators 1.0 lets an implementation choose a smaller range. Eight QT3 cases expect a rejection beyond a smaller range and are a deliberate deviation, tracked in [#889](https://github.com/dailydevops/schemas/issues/889): the four `castable as` cases `cbcl-castable-date-001`, `cbcl-castable-date-002`, `cbcl-castable-dateTime-001`, and `cbcl-castable-dateTime-002` (excluded from the selection) and the four cases `cbcl-adjust-date-to-timezone-002` and `-003` and `cbcl-adjust-dateTime-to-timezone-002` and `-003` (listed in the baseline), which expect `FODT0001`.
- **Conformance.** The cases with serialization assertions and compound assertions are skipped by the runner and are not part of the rate.
- **Compilers older than C# 14.** The API of this package has no C# 14 extension members, so every compiler calls it the same way; the C# 14 calling path of `SchemaValidator.ForSchematron` belongs to the Schematron package.

## Requirements

- .NET Standard 2.0, .NET 10, or .NET 11 (the package targets `netstandard2.0`, `net10.0`, and `net11.0`; the tests run on `net5.0`, `net10.0`, and `net11.0`, and `net5.0` runs the `netstandard2.0` build)
- `NetEvolve.Schemas.Xml` and `NetEvolve.Arguments` as dependencies; on `netstandard2.0` also `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`

## Related Packages

- [**NetEvolve.Schemas**](https://www.nuget.org/packages/NetEvolve.Schemas/) - Core abstractions: validation limits, resource resolvers, and the validator anchor
- [**NetEvolve.Schemas.Xml**](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/) - XML 1.0 reader and the document tree that the expressions run on
- [**NetEvolve.Schemas.Schematron**](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/) - ISO Schematron validation on top of this engine

## Documentation

For complete documentation, please visit the [official documentation](https://github.com/dailydevops/schemas/blob/main/README.md).

## Contributing

Contributions are welcome! Please read the [Contributing Guidelines](https://github.com/dailydevops/schemas/blob/main/CONTRIBUTING.md) before submitting a pull request.

## Support

- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/dailydevops/schemas/issues)
- **Documentation**: Read the full documentation at [https://github.com/dailydevops/schemas](https://github.com/dailydevops/schemas)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dailydevops/schemas/blob/main/LICENSE) file for details.

---

> [!NOTE]
> **Made with ❤️ by the NetEvolve Team**
> Visit us at [https://www.daily-devops.net](https://www.daily-devops.net) for more information about our services and solutions.
