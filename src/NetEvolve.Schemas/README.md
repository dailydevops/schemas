# NetEvolve.Schemas

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.svg)](https://www.nuget.org/packages/NetEvolve.Schemas/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.svg)](https://www.nuget.org/packages/NetEvolve.Schemas/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

The format-agnostic core of the NetEvolve schema libraries: the validator contract, the result model, validation limits, and external resource resolvers. Format packages build on it to read and validate XML-based schema formats; it does not read any schema format by itself.

## Features

- Validator contract - `ISchemaValidator` validates a stream, a span, memory, or a sequence of bytes or characters, synchronously or asynchronously, see [Quick Start](#quick-start)
- Result model - an immutable `ValidationResult` of `ValidationIssue` entries with stable diagnostic codes and one-based document locations, see [Result model](#result-model)
- Bounded validation - `ValidationLimits` with finite defaults and ceilings, see [Configuration](#configuration)
- Deny-by-default resources - external resources are refused unless an `IResourceResolver` is set, and `SandboxedFileSystemResourceResolver` serves `file` URIs below one root directory, see [External resources](#external-resources)
- Building blocks for format packages - `SchemaValidatorBuilder<TBuilder, TValidator>`, `SchemaValidatorBase`, `DocumentSource`, and `ValidationIssueCollector`, see [Writing a format package](#writing-a-format-package)
- Diagnostic code ranges - `DiagnosticCode` (printed as `NES` and four digits) and the `DiagnosticRanges` of each package, see [Diagnostic codes](#diagnostic-codes)

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas" Version="x.y.z" />
```

No package has been released yet; the lines apply from the first release, and `x.y.z` stands for the released version.

## Quick Start

`SchemaValidator` is the anchor for the format entry points. A format package adds a `For<Format>()` method to it as a C# 14 extension member, which returns a fluent builder:

- `WithLimits` sets the `ValidationLimits`.
- `WithResolver` sets the `IResourceResolver` for external resources.
- `Build` and `BuildAsync` create an immutable, thread-safe `ISchemaValidator`.

`ISchemaValidator` validates a `Stream`, `ReadOnlySpan<byte>`, `ReadOnlySpan<char>` (a `string` binds to it), `ReadOnlySequence<byte>`, or `ReadOnlySequence<char>` synchronously, and a `Stream`, `ReadOnlyMemory<byte>`, `ReadOnlyMemory<char>`, `ReadOnlySequence<byte>`, or `ReadOnlySequence<char>` asynchronously. The optional `systemId` is the base URI of the document and appears in the locations of the issues.

The samples in this readme use `ForFake()`, a placeholder for the entry point of a real format. They need these namespaces:

```
using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.FileSystem;
```

```csharp
var limits = ValidationLimits.Default.WithMaxDepth(64).WithMaxIssues(100);
var validator = await SchemaValidator
    .ForFake()
    .WithLimits(limits)
    .WithResolver(new SandboxedFileSystemResourceResolver("schemas"))
    .BuildAsync(cancellationToken);

await validator.ValidateAsync("<root/>".AsMemory(), "file:///documents/root.xml", cancellationToken);
```

## Usage

### Result model

A validation returns a `ValidationResult`. It is immutable and thread-safe.

| Member | Meaning |
| --- | --- |
| `IsValid` | `true` when there is no error and no dropped error. |
| `Issues` | The `ValidationIssue` list, at most `MaxIssues` entries. |
| `IsTruncated` | `true` when the issue list is not exhaustive. |
| `HasDroppedErrors` | `true` when at least one dropped issue had severity `Error`. |

A `ValidationIssue` carries a stable `DiagnosticCode` (printed as `NES` and four digits), an `IssueSeverity`, a message, a `DocumentLocation` (one-based line and column, zero meaning unknown), and the constraint identifier of the specification when there is one. A schema that cannot be compiled throws a `SchemaCompilationException` that carries the compilation issues.

The following builds a result by hand to show the model:

```csharp
var issue = new ValidationIssue(
    CoreDiagnosticCodes.DepthLimitExceeded,
    IssueSeverity.Error,
    "The element depth exceeds the limit of 64.",
    new DocumentLocation("file:///documents/root.xml", 3, 14)
);
var result = new ValidationResult([issue]);

var isValid = result.IsValid;
var first = result.Issues[0].ToString();
```

Here `isValid` is `false` and `first` is `NES0002 Error file:///documents/root.xml(3,14): The element depth exceeds the limit of 64.`.

### External resources

External resources are deny by default. Unless a resolver is configured, every request for an external resource is refused: the builder uses `DenyAllResourceResolver`, which never opens anything, and the refusal is reported with the diagnostic code `NES0100`.

To allow resources, pass an `IResourceResolver` to `WithResolver`. The package ships `SandboxedFileSystemResourceResolver`, which serves `file` URIs below one root directory and refuses everything else. Any other resolver implements `IResourceResolver`, which must be thread-safe when the validator is shared and must report a refusal through its result instead of throwing.

```csharp
var resolver = SchemaValidator.ForFake().Resolver;
var request = new ResourceRequest(
    ResourceKind.SchemaDocument,
    "types.xsd",
    "file:///schemas/main.xsd",
    "file:///schemas/types.xsd"
);

using var resolved = resolver.Resolve(in request, cancellationToken);
```

Here `resolved.Status` is `ResourceStatus.Denied`.

### Writing a format package

A format package derives its builder from `SchemaValidatorBuilder<TBuilder, TValidator>` and its validator from `SchemaValidatorBase`. The base class funnels every `Validate` and `ValidateAsync` overload into one `ValidateCore` method that receives a `DocumentSource`: a `ref struct` that exposes the input as bytes or characters, as a span or a sequence, with its `Kind`, `Length`, and `SystemId`. A `ValidationIssueCollector` implements the drop policy of `MaxIssues` once, so no format handles it itself: it keeps at most `MaxIssues` issues, records a drop in `IsTruncated` and `HasDroppedErrors`, and `Stop` appends a final error issue and ignores the rest.

### Diagnostic codes

`DiagnosticCode` wraps a number and prints it as `NES` and four digits. `DiagnosticRanges` assigns each package a range, and `CoreDiagnosticCodes` holds the codes of this package.

| Range | Owner |
| --- | --- |
| 1-999 | `NetEvolve.Schemas` (`DiagnosticRanges.Core`) |
| 1000-1999 | `NetEvolve.Schemas.Xml` |
| 2000-2999 | `NetEvolve.Schemas.XPath` |
| 3000-3999 | `NetEvolve.Schemas.Schematron` |
| 4000-4999 | `NetEvolve.Schemas.Xsd` |
| 5000-5999 | `NetEvolve.Schemas.DependencyInjection` |
| 6000-8999 | Reserved |
| 9000-9999 | Consumers (`DiagnosticRanges.Consumer`) |

## Configuration

The limits of a validation are `ValidationLimits`, which is immutable. Every limit is finite, and the limits that have a ceiling reject larger values. `ValidationLimits.Default` applies unless a builder gets other limits through `WithLimits`. A `With...` method returns a copy.

| Limit | Default | Ceiling |
| --- | --- | --- |
| `MaxIssues` | 1,000 | none |
| `MaxInputSize` | 64 MiB | 2,000,000,000 |
| `MaxDepth` | 256 | 10,000 |
| `MaxAttributesPerElement` | 1,024 | none |
| `MaxEntityExpansion` | 10,000,000 | none |
| `MaxEntityDepth` | 16 | 1,000 |
| `MaxExternalResources` | 16 | none |
| `MaxResourceSize` | 16 MiB | 2,000,000,000 |

`MaxInputSize` counts bytes for byte and stream input and characters for character input. `MaxEntityExpansion` counts characters. `MaxResourceSize` counts bytes of one resolved resource, and `MaxExternalResources` counts the resources of one validation or compilation. The defaults are provisional until they are benchmarked.

## Limitations

- **Compilers older than C# 14.** `SchemaValidator.For<Format>()` is a C# 14 extension member. A compiler without extension member syntax calls the public static extension class of the format package directly, named `<Format>SchemaValidatorExtensions` and declared in the namespace `NetEvolve.Schemas`:

  ```csharp
  var builder = FakeSchemaValidatorExtensions.ForFake();
  ```

- **Provisional limit defaults.** The defaults of `ValidationLimits` are provisional until they are benchmarked.
- **Sandboxed file system resolver.** The file system can change between the check and the open (time of check to time of use). Case sensitivity follows the host. A name with a colon is refused on every platform. A symbolic link or junction is served only when its final target is below the root, and a link whose target cannot be determined is refused. The root directory itself is trusted and never checked for links.
- **Placeholder samples.** The samples use a placeholder format because they exercise the core package only.

## Requirements

- .NET Standard 2.0, .NET 10, or .NET 11 (the package targets `netstandard2.0`, `net10.0`, and `net11.0`; the tests run on `net5.0`, `net10.0`, and `net11.0`, and `net5.0` runs the `netstandard2.0` build)
- `NetEvolve.Arguments` as a dependency; on `netstandard2.0` also `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`

## Related Packages

- [**NetEvolve.Schemas.Xml**](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/) - XML 1.0 reader and document tree with a line and column on every node
- [**NetEvolve.Schemas.XPath**](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/) - XPath engine that evaluates compiled expressions against the document tree
- [**NetEvolve.Schemas.Xsd**](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/) - XML Schema validation
- [**NetEvolve.Schemas.Schematron**](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/) - ISO Schematron validation
- [**NetEvolve.Schemas.DependencyInjection**](https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/) - registers validators with `Microsoft.Extensions.DependencyInjection`

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
