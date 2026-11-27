# NetEvolve.Schemas.Xsd

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.Xsd.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.Xsd.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

Compiles XML Schema (XSD) documents into an immutable, thread-safe validator and validates XML instances against it, from characters, bytes, sequences, streams, or an already loaded document tree. XSD 1.0 is supported, with a measured conformance rate. XSD 1.1 is the default version but is incomplete: a 1.1 construct that is not wired in yet fails the schema build with a coded "not supported yet" error.

This readme describes the state of the main branch at the time of writing; the sections [Conformance](#conformance) and [Known limitations](#known-limitations) say what is missing. No package has been released yet; the install lines apply from the first release.

## Features

- XSD 1.0 schema compilation - include, import, redefine, identity constraints, wildcards, derivation checks, and the built-in datatypes
- XSD 1.0 instance validation - content models, attributes, `xsi:type`, `xsi:nil`, `ID` and `IDREF`, identity constraints, and `ENTITY` values
- Version selection per validator - `WithVersion(XsdVersion.V10)` is strict XSD 1.0; the default is XSD 1.1, which is incomplete (see [Version selection](#version-selection))
- Tree validation with an annotated result - `ValidateTree` validates a loaded document tree and returns the issues with the validity, the type, and the defaulted attributes of every node
- Schema location hints - `xsi:schemaLocation` and `xsi:noNamespaceSchemaLocation` of an instance can load schema documents through the resolver; off by default
- Secure by default - every external resource is refused unless a resolver is set, a document type declaration of an instance is rejected unless enabled, and every run is bounded by the validation limits
- Coded diagnostics - every issue carries a stable code (`NES4xxx`), a severity, the constraint, and the line and column

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas.Xsd
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas.Xsd
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas.Xsd" Version="x.x.x" />
```

## Quick Start

`SchemaValidator.ForXsd()` creates the builder. Add the schema documents, build the validator once, and validate any number of instances with it. A failed constraint is an issue with severity `Error`, a stable code, a message, and a location; the `systemId` of the instance is the source of the location. A schema with an error never yields a validator: `Build` and `BuildAsync` throw one `SchemaCompilationException` that carries every issue.

The samples in this readme run in an async method that has a `CancellationToken cancellationToken`. They need these namespaces:

```
using System;
using System.Collections.Generic;
using System.IO;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
```

```csharp
const string schema = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:element name="order">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="quantity" type="xs:positiveInteger"/>
          </xs:sequence>
          <xs:attribute name="id" type="xs:ID" use="required"/>
        </xs:complexType>
      </xs:element>
    </xs:schema>
    """;

var validator = await SchemaValidator.ForXsd().AddSchema(schema.AsMemory()).BuildAsync(cancellationToken);
var result = await validator.ValidateAsync(
    """<order id="o1"><quantity>0</quantity></order>""".AsMemory(),
    "file:///orders/1.xml",
    cancellationToken
);
```

Here `result.IsValid` is `false`, and the single issue has the code `NES4625` (a value below the minimum of the type) at line 1 of `file:///orders/1.xml`.

## Usage

### Input kinds

The schema is added with `AddSchema`, as `ReadOnlyMemory<char>`, `ReadOnlyMemory<byte>`, or a `Stream`; the `systemId` is the base URI of the document, which `include` and `import` resolve against. `ReadOnlyMemory` input is copied, so the caller may reuse its buffer after `AddSchema` returns. A stream is read from its current position at `Build` or `BuildAsync` and is never disposed. The bytes of a stream are kept, so a second `Build` does not read it again, even if it cannot seek. `AddSchemaLocation(reference, namespaceName)` names a document that `Build` loads through the resolver as `ResourceKind.SchemaDocument`, with the namespace in `ResourceRequest.NamespaceUri`; a document with another target namespace is a schema error. An instance is validated with `Validate` (synchronous) or `ValidateAsync` from a `Stream`, `ReadOnlyMemory<char>`, `ReadOnlyMemory<byte>`, `ReadOnlySequence<char>`, or `ReadOnlySequence<byte>`; the synchronous overloads also take spans, and a `string` binds to the `ReadOnlySpan<char>` overload.

In the sample, `schemaStream` and `instanceStream` are streams over the schema document and over `<order>7</order>`.

```csharp
var validator = await SchemaValidator
    .ForXsd()
    .AddSchema(schemaStream, "file:///order.xsd")
    .BuildAsync(cancellationToken);

var fromStream = await validator.ValidateAsync(instanceStream, "file:///order.xml", cancellationToken);
var fromBytes = validator.Validate("<order>seven</order>"u8, "file:///order.xml", cancellationToken);
```

Here `fromStream` is valid, and `fromBytes` has one issue with the code `NES4616` (a value that is not of the type). The validator is immutable and thread-safe; the builder is not.

### Version selection

The version is a property of the validator, set with `WithVersion` before the build and readable as `Version`:

| Selection | Meaning |
| --- | --- |
| `XsdVersion.V11` (the default) | XSD 1.1, which is incomplete. A 1.1 construct that is not wired in yet is a schema error with a code from `NES4066` to `NES4077` ("not supported yet"), so a schema never silently loses a construct. |
| `XsdVersion.V10` | Strict XSD 1.0. Every XSD 1.1 construct is a schema error with a code from `NES4020` to `NES4035` ("not allowed in strict 1.0 mode"). |

The sample builds a schema with a `defaultAttributesApply` attribute, an XSD 1.1 construct that is not wired in yet, under both versions:

```csharp
const string schema = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:complexType name="plain" defaultAttributesApply="false"/>
    </xs:schema>
    """;

var codes = new List<string>();
foreach (var version in new[] { XsdVersion.V11, XsdVersion.V10 })
{
    try
    {
        _ = await SchemaValidator
            .ForXsd()
            .WithVersion(version)
            .AddSchema(schema.AsMemory())
            .BuildAsync(cancellationToken);
    }
    catch (SchemaCompilationException exception)
    {
        codes.Add($"{version}: {exception.Issues[0].Code}");
    }
}
```

Here `codes` holds `V11: NES4067` and `V10: NES4027`. A validator for XSD 1.0 is built like this:

```csharp
var validator = await SchemaValidator
    .ForXsd()
    .WithVersion(XsdVersion.V10)
    .AddSchema(schema.AsMemory())
    .BuildAsync(cancellationToken);
```

The default is XSD 1.1, and it stays incomplete until the XSD 1.1 wiring is done (#360); see [XSD 1.1 status](#xsd-11-status). A schema that uses only XSD 1.0 constructs builds and validates under both versions.

### Streaming and tree validation

Every `Validate` and `ValidateAsync` call runs one pass over the events of the reader against the schema that was compiled once. Only the paths that need a document tree build one:

| Feature | Runs | Notes |
| --- | --- | --- |
| Content models, attributes, datatypes, `xsi:type`, `xsi:nil`, wildcards | streaming | No tree is built. |
| `ID`, `IDREF`, and `ENTITY` values | streaming | The check exists only when the schema names a type that needs it. |
| Identity constraints (`key`, `keyref`, `unique`) | streaming | The selector and field paths are evaluated on the events. |
| Assertions (`xs:assert`) | needs a subtree | Part of XSD 1.1: the subtree of an element with assertions is buffered and the assertions are evaluated at its end tag. A violation is reported as `cvc-assertion`. |
| `ValidateTree` with the annotated result | tree | Validates a loaded `XmlTree` or one element of it, and returns a copy of the content with the annotations. |

A `Stream` input is read completely into pooled memory (up to `MaxInputSize`) before the run starts: the pass over the events is single, but the input is not parsed incrementally.

`IXsdValidator.ValidateTree` takes a document tree of the `NetEvolve.Schemas.Xml` package and returns an `XsdAnnotatedResult`: the `Result` with the issues, which equal those of the streaming path for the same content, and the `Document`, a copy of the tree whose nodes carry the annotations. `TryGetAnnotation` reads the validity, the type name (an anonymous type has no name), whether the element is nilled, and the typed value of a node. The copy also holds what the schema adds: a defaulted attribute is an attribute node after the specified ones, and `IsSpecified` is `false` for it.

```csharp
var tree = XmlDocumentLoader.Load("<order><quantity>12</quantity></order>".AsSpan());

var annotated = validator.ValidateTree(tree, cancellationToken);

var order = annotated.Document.Document.FirstChild;
var found = annotated.TryGetAnnotation(order, out var annotation);
```

Here `found` is `true`, `annotation.Validity` is `XsdValidity.Valid`, and the defaulted attribute `priority` is the first attribute of `order`, with the value `3` and `IsSpecified` `false`. `ValidateTree(tree, element)` validates the subtree that starts at one element. The tree path does not read schema location hints, and it sees no document type declaration, because the document model keeps none (#731).

### Compilers older than C# 14

`SchemaValidator.ForXsd()` is a C# 14 extension member. A compiler without extension member syntax calls the public static extension class directly, named `XsdSchemaValidatorExtensions` and declared in the namespace `NetEvolve.Schemas`. It returns the same builder:

```csharp
var builder = XsdSchemaValidatorExtensions.ForXsd();
```

### Schema location hints

An instance can name schema documents with `xsi:schemaLocation` and `xsi:noNamespaceSchemaLocation`. By default the hints are ignored and no location is accessed. `WithSchemaLocationHints(true)` loads the named documents through the resolver of the builder, which refuses everything unless another one is set. In the sample, `directory` holds the file `order.xsd`, a schema for an `order` element of type `xs:int`.

```csharp
var validator = await SchemaValidator
    .ForXsd()
    .WithSchemaLocationHints(true)
    .WithResolver(new SandboxedFileSystemResourceResolver(directory))
    .BuildAsync(cancellationToken);

var result = await validator.ValidateAsync(
    """
    <order xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
           xsi:noNamespaceSchemaLocation="order.xsd">seven</order>
    """.AsMemory(),
    new Uri(Path.Combine(directory, "order.xml")).AbsoluteUri,
    cancellationToken
);
```

Here the hinted schema is loaded from `directory`, and the result has one issue with the code `NES4616`. A hinted schema document is compiled on its own: it cannot refer to components of the configured schema, and its identity constraints are not enforced.

## Configuration

### Resolver defaults

A schema document with `include`, `import`, `redefine`, or `override` reads the documents it names through the `IResourceResolver` of the builder. The default is `DenyAllResourceResolver`: every request is refused with the code `NES0100`, so no schema document and no hint can reach the file system or the network unless the caller sets a resolver with `WithResolver`. The package `NetEvolve.Schemas` ships `SandboxedFileSystemResourceResolver`, which serves files below one root directory and refuses everything outside it. In the sample, `directory` holds `main.xsd`, which includes `types.xsd`.

```csharp
var path = Path.Combine(directory, "main.xsd");
var main = new ReadOnlyMemory<byte>(await File.ReadAllBytesAsync(path, cancellationToken));
var systemId = new Uri(path).AbsoluteUri;

string? deniedCode = null;
try
{
    _ = await SchemaValidator.ForXsd().AddSchema(main, systemId).BuildAsync(cancellationToken);
}
catch (SchemaCompilationException exception)
{
    deniedCode = exception.Issues[0].Code.ToString();
}

var validator = await SchemaValidator
    .ForXsd()
    .WithResolver(new SandboxedFileSystemResourceResolver(directory))
    .AddSchema(main, systemId)
    .BuildAsync(cancellationToken);
var result = validator.Validate("<order>7</order>", null, cancellationToken);
```

Here `deniedCode` is `NES0100`, and the validator that has the resolver is built and accepts the instance.

### Limits

`WithLimits` takes a `ValidationLimits`; every limit is finite and bounded by a ceiling. A run that exceeds a limit stops with a core code (`NES0001` to `NES0007`) instead of exhausting memory.

| Limit | Default |
| --- | --- |
| `MaxIssues` | 1,000 (a larger result is truncated, and `IsTruncated` is `true`) |
| `MaxInputSize` | 64 MiB (bytes for byte and stream input, characters for char input) |
| `MaxDepth` | 256 |
| `MaxAttributesPerElement` | 1,024 |
| `MaxEntityExpansion` | 10,000,000 characters |
| `MaxEntityDepth` | 16 |
| `MaxExternalResources` | 16 per validation or compilation |
| `MaxResourceSize` | 16 MiB per resolved resource and per schema document given to `AddSchema` |

```csharp
var validator = await SchemaValidator
    .ForXsd()
    .WithLimits(ValidationLimits.Default.WithMaxIssues(1).WithMaxDepth(32))
    .AddSchema(schema.AsMemory())
    .BuildAsync(cancellationToken);

var result = validator.Validate("<list><n>a</n><n>b</n></list>", null, cancellationToken);
```

Here the result has one issue, and `IsTruncated` is `true`.

A schema document over `MaxResourceSize` ends the build with a `SchemaCompilationException` whose issue has the code `NES0007`. Before the schema sources moved to the shared `SchemaSourceHolder`, the same condition was reported as `NES0001` against `MaxInputSize`; `MaxInputSize` now bounds instances only.

The documents that a schema names with `include`, `import`, `redefine`, or `override`, and the schema location hints of an instance, are loaded by the shared `ResourceLoader` of `NetEvolve.Schemas`, which counts every request against `MaxExternalResources` and caps each resource at `MaxResourceSize` with `NES0007`. A relative location without an absolute base URI is still passed to the resolver, with `ResolvedUri` set to `null`, so a custom resolver can serve it; the default resolver refuses it with `NES0100`.

### Document type declarations

A document type declaration in an instance ends the run with a reader error by default. `WithDtdProcessing(true)` reads the internal subset, so `ENTITY` and `ENTITIES` values are checked against its unparsed entity declarations. Only the internal subset is read; the entities of an external subset are not visible.

## Requirements

- .NET 10.0 or higher, or any runtime that implements .NET Standard 2.0 (the tests also run the `netstandard2.0` build on .NET 9)
- The package targets `netstandard2.0`, `net10.0`, and `net11.0`
- Depends on `NetEvolve.Schemas`, `NetEvolve.Schemas.Xml`, `NetEvolve.Schemas.XPath`, and `NetEvolve.Arguments`; on `netstandard2.0` it also depends on `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`
- The call `SchemaValidator.ForXsd()` needs C# 14; older compilers use the named class (see [Compilers older than C# 14](#compilers-older-than-c-14))

## Related Packages

- [**NetEvolve.Schemas**](https://www.nuget.org/packages/NetEvolve.Schemas/) - The format-agnostic core: validator contract, result model, validation limits, and resource resolvers
- [**NetEvolve.Schemas.Xml**](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/) - XML 1.0 reader, document tree, datatypes, and DTD validation
- [**NetEvolve.Schemas.XPath**](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/) - The XPath engine behind identity constraints and assertions
- [**NetEvolve.Schemas.Schematron**](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/) - ISO Schematron validation with SVRL output
- [**NetEvolve.Schemas.DependencyInjection**](https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/) - Registration of the validators with `Microsoft.Extensions.DependencyInjection`

## Documentation

For complete documentation, please visit the [official documentation](https://github.com/dailydevops/schemas/blob/main/README.md).

### XSD 1.1 status

XSD 1.1 is the default version, and it is incomplete until the 1.1 wiring (#360) is done. The package holds the building blocks of several 1.1 features (conditional inclusion), but the builder does not enable them yet. Open content is wired: the builder compiles `xs:openContent` (modes `interleave`, `suffix`, and `none`) and the `xs:defaultOpenContent` of the schema document (with `appliesToEmpty`) into the complex type definitions, and the validator accepts the wildcard elements in the places that the mode allows; an extension that narrows or re-modes inherited open content, and a restriction that allows more open content than its base type, are located schema errors (`NES4063` and `NES4064` are no longer reported). Assertions on complex types (`xs:assert`), the assertion facet of simple types (`xs:assertion`), the `xs:explicitTimezone` facet of the date and time types, the override of schema components (`xs:override`), the negated wildcards (the `notNamespace` and `notQName` attributes of `xs:any` and `xs:anyAttribute`), and the type alternatives (`xs:alternative` with the `inheritable` attribute of attribute declarations) are wired. The `targetNamespace` attribute of local element and attribute declarations, the `ref` attribute of `xs:key`, `xs:unique`, and `xs:keyref`, and a list of several substitution group heads are wired too (#1429, #824): the builder compiles them and the validator accepts a member of several groups wherever any head is allowed. The builder compiles `xs:assert`, `xs:assertion` and `xs:explicitTimezone` and the validator evaluates them (at the end tag of the element for `xs:assert`, with the value of the element or attribute bound to `$value` for `xs:assertion`), with the limitations of the assertion listener and the facet evaluator (the `xpathDefaultNamespace` attribute is honored on `xs:assert`, `xs:assertion`, and the `xs:selector` and `xs:field` of identity constraints, with `xs:schema` as the default; on `xs:alternative` too; the assertions of list items and union members are not checked, #949). The builder compiles the `xs:alternative` children of an element declaration into its type table, and the validator selects the type when the attributes of the element are complete, before `xsi:type` is applied: the first alternative whose `test` is true wins, then the alternative without a `test`, then the declared type. A `test` sees the attributes of the element, including the attributes that an ancestor declared `inheritable`, and nothing else (no child, no parent). A `test` that raises a dynamic error is false (Structures 1.1, section 3.12.4), a `test` that is not valid XPath and a type that is not derived from the declared type are located schema errors (`xpath-valid`, `ta-props-correct`), and `xsi:type` must be derived from the selected type. Limitations: the `test` is compiled with the full XPath 2.0 of the XPath package, not the subset that Structures 1.1 requires at least; the attributes that a default adds are not visible to a `test`; the type `xs:error` cannot be named yet (`NES4077`). `xs:override` is a compile-time transformation of the schema documents, so nothing evaluates it, and an override that fails to compile is a located schema error (`NES4065` is no longer reported for it). A schema that uses one of these constructs under the default version fails the build with a coded, located schema error that states the construct is "not supported yet": `NES4066` for `defaultAttributes`, and the codes up to `NES4077` for the other constructs, among them the datatypes `xs:precisionDecimal` and `xs:error`. Strict `V10` reports the same constructs with the codes `NES4020` to `NES4035`. The attributes of the version control namespace (`vc:`) are not applied yet, so a construct inside an excluded block is reported as well. No XSD 1.1 conformance rate is claimed: the cases of XSD 1.1 of the test suite are all baselined as known failures until the wiring is done, and the XSD 1.1 conformance statement is a work package of its own.

### Conformance

The conformance run executes the W3C XML Schema Test Suite (XSTS), pinned by checksum in `tests/conformance/suites/xsts.json`; no part of the suite is committed. It selects the accepted cases of XSD 1.0 and runs the schema tests through the schema compiler and the instance tests through the validator, both in strict `V10` mode. The rate is passed / (passed + known failures), truncated to one decimal, as the Conformance workflow publishes it in its job summary for the rows `NetEvolve.Schemas.Xsd XSTS 1.0 schema tests` and `NetEvolve.Schemas.Xsd XSTS 1.0 instance tests`.

| Part | Applicable | Passed | Known failures | Rate |
| --- | --- | --- | --- | --- |
| Schema tests | 14364 | 14329 | 35 | 99.7% |
| Instance tests | 25017 | 24073 | 944 | 96.2% |
| Both | 39381 | 38402 | 979 | 97.5% |

The last row adds the two published rows; the job summary shows no row for the sum. The selection also leaves 104 excluded cases of XSD 1.0 out of these counts: cases whose status is not accepted, whose expected outcome is not definite, or that depend on a processor feature that the runner does not model. Every known failure is a case of the committed baseline in `tests/conformance/baselines/xsts/`, and it is listed with its reason and, where a bug issue exists, its issue in `tests/NetEvolve.Schemas.Xsd.Tests.Integration/SchemaTests/known-failures.txt` and `tests/NetEvolve.Schemas.Xsd.Tests.Integration/InstanceTests/known-failures.txt`. A regression and an unexpected pass fail the run. To reproduce the numbers, run the conformance tests of the integration project on one leg, with a file in `GITHUB_STEP_SUMMARY` to receive the rows:

```powershell
$env:GITHUB_STEP_SUMMARY = "$PWD/summary.md"
dotnet test --project tests/NetEvolve.Schemas.Xsd.Tests.Integration --configuration Release --framework net10.0 --treenode-filter "/*/*Conformance*/*/*"
```

The summary file then also holds the row `NetEvolve.Schemas.Xsd`, which counts the cases of XSD 1.0 and XSD 1.1 together (47.4%, because the cases of XSD 1.1 are baselined as failures). The tests `XsdReadmeConformanceTests` keep the table above equal to the selection and to the known-failures lists.

### Known limitations

- XSD 1.1 is the default but incomplete: see [XSD 1.1 status](#xsd-11-status). The wiring of the 1.1 features is #360, and the assertions of complex types and the assertion facet and the explicitTimezone facet of simple types are wired (#318, #1191, #1430).
- Incomplete schemas are rejected: a schema with a reference to a component that is not there fails the build, instead of failing only where the component is used (#956).
- The enumeration facet of a restriction is not evaluated when instance values are validated for list types (#1077), union types (#1078), and hexBinary and base64Binary (#1079); the other facets of a user-defined simple type are evaluated.
- Substitution groups are not supported yet (#868) and circular redefinitions are not resolved (#1418). A group (XSD 1.0) or attribute group that is redefined without a reference to itself is checked against the original. `xs:include`, `xs:import`, and `xs:redefine` otherwise work through the resolver.
- The datatype `xs:precisionDecimal` is not supported (#64).
- The model of the document tree holds no document type declaration, so a tree validation can differ from the streaming path for an instance that depends on one (#731); the XPath evaluator reads typed values only through the annotations (#791).
- A `Stream` input is buffered completely before the run, see [Streaming and tree validation](#streaming-and-tree-validation).

The differences to `System.Xml.Schema` that the differential run finds over the XSTS cases of XSD 1.0 are listed in the [deviation list](https://github.com/dailydevops/schemas/blob/main/docs/xsd/system-xml-schema-deviations.md): each is classified as a place where this library follows the specification, an equivalent difference in issue positions, or a defect that a bug issue tracks.

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
