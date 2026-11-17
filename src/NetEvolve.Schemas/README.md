# NetEvolve.Schemas

The format-agnostic core of the NetEvolve schema libraries: the validator contract, the result model, validation limits, and external resource resolvers. Format packages build on it to read and validate XML-based schema formats.

This package defines how a document is validated and how the outcome is reported. It does not read any schema format by itself. A format package contributes the format-specific builder and validator.

## Entry point

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

## Result model

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

## Limits

`ValidationLimits` is immutable. Every limit is finite, and the limits that have a ceiling reject larger values. `ValidationLimits.Default` applies unless a builder gets other limits through `WithLimits`. A `With...` method returns a copy.

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

## External resources

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

## Limitations

- **Compilers older than C# 14.** `SchemaValidator.For<Format>()` is a C# 14 extension member. A compiler without extension member syntax calls the public static extension class of the format package directly, named `<Format>SchemaValidatorExtensions` and declared in the namespace `NetEvolve.Schemas`:

  ```csharp
  var builder = FakeSchemaValidatorExtensions.ForFake();
  ```

- **Provisional limit defaults.** The defaults of `ValidationLimits` are provisional until they are benchmarked.
- **Sandboxed file system resolver.** The file system can change between the check and the open (time of check to time of use). Case sensitivity follows the host. A name with a colon is refused on every platform. A symbolic link or junction is served only when its final target is below the root, and a link whose target cannot be determined is refused. The root directory itself is trusted and never checked for links.
- **Placeholder samples.** The samples use a placeholder format because they exercise the core package only.
