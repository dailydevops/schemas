# NetEvolve.Schemas.Schematron

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.Schematron.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.Schematron.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

Validates XML documents against ISO Schematron schemas. A schema is compiled once into an immutable, thread-safe validator; a validation reports every failed `assert` and every successful `report` as an issue, and can also write the run as SVRL (Schematron Validation Report Language). The implemented edition is ISO/IEC 19757-3:2025, and a schema without `schematronEdition` keeps the semantics of the earlier edition.

## Features

- ISO Schematron 2025 and 2016 - the edition is detected from `schematronEdition`, see [Edition detection](#edition-detection)
- Compiled, thread-safe validators - build once with `SchemaValidator.ForSchematron`, validate many documents from many threads
- Single document pass - the rule contexts of the active patterns are matched in one pass over a document
- Phases and parameters - select a phase with `WithPhase` and set a `param` with `WithParameter`
- SVRL output - `SvrlReportSink` writes the events of a run as 2016-compatible SVRL plus the 2025 grammar
- Query bindings - `xslt`, `xslt1`, `xslt2`, `xslt3`, `xpath`, and `xpath2` on the XPath engine of `NetEvolve.Schemas.XPath`, see [Query bindings](#query-bindings)
- A subset of XSLT 2.0 in a schema - functions, keys, variables, and the instructions of their bodies, see [XSLT constructs](#xslt-constructs)
- Deny-by-default resources - `include`, `extends`, and `xi:include` reach nothing unless a resolver is set, see [Resolvers and security defaults](#resolvers-and-security-defaults)

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas.Schematron
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas.Schematron
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas.Schematron" Version="x.y.z" />
```

No package has been released yet; the lines apply from the first release, and `x.y.z` stands for the released version.

## Quick Start

`SchemaValidator.ForSchematron` is a C# 14 extension member of the `SchemaValidator` anchor of the core package. It takes the schema as a `Stream`, `ReadOnlySpan<byte>`, `ReadOnlySpan<char>` (a `string` binds to it), `ReadOnlySequence<byte>`, or `ReadOnlySequence<char>`, and an optional `systemId`, the base URI of the schema that `include` resolves against. Nothing is read or compiled by the call: a stream is read from its current position at `Build` and never disposed, and spans and sequences are copied once. Every problem of the schema surfaces at `Build` or `BuildAsync` as one `SchemaCompilationException` that carries the issues. The builder is not thread-safe; the validator it builds is.

The samples in this readme run in an async method that has a `CancellationToken cancellationToken`. They need these namespaces:

```
using System.Buffers;
using System.Text;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Schematron.Svrl;
```

A failed `assert` is an issue with severity `Error` and the code `NES3400`; a successful `report` is an issue with severity `Warning` and the code `NES3401`, so a document that only fires reports is still valid. The message is the rendered text of the assertion, the constraint is the `id` of the assertion, and the location is the position of the reported node, with the `systemId` of the document as its source.

```csharp
const string schema = """
    <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
      <sch:pattern id="totals">
        <sch:rule id="order" context="order">
          <sch:assert id="total-limit" test="number(@total) le 100">The total <sch:value-of select="@total"/> exceeds 100.</sch:assert>
          <sch:report test="number(@total) = 0">The total is zero.</sch:report>
        </sch:rule>
      </sch:pattern>
    </sch:schema>
    """;

var validator = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);
var result = await validator.ValidateAsync(
    """<order total="150"/>""".AsMemory(),
    "file:///orders/1.xml",
    cancellationToken
);
```

Here `result.IsValid` is `false`, and `result.Issues` holds one issue: `NES3400`, severity `Error`, the message `The total 150 exceeds 100.`, the constraint `total-limit`, and the location `file:///orders/1.xml` at line 1, column 1.

## Usage

The builder sets the phase with `WithPhase`, the parameters with `WithParameter`, the limits of the core package with `WithLimits`, and the resolver for external resources with `WithResolver`. The validator is an `ISchematronValidator`, an `ISchemaValidator` of the core package that also exposes the effective model (`Schema`), the `Edition`, the `Phase`, the `EffectivePhase`, and the `Parameters`.

### Phases and parameters

`WithPhase` takes a phase `id`, `#ALL`, or `#DEFAULT` (the default: the `defaultPhase` of the schema, or `#ALL`). An unknown phase fails at `Build` with `NES3013`. `WithParameter` sets the value of a `param` of the schema; the last call per name wins, and an undeclared name fails at `Build` with `NES3112`. A parameter value is a literal string, never an expression, so an expression that compares it with a number converts it first. The element `param` belongs to the 2025 edition and needs `schematronEdition="2025"`.

```csharp
const string schema = """
    <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" schematronEdition="2025" defaultPhase="basic">
      <sch:param name="limit" value="100"/>
      <sch:phase id="basic"><sch:active pattern="totals"/></sch:phase>
      <sch:phase id="strict"><sch:active pattern="totals"/><sch:active pattern="notes"/></sch:phase>
      <sch:pattern id="totals">
        <sch:rule context="order"><sch:assert test="number(@total) le number($limit)">Over <sch:value-of select="$limit"/>.</sch:assert></sch:rule>
      </sch:pattern>
      <sch:pattern id="notes">
        <sch:rule context="order"><sch:assert test="note">An order needs a note.</sch:assert></sch:rule>
      </sch:pattern>
    </sch:schema>
    """;

var validator = await SchemaValidator
    .ForSchematron(schema)
    .WithPhase("strict")
    .WithParameter("limit", "200")
    .BuildAsync(cancellationToken);
var result = await validator.ValidateAsync(
    """<order total="150"/>""".AsMemory(),
    cancellationToken: cancellationToken
);
```

Here the phase `strict` is active and the parameter `limit` is `200`, so the first pattern passes, and the only issue is the failed assertion of the second pattern.

### SVRL

`SvrlReportSink` is an `ISchematronReportSink` that writes the events of a run as SVRL: 2016-compatible output plus the attributes and elements of the public 2025 grammar. It writes to an `IBufferWriter<byte>` (complete when the run finishes) or to a `Stream` (complete after `CompleteAsync`). `WithReportSink` returns a lightweight view of the validator that delivers the events of each validation to the sink; the view serves one validation at a time, and the validator it came from stays thread-safe. `SvrlWriterOptions` sets the encoding, the indentation, the namespace declarations, and the cap of the buffer of one active pattern (16 MiB by default; on overflow an `error` with `NES3500` replaces the fired rules and findings of that pattern, and the validation result is not affected).

```csharp
var output = new ArrayBufferWriter<byte>();
using var sink = new SvrlReportSink(output);
var schematron = await SchemaValidator.ForSchematron(schema).BuildAsync(cancellationToken);
var validator = schematron.WithReportSink(sink);

var result = await validator.ValidateAsync(
    """<order total="150"/>""".AsMemory(),
    cancellationToken: cancellationToken
);
var svrl = Encoding.UTF8.GetString(output.WrittenSpan);
```

Here `schema` is the schema of the first sample, and `svrl` holds a `svrl:schematron-output` document with the active pattern `totals`, the fired rule `order`, and the `svrl:failed-assert` `total-limit`.

### Query bindings

The `queryBinding` of the schema selects the language of the expressions. Names compare without regard to case, and a schema without `queryBinding` is bound as `xslt`. Any other name fails at `Build` with `NES3010`, which lists the supported names. The table lists every reserved name: supported, planned with its work package, or deferred with its DEF ID.

| Binding | Status | Notes |
| --- | --- | --- |
| `xslt` | supported | The default binding. XPath 1.0 compatibility mode on the XPath 2.0 engine; XSLT elements and the functions of XSLT are available. |
| `xslt1` | supported | As `xslt`. It is XPath 1.0 compatibility mode, not XSLT 1.0: the differences of XSLT 1.0 to XSLT 2.0 are not applied. |
| `xpath` | supported | XPath 1.0 compatibility mode. XSLT elements in the schema are ignored, and the functions of XSLT are not available. |
| `xslt2` | supported | XPath 2.0. XSLT elements and the functions of XSLT are available. |
| `xpath2` | supported | XPath 2.0. XSLT elements in the schema are ignored, and the functions of XSLT are not available. |
| `xslt3` | supported, with the XPath 2.0 contract of `xslt2` | XPath 3.x semantics are planned: SCH-21 ([#378](https://github.com/dailydevops/schemas/issues/378)). The cases of the suite for this binding are excluded from the conformance rate. |
| `xpath3` | planned: SCH-21 ([#378](https://github.com/dailydevops/schemas/issues/378)) | Rejected with `NES3010` until the binding is enabled. |
| `xpath31` | planned: SCH-21 ([#378](https://github.com/dailydevops/schemas/issues/378)) | Rejected with `NES3010` until the binding is enabled. |
| `xpath4` | deferred: DEF-38 ([#50](https://github.com/dailydevops/schemas/issues/50)) | |
| `xquery` | deferred: DEF-34 ([#46](https://github.com/dailydevops/schemas/issues/46)) | |
| `xquery3` | deferred: DEF-35 ([#47](https://github.com/dailydevops/schemas/issues/47)) | |
| `xquery31` | deferred: DEF-36 ([#48](https://github.com/dailydevops/schemas/issues/48)) | |
| `xquery4` | deferred: DEF-37 ([#49](https://github.com/dailydevops/schemas/issues/49)) | |
| `xslt1.1` | deferred: DEF-59 ([#71](https://github.com/dailydevops/schemas/issues/71)) | |
| `xslt4` | deferred: DEF-39 ([#51](https://github.com/dailydevops/schemas/issues/51)) | |
| `exslt` | deferred: DEF-56 ([#68](https://github.com/dailydevops/schemas/issues/68)) | |
| `stx` | deferred: DEF-57 ([#69](https://github.com/dailydevops/schemas/issues/69)) | |

The expressions run on the XPath engine of `NetEvolve.Schemas.XPath`, so its function library bounds what an expression can call. A rule context keeps XPath 2.0 semantics under every binding ([#838](https://github.com/dailydevops/schemas/issues/838)).

### Edition detection

The edition is read from the `schematronEdition` attribute of the `schema` element: an absent attribute and the value `2016` select the semantics of the earlier edition, the value `2025` selects the semantics of the 2025 edition, and any other value, compared exactly, fails at `Build` with `NES3009`. An element or an attribute that exists only in the 2025 edition, such as `param`, fails with `NES3008` unless the schema declares `schematronEdition="2025"`. The edition of the validator is `ISchematronValidator.Edition`, and the SVRL output carries `schematronEdition` for the 2025 edition only.

## XSLT constructs

Schematron lets a schema embed XSLT: stylesheet functions, keys, variables, and the instructions of their bodies. The package ships a subset of XSLT 2.0, delivered in five stages that the rule sets of real schemas need:

1. Stylesheet functions with the flow instructions of their bodies, decimal formats, and `format-number`.
2. Keys.
3. Stylesheet modules (`xsl:include`, `xsl:import`) and access to further documents.
4. Global variables and parameters, temporary trees, and `let` with element content.
5. Grouping and analysis of strings with regular expressions.

### What a schema reaches today

The instruction engine (instruction registry, sequence sink, function table, key index, global table, module linker) is part of the package and covered by its unit tests, and the build of a schema hands the XSLT declarations to it. Under the bindings `xslt`, `xslt1`, `xslt2`, and `xslt3` the resolve stage follows `xsl:include` and `xsl:import` through the resolver of the builder, and the compile stage compiles `xsl:function`, `xsl:key`, `xsl:variable`, `xsl:param`, and `xsl:decimal-format` with the import precedence applied; any other `xsl:` element, such as `xsl:template`, fails at `Build` with `NES3204`, the unsupported XSLT construct error. The compiled schema holds the tables; the global variables and parameters take part in a run, and a rule, a variable, or another function body calls a stylesheet function through an XPath expression ([#628](https://github.com/dailydevops/schemas/issues/628)), while keys are not callable from the XPath of a rule yet ([#832](https://github.com/dailydevops/schemas/issues/832)). Problems of a declaration that an included Schematron library holds are located at the principal schema, and without a line. A `let` with element content and a property with `xsl:copy-of` build, and the run reports `NES3204` where it needs them, because no embedded content is wired into the pipeline ([#914](https://github.com/dailydevops/schemas/issues/914)); a `let` with element content binds a document node (maintainer decision of [#856](https://github.com/dailydevops/schemas/issues/856)), which the suite case for it does not expect ([#877](https://github.com/dailydevops/schemas/issues/877)).

What does work are the XSLT functions in the expressions of a schema (`test`, `select`, `value`, `context`) under the bindings `xslt`, `xslt1`, `xslt2`, and `xslt3`: `current`, `system-property`, `function-available`, `format-number`, and `generate-id`. These functions do not exist under `xpath` and `xpath2`. A named decimal format is unknown, because no `xsl:decimal-format` is handed to the run ([#882](https://github.com/dailydevops/schemas/issues/882)).

These functions are not available yet: `key` (the function is in the XPath package, [#776](https://github.com/dailydevops/schemas/issues/776), but a run has no key provider until the stylesheet modules reach it, [#832](https://github.com/dailydevops/schemas/issues/832); as a rule context, [#780](https://github.com/dailydevops/schemas/issues/780)), `document`, `doc`, and `unparsed-text` (the XPath package has the functions, but a schema run does not bind them to the schema base URI and the resolver yet, [#312](https://github.com/dailydevops/schemas/issues/312)), `current-group` and `current-grouping-key` ([#803](https://github.com/dailydevops/schemas/issues/803)), `regex-group` (the function is in the package, [#287](https://github.com/dailydevops/schemas/issues/287), but a run offers no regex groups slot, [#628](https://github.com/dailydevops/schemas/issues/628)), `element-available` (DEF-30, [#42](https://github.com/dailydevops/schemas/issues/42)), `type-available` (DEF-31, [#43](https://github.com/dailydevops/schemas/issues/43)), `unparsed-entity-uri` and `unparsed-entity-public-id` (DEF-32, [#44](https://github.com/dailydevops/schemas/issues/44)), and `snapshot` (DEF-33, [#45](https://github.com/dailydevops/schemas/issues/45)).

### Matrix of the registered constructs

Every entry of the instruction registry, with the stage that delivered it. The position is where the registry accepts the element: an instruction (in a sequence constructor), a declaration (top level), or a child of a parent instruction.

| Construct | Position | Stage | Notes |
| --- | --- | --- | --- |
| `xsl:function` | declaration | 1 | Overloads by arity. The `as` conversion of arguments and results does not cover derived atomic types and kind tests ([#635](https://github.com/dailydevops/schemas/issues/635)). |
| `xsl:param` | child of `xsl:function`, or declaration | 1, 4 | Stage 1: parameter of a function. Stage 4: global parameter, valued by `WithParameter`; `static` is not supported (DEF-28, [#40](https://github.com/dailydevops/schemas/issues/40)). |
| `xsl:variable` | instruction, or declaration | 1, 4 | Stage 1: local variable. Stage 4: global variable with lazy evaluation and circularity detection. The value is the `select` attribute or, for a local variable, a sequence constructor (a temporary tree with a document node, or the converted sequence with `as`); the global form with content is [#794](https://github.com/dailydevops/schemas/issues/794); with `select`, `as` is limited to `xs:string`, `xs:boolean`, `xs:double`, and `xs:integer` (`NES3007`, [#392](https://github.com/dailydevops/schemas/issues/392)); `static` is not supported (DEF-28, [#40](https://github.com/dailydevops/schemas/issues/40)). |
| `xsl:sequence` | instruction | 1 | |
| `xsl:value-of` | instruction | 1 | |
| `xsl:text` | instruction | 1 | |
| `xsl:if` | instruction | 1 | |
| `xsl:choose` | instruction | 1 | |
| `xsl:when` | child of `xsl:choose` | 1 | |
| `xsl:otherwise` | child of `xsl:choose` | 1 | |
| `xsl:for-each` | instruction | 1 | |
| `xsl:sort` | child of `xsl:for-each` and `xsl:for-each-group` | 1 | `data-type` is limited to text and the key is the `select` attribute ([#682](https://github.com/dailydevops/schemas/issues/682)); the collation is limited to the codepoint collation (DEF-43, [#55](https://github.com/dailydevops/schemas/issues/55)). |
| `xsl:message` | instruction | 1 | The output does not reach the validation result ([#683](https://github.com/dailydevops/schemas/issues/683)). |
| `xsl:decimal-format` | declaration | 1 | `exponent-separator` is not supported ([#836](https://github.com/dailydevops/schemas/issues/836)). |
| `xsl:key` | declaration | 2 | `match` and `use` as attribute or as content. `composite` is not supported ([#779](https://github.com/dailydevops/schemas/issues/779)); the collation is limited to the codepoint collation (DEF-43, [#55](https://github.com/dailydevops/schemas/issues/55)). |
| `xsl:element` | instruction | 4 | |
| `xsl:attribute` | instruction | 4 | |
| `xsl:copy` | instruction | 4 | |
| `xsl:copy-of` | instruction | 4 | |
| `xsl:comment` | instruction | 4 | |
| `xsl:processing-instruction` | instruction | 4 | |
| `xsl:namespace` | instruction | 4 | |
| `xsl:document` | instruction | 4 | |
| literal result element | instruction | 4 | Has no XSLT name; the registry holds one entry for it. |
| text node | instruction | 4 | Character data of a sequence constructor; the registry holds one entry for it. |
| `xsl:for-each-group` | instruction | 5 | `group-by`, `group-adjacent`, `group-starting-with`, and `group-ending-with`. `composite` is not supported (DEF-54, [#66](https://github.com/dailydevops/schemas/issues/66)); the collation is limited to the codepoint collation (DEF-43, [#55](https://github.com/dailydevops/schemas/issues/55)). |
| `xsl:analyze-string` | instruction | 5 | `regex` and `flags` are attribute value templates; `xsl:fallback` inside it is not supported (DEF-27, [#39](https://github.com/dailydevops/schemas/issues/39)). A regex that is invalid, has invalid flags, or matches the zero-length string is a dynamic error when the instruction runs (XTDE1140, XTDE1145, XTDE1150). `regex-group()` is in the package, but no run offers its slot yet ([#628](https://github.com/dailydevops/schemas/issues/628)). |
| `xsl:matching-substring` | child of `xsl:analyze-string` | 5 | |
| `xsl:non-matching-substring` | child of `xsl:analyze-string` | 5 | |

The module linker (stage 3) is not an entry of the registry: it handles `xsl:include`, `xsl:import`, and the root elements `xsl:stylesheet` and `xsl:transform` of a module, with import precedence and cycle detection, and loads modules through the resolver of the builder. The build runs it in the resolve stage, with the loader of the Schematron `include`, so the resource limits are shared ([#832](https://github.com/dailydevops/schemas/issues/832)).

### The unsupported XSLT construct error

Every other element of the XSLT namespace is not registered. Compiling one raises `NES3204`, `The XSLT element 'xsl:name' is not supported.`, with the location of the element. The elements that are left out on purpose are deferred, each with its DEF ID:

| Element | Deferral |
| --- | --- |
| `xsl:template` | DEF-01 ([#13](https://github.com/dailydevops/schemas/issues/13)) |
| `xsl:apply-templates` | DEF-02 ([#14](https://github.com/dailydevops/schemas/issues/14)) |
| `xsl:call-template`, `xsl:with-param` | DEF-03 ([#15](https://github.com/dailydevops/schemas/issues/15)) |
| `xsl:mode` | DEF-04 ([#16](https://github.com/dailydevops/schemas/issues/16)) |
| `xsl:apply-imports` | DEF-05 ([#17](https://github.com/dailydevops/schemas/issues/17)) |
| `xsl:next-match` | DEF-06 ([#18](https://github.com/dailydevops/schemas/issues/18)) |
| `xsl:number` | DEF-07 ([#19](https://github.com/dailydevops/schemas/issues/19)) |
| `xsl:iterate`, `xsl:break`, `xsl:next-iteration`, `xsl:on-completion` | DEF-08 ([#20](https://github.com/dailydevops/schemas/issues/20)) |
| `xsl:try`, `xsl:catch` | DEF-09 ([#21](https://github.com/dailydevops/schemas/issues/21)) |
| `xsl:evaluate` | DEF-10 ([#22](https://github.com/dailydevops/schemas/issues/22)) |
| `xsl:import-schema` | DEF-11 ([#23](https://github.com/dailydevops/schemas/issues/23)) |
| `xsl:accumulator`, `xsl:accumulator-rule` | DEF-12 ([#24](https://github.com/dailydevops/schemas/issues/24)) |
| `xsl:package`, `xsl:use-package`, `xsl:accept`, `xsl:expose`, `xsl:override` | DEF-13 ([#25](https://github.com/dailydevops/schemas/issues/25)) |
| `xsl:assert` | DEF-15 ([#27](https://github.com/dailydevops/schemas/issues/27)) |
| `xsl:perform-sort` | DEF-16 ([#28](https://github.com/dailydevops/schemas/issues/28)) |
| `xsl:merge`, `xsl:merge-source`, `xsl:merge-key`, `xsl:merge-action` | DEF-17 ([#29](https://github.com/dailydevops/schemas/issues/29)) |
| `xsl:fork` | DEF-18 ([#30](https://github.com/dailydevops/schemas/issues/30)) |
| `xsl:source-document` | DEF-19 ([#31](https://github.com/dailydevops/schemas/issues/31)) |
| `xsl:result-document` | DEF-20 ([#32](https://github.com/dailydevops/schemas/issues/32)) |
| `xsl:output`, `xsl:character-map`, `xsl:output-character` | DEF-21 ([#33](https://github.com/dailydevops/schemas/issues/33)) |
| `xsl:attribute-set` | DEF-22 ([#34](https://github.com/dailydevops/schemas/issues/34)) |
| `xsl:namespace-alias` | DEF-23 ([#35](https://github.com/dailydevops/schemas/issues/35)) |
| `xsl:strip-space`, `xsl:preserve-space` | DEF-24 ([#36](https://github.com/dailydevops/schemas/issues/36)) |
| `xsl:where-populated`, `xsl:on-empty`, `xsl:on-non-empty` | DEF-25 ([#37](https://github.com/dailydevops/schemas/issues/37)) |
| `xsl:map`, `xsl:map-entry` | DEF-26 ([#38](https://github.com/dailydevops/schemas/issues/38)) |
| `xsl:fallback` | DEF-27 ([#39](https://github.com/dailydevops/schemas/issues/39)) |
| `xsl:context-item`, `xsl:global-context-item` | DEF-29 ([#41](https://github.com/dailydevops/schemas/issues/41)) |

The module elements of stage 3 (`xsl:include`, `xsl:import`, `xsl:stylesheet`, `xsl:transform`) are not registry entries either, see above; the build handles them in the linker ([#832](https://github.com/dailydevops/schemas/issues/832)). Further, a registered construct raises `NES3204` for an attribute or form that is left out:

| Attribute or form | Tracking |
| --- | --- |
| `type` and `validation` on the construction instructions and on literal result elements | DEF-11 ([#23](https://github.com/dailydevops/schemas/issues/23)) |
| `use-attribute-sets` on `xsl:element`, `xsl:copy`, and literal result elements | DEF-22 ([#34](https://github.com/dailydevops/schemas/issues/34)) |
| `inherit-namespaces`, and `copy-namespaces="no"` | [#813](https://github.com/dailydevops/schemas/issues/813) |
| The standard attributes `xsl:extension-element-prefixes`, `xsl:default-collation`, and `xsl:xpath-default-namespace` on a literal result element | [#815](https://github.com/dailydevops/schemas/issues/815) |
| An attribute value template with an expression in the attributes of the flow instructions | [#681](https://github.com/dailydevops/schemas/issues/681) |
| `data-type` other than text, and content as the key of `xsl:sort` | [#682](https://github.com/dailydevops/schemas/issues/682) |
| A collation other than the codepoint collation on `xsl:sort`, `xsl:for-each-group`, and `xsl:key` | DEF-43 ([#55](https://github.com/dailydevops/schemas/issues/55)) |
| `composite` on `xsl:for-each-group` | DEF-54 ([#66](https://github.com/dailydevops/schemas/issues/66)) |
| `composite` on `xsl:key` | [#779](https://github.com/dailydevops/schemas/issues/779) |
| `static` on `xsl:variable` and `xsl:param` | DEF-28 ([#40](https://github.com/dailydevops/schemas/issues/40)) |
| `exponent-separator` on `xsl:decimal-format` | [#836](https://github.com/dailydevops/schemas/issues/836) |

The attributes `use-when` (DEF-28, [#40](https://github.com/dailydevops/schemas/issues/40)) and `expand-text` (DEF-14, [#26](https://github.com/dailydevops/schemas/issues/26)) are not supported either. The pattern matcher for rule contexts and `xsl:key` covers a subset of the XSLT 2.0 patterns: patterns that start with `id()` (DEF-55, [#67](https://github.com/dailydevops/schemas/issues/67)), patterns that start with `key()` ([#780](https://github.com/dailydevops/schemas/issues/780)), and the pattern forms of XSLT 3.0 (DEF-60, [#72](https://github.com/dailydevops/schemas/issues/72)) are not supported.

## Configuration

The builder that `ForSchematron` returns takes the options of a build:

| Method | Effect |
| --- | --- |
| `WithPhase` | Selects the phase: an `id`, `#ALL`, or `#DEFAULT` (the default). |
| `WithParameter` | Sets the value of a `param` of the schema. |
| `WithLimits` | Sets the `ValidationLimits` of the core package that bound the documents and the schema. |
| `WithResolver` | Sets the `IResourceResolver` for external resources; the default is `DenyAllResourceResolver`. |

`WithReportSink` is a method of the built validator, not of the builder: it returns the view that delivers the events of a validation to an `ISchematronReportSink`, see [SVRL](#svrl).

### Resolvers and security defaults

External resources are deny by default. Unless a resolver is set, the builder uses `DenyAllResourceResolver`, which never opens anything, and a schema that reaches a resource fails at `Build`: an `include`, an `extends` with `href`, or an `xi:include` is refused with `NES0100` at the reference. A relative reference needs an absolute base URI, so pass the `systemId` of the schema to `ForSchematron`; without one the reference fails with `NES0101`.

To opt in, pass an `IResourceResolver` to `WithResolver`. The core package ships `SandboxedFileSystemResourceResolver`, which serves `file` URIs below one root directory and refuses everything else; any other resolver implements `IResourceResolver`. The limits `MaxExternalResources` and `MaxResourceSize` of `ValidationLimits` bound the resources of one build and the size of each.

In the sample, `schema` holds a schema with `<sch:include href="rules.sch"/>`, `systemId` is the file URI of the schema, and `schemaDirectory` is the folder that holds both files.

```csharp
var validator = await SchemaValidator
    .ForSchematron(schema, systemId)
    .WithResolver(new SandboxedFileSystemResourceResolver(schemaDirectory))
    .BuildAsync(cancellationToken);
```

No other path reaches a resource. A document type declaration in the schema or in a document is rejected, so no external entity or subset is read, and the expressions of a schema cannot open a resource, because `document`, `doc`, and `unparsed-text` are not available yet. The documents and the schema are read under `ValidationLimits` (size, depth, attributes, and entity expansion), and a schema is bounded by `MaxResourceSize`.

## Conformance

The package is measured against the official Schematron conformance suite, release 2.0, pinned by checksum in [`tests/conformance/suites/schematron-conformance.json`](https://github.com/dailydevops/schemas/blob/main/tests/conformance/suites/schematron-conformance.json); no part of the suite is committed. The run selects the cases of the suite bindings `xslt-1` and `xslt-2` and runs each through `SchemaValidator.ForSchematron` and the SVRL report sink. The rate is passed / (passed + failed), truncated to one decimal, as the [Conformance workflow](https://github.com/dailydevops/schemas/blob/main/.github/workflows/conformance.yml) publishes it in its job summary.

### Schematron conformance suite

**50 of 84 applicable cases pass, a rate of 59.5%** on `net10.0`, the leg that runs the suites. The other cases are the 34 known failures below, and 3 excluded cases do not count. Every known failure is a case of the committed baseline in `tests/conformance/baselines/schematron-conformance/` (34 baseline entries in 5 files, one file per test set), linked to an issue; a regression and an unexpected pass fail the run.

Reproduce the numbers on a clone: set `GITHUB_STEP_SUMMARY` to a file and run the conformance tests; the table of the rate per test set is appended to that file.

```bash
dotnet test --project tests/NetEvolve.Schemas.Schematron.Tests.Integration --configuration Release --framework net10.0 --treenode-filter "/*/*Conformance*/*/*"
```

| Test set | Applicable | Passed | Rate |
| --- | --- | --- | --- |
| `iso-instance-document-selection` | 2 | 0 | 0.0% |
| `iso-pattern-templating` | 12 | 4 | 33.3% |
| `iso-reporting` | 45 | 30 | 66.6% |
| `iso-rule-based-validation` | 14 | 11 | 78.5% |
| `iso-schema-composition` | 11 | 5 | 45.4% |

The acceptance of XSLT stage 4 (own test schemas and four cases of the suite, which are also among the 84) publishes its own row in the job summary, `NetEvolve.Schemas.Schematron (stage 4 goldens)`: 12 of 17 cases pass, a rate of 70.5%. The other 5 wait for [#914](https://github.com/dailydevops/schemas/issues/914) (a `let` with element content), for temporary trees in global declarations, and for builder parameters of a global `xsl:param` ([#832](https://github.com/dailydevops/schemas/issues/832)).

#### Excluded cases

The suite file of the binding `xslt-3-2025` lists three cases that no selected binding lists. The bindings `xslt-3` and `xslt-3-2025` are not selected, because the `xslt3` binding is owned by SCH-21 ([#378](https://github.com/dailydevops/schemas/issues/378)), so these cases are skipped and do not count:

- `iso-schema-composition/schema-phase-from.xml`
- `iso-pattern-templating/pattern-abstract-default-values.xml`
- `iso-rule-based-validation/rule-visit-each.xml`

#### Known failures

**[#575](https://github.com/dailydevops/schemas/issues/575)** (4 cases): A fragment identifier in the href of include and extends is not supported (NES3007).

- `iso-schema-composition/extends-href-fragment-xml-id-preserve-ns.xml`
- `iso-schema-composition/extends-href-fragment-xml-id.xml`
- `iso-schema-composition/include-href-fragment-xml-id-preserve-ns.xml`
- `iso-schema-composition/include-href-fragment-xml-id.xml`

**[#575](https://github.com/dailydevops/schemas/issues/575)** (1 case): An extends with href needs a library that holds exactly one abstract rule; a rule document with a recursive extends is not accepted (NES3101).

- `iso-schema-composition/extends-recursive.xml`

**[#832](https://github.com/dailydevops/schemas/issues/832)** (2 cases): The keys of a schema build, but the XPath of a rule cannot call them yet (#832).

- `iso-rule-based-validation/xsl-key-element-content.xml`
- `iso-rule-based-validation/xsl-key.xml`

**[#877](https://github.com/dailydevops/schemas/issues/877)** (1 case): A let with element content binds a document node (maintainer decision of #856), and the run reports NES3204 until the embedded content is wired (#914).

- `iso-rule-based-validation/let-value-element-content.xml`

**[#913](https://github.com/dailydevops/schemas/issues/913)** (7 cases): The documents attribute is ignored: no subordinate document is validated and the SVRL does not name it.

- `iso-instance-document-selection/pattern-subordinate-document-variable.xml`
- `iso-instance-document-selection/pattern-subordinate-document.xml`
- `iso-pattern-templating/pattern-abstract-pattern-documents.xml`
- `iso-reporting/svrl-active-pattern-documents-primary.xml`
- `iso-reporting/svrl-active-pattern-documents.xml`
- `iso-reporting/svrl-fired-rule-document-primary.xml`
- `iso-reporting/svrl-fired-rule-document.xml`

**[#914](https://github.com/dailydevops/schemas/issues/914)** (2 cases): A property with xsl:copy-of is reported NES3204 because no embedded content is wired into the pipeline.

- `iso-reporting/svrl-property-copy-of.xml`
- `iso-reporting/svrl-property-variable-copy-of-rule.xml`

**[#915](https://github.com/dailydevops/schemas/issues/915)** (4 cases): Abstract pattern parameters are not substituted in name paths and in diagnostic and property content (NES3300).

- `iso-pattern-templating/pattern-abstract-diagnostic-value-of.xml`
- `iso-pattern-templating/pattern-abstract-name-path.xml`
- `iso-pattern-templating/pattern-abstract-property-copy-of.xml`
- `iso-pattern-templating/pattern-abstract-property-value-of.xml`

**[#916](https://github.com/dailydevops/schemas/issues/916)** (6 cases): The SVRL text of a diagnostic or property reference lacks fpi, icon, and see.

- `iso-reporting/svrl-diagnostic-fpi.xml`
- `iso-reporting/svrl-diagnostic-icon.xml`
- `iso-reporting/svrl-diagnostic-see.xml`
- `iso-reporting/svrl-property-fpi.xml`
- `iso-reporting/svrl-property-icon.xml`
- `iso-reporting/svrl-property-see.xml`

**[#918](https://github.com/dailydevops/schemas/issues/918)** (2 cases): The SVRL property-reference lacks role and scheme.

- `iso-reporting/svrl-property-role.xml`
- `iso-reporting/svrl-property-scheme.xml`

**[#919](https://github.com/dailydevops/schemas/issues/919)** (1 case): The message of sch:name with a path holds the node name, the case expects the value.

- `iso-reporting/svrl-name-path.xml`

**[#920](https://github.com/dailydevops/schemas/issues/920)** (3 cases): White space only text around value-of is kept in the message, the case expects it stripped.

- `iso-pattern-templating/pattern-abstract-pattern-let-value.xml`
- `iso-pattern-templating/pattern-abstract-rule-let-value.xml`
- `iso-pattern-templating/pattern-abstract-value-of.xml`

**[#921](https://github.com/dailydevops/schemas/issues/921)** (1 case): An extends of an abstract rule of another pattern builds, the case expects an error.

- `iso-schema-composition/rule-abstract-scope-error.xml`

### EN 16931 golden tests

The CEN EN 16931 validation artefacts, release 1.3.17, are a real rule set. The archive is pinned by checksum in [`tests/conformance/suites/cen-en16931.json`](https://github.com/dailydevops/schemas/blob/main/tests/conformance/suites/cen-en16931.json) and is fetched, never committed. The cases are every `test` of the unit-test corpus and every official example of the UBL and CII syntaxes, and each case compares the rules that fire with the expected ones.

**1,174 of 1,174 cases pass, a rate of 100.0%** on `net10.0`; no case is baselined or excluded. The job summary publishes the row `NetEvolve.Schemas.Schematron (EN 16931 goldens)`.

```bash
dotnet test --project tests/NetEvolve.Schemas.Schematron.Tests.Integration --configuration Release --framework net10.0 --treenode-filter "/*/*/En16931GoldenTests/*"
```

## Limitations

- **Compilers older than C# 14.** `SchemaValidator.ForSchematron(...)` is a C# 14 extension member. A compiler without extension member syntax calls the public static extension class directly, named `SchematronSchemaValidatorExtensions` and declared in the namespace `NetEvolve.Schemas`. It has the same overloads and returns the same builder:

  ```csharp
  var builder = SchematronSchemaValidatorExtensions.ForSchematron(schema);
  ```

- **No `ReadOnlyMemory<T>` overload.** With one, a `byte[]` or `char[]` argument is ambiguous for compilers older than C# 14; pass `memory.Span` instead.
- **XSLT in a schema.** Declarations of XSLT build and stylesheet functions are callable from XPath, but keys are not callable from a rule yet ([#832](https://github.com/dailydevops/schemas/issues/832)), and `let` with element content and `xsl:copy-of` in a property are reported at run time ([#914](https://github.com/dailydevops/schemas/issues/914)); see [XSLT constructs](#xslt-constructs).
- **Attributes that are accepted and ignored.** The `documents` attribute of a pattern ([#913](https://github.com/dailydevops/schemas/issues/913), feature [#388](https://github.com/dailydevops/schemas/issues/388)) and the `visit-each` attribute of a rule ([#934](https://github.com/dailydevops/schemas/issues/934), feature [#387](https://github.com/dailydevops/schemas/issues/387)) neither fail the build nor take effect.
- **Phase attributes.** A selected phase with `from` ([#385](https://github.com/dailydevops/schemas/issues/385)) or `when` ([#386](https://github.com/dailydevops/schemas/issues/386)) fails the build with `NES3007`.
- **Include and extends.** An `href` with a fragment identifier is not supported ([#575](https://github.com/dailydevops/schemas/issues/575)), and an `extends` of an abstract rule of another pattern builds without an error ([#921](https://github.com/dailydevops/schemas/issues/921)).
- **SVRL.** The attributes `fpi`, `icon`, and `see` of a diagnostic or property reference ([#916](https://github.com/dailydevops/schemas/issues/916)), and the attributes `role` and `scheme` of a property reference ([#918](https://github.com/dailydevops/schemas/issues/918)). An SVRL `id` is written only for an author id, and a `svrl:error` in the output is not a failure of the run.
- **Messages.** White space only text around `value-of` is kept in a message ([#920](https://github.com/dailydevops/schemas/issues/920)), `sch:name` with a path outputs the node name ([#919](https://github.com/dailydevops/schemas/issues/919)), and abstract pattern parameters are not substituted in name paths and in diagnostic and property content ([#915](https://github.com/dailydevops/schemas/issues/915)).
- **Locations.** A problem of an expression is located by the base URI of the schema and by the line and column within the text of the expression, and a dynamic problem has no line and column ([#881](https://github.com/dailydevops/schemas/issues/881)). `last()` in a predicate without a known size equals `position()` ([#883](https://github.com/dailydevops/schemas/issues/883)).
- **Thread safety.** A validator is immutable and thread-safe; a `WithReportSink` view and a builder are not.
- **Compile cost.** Every `Build` compiles the schema once; a validation compiles nothing.

## Requirements

- .NET 10 or higher, or any runtime that implements .NET Standard 2.0 (the package targets `netstandard2.0`, `net10.0`, and `net11.0`; the tests run on `net5.0`, `net10.0`, and `net11.0`, and `net5.0` runs the `netstandard2.0` build)
- `NetEvolve.Schemas.XPath`, `NetEvolve.Schemas.Xml`, `NetEvolve.Schemas`, and `NetEvolve.Arguments` as dependencies; on `netstandard2.0` also `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`
- The call `SchemaValidator.ForSchematron(...)` needs C# 14; older compilers use the named class, see [Limitations](#limitations)

## Related Packages

- [**NetEvolve.Schemas**](https://www.nuget.org/packages/NetEvolve.Schemas/) - Core abstractions: validation limits, resource resolvers, and the validator anchor
- [**NetEvolve.Schemas.Xml**](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/) - XML 1.0 reader and the document tree that the documents are read into
- [**NetEvolve.Schemas.XPath**](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/) - The XPath engine that runs the expressions of a schema
- [**NetEvolve.Schemas.Xsd**](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/) - XML Schema (XSD) validation
- [**NetEvolve.Schemas.DependencyInjection**](https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/) - Registration of the validators with `Microsoft.Extensions.DependencyInjection`

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
