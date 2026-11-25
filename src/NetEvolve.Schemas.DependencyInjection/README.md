# NetEvolve.Schemas.DependencyInjection

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.DependencyInjection.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.DependencyInjection.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.DependencyInjection/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

Registers the schema validators of the NetEvolve schema libraries (XSD, Schematron, DTD, or your own) with `Microsoft.Extensions.DependencyInjection`, plain or keyed, as lazily built singletons. The package is the generic registration core: it depends on `NetEvolve.Schemas` and `Microsoft.Extensions.DependencyInjection.Abstractions` only, references no format package, and contains no validation logic of its own. It adds `AddSchemaValidator<TValidator>` and `AddKeyedSchemaValidator<TValidator>` on `IServiceCollection`. The registrations of a format ship with the package of that format, in the same namespace and built on this core: `AddXsdValidator` and `AddKeyedXsdValidator` in `NetEvolve.Schemas.Xsd`, `AddSchematronValidator` and `AddKeyedSchematronValidator` in `NetEvolve.Schemas.Schematron`, and `AddDtdValidator` and `AddKeyedDtdValidator` in `NetEvolve.Schemas.Xml`. Referencing one format never brings another format's assembly.

## Features

- XSD, Schematron, and DTD registration - `AddXsdValidator`, `AddSchematronValidator`, and `AddDtdValidator` register `IXsdValidator`, `ISchematronValidator`, and `IDtdValidator`
- Keyed registration - `AddKeyedXsdValidator`, `AddKeyedSchematronValidator`, and `AddKeyedDtdValidator` register one validator per service key, so differently configured validators live side by side
- Generic registration - `AddSchemaValidator<TValidator>` and `AddKeyedSchemaValidator<TValidator>` register any `ISchemaValidator` that you build yourself
- Lazy singletons - nothing is opened, read, or compiled at registration; the first resolve builds the validator and every later resolve returns the same instance
- Configure callbacks - set the builder options (version, phase, resolver, limits) with a callback that receives the `IServiceProvider` and, for keyed registrations, the key
- Deny-by-default resources - a registered validator reaches no external resource unless the configure callback sets a resolver, see [Configuration](#configuration)
- Repeat-safe - every method has `TryAdd` semantics, so a repeated registration is a silent no-op

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas.DependencyInjection
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas.DependencyInjection
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas.DependencyInjection" Version="x.y.z" />
```

No package has been released yet; the lines apply from the first release, and `x.y.z` stands for the released version.

## Quick Start

`AddXsdValidator` takes a callback that opens the schema stream. The callback runs on the first resolve, and the registration disposes the stream right after the build. The validator is registered as `IXsdValidator`.

The samples that register a format need the package of that format next to this package; the calls resolve without a further using directive. The samples in this readme use `cancellationToken` for the token of the caller and need these namespaces:

```
using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
```

```csharp
const string schema = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:element name="order" type="xs:int"/>
    </xs:schema>
    """;

var services = new ServiceCollection();
services.AddXsdValidator(_ => new MemoryStream(Encoding.UTF8.GetBytes(schema)));

using var provider = services.BuildServiceProvider();
var validator = provider.GetRequiredService<IXsdValidator>();
var valid = validator.Validate("<order>42</order>".AsSpan(), null, cancellationToken).IsValid;
var invalid = validator.Validate("<order>x</order>".AsSpan(), null, cancellationToken).IsValid;
```

Here `valid` is `true` and `invalid` is `false`. A second resolve of `IXsdValidator` returns the same instance. A plain registration registers the validator type only: `ISchemaValidator` is not resolvable unless you register it, see [Generic registration](#generic-registration).

## Usage

### Registration methods

| Method | Package | Registers | Callbacks |
| --- | --- | --- | --- |
| `AddXsdValidator(schema, configure, systemId)` | `NetEvolve.Schemas.Xsd` | `IXsdValidator` | `schema` opens the schema stream; `configure` receives the provider and an `XsdValidatorBuilder` |
| `AddKeyedXsdValidator(serviceKey, schema, configure, systemId)` | `NetEvolve.Schemas.Xsd` | `IXsdValidator` per key | the callbacks also receive the key |
| `AddSchematronValidator(schema, configure, systemId)` | `NetEvolve.Schemas.Schematron` | `ISchematronValidator` | `schema` opens the schema stream; `configure` receives the provider and a `SchematronValidatorBuilder` |
| `AddKeyedSchematronValidator(serviceKey, schema, configure, systemId)` | `NetEvolve.Schemas.Schematron` | `ISchematronValidator` per key | the callbacks also receive the key |
| `AddDtdValidator(configure)` | `NetEvolve.Schemas.Xml` | `IDtdValidator` | `configure` receives the provider and a `DtdValidatorBuilder`; no schema, because a DTD is part of the document |
| `AddKeyedDtdValidator(serviceKey, configure)` | `NetEvolve.Schemas.Xml` | `IDtdValidator` per key | `configure` also receives the key |
| `AddSchemaValidator<TValidator>(factory)` | `NetEvolve.Schemas.DependencyInjection` | `TValidator` | `factory` receives the provider and returns the validator |
| `AddKeyedSchemaValidator<TValidator>(serviceKey, factory)` | `NetEvolve.Schemas.DependencyInjection` | `TValidator` per key, and `ISchemaValidator` under the same key | `factory` receives the provider and the key |

All methods are declared in the namespace `Microsoft.Extensions.DependencyInjection` and return the same `IServiceCollection`. Every `configure` callback is optional. The optional `systemId` is the base URI of the schema for relative references.

### Keyed registration

A key names exactly one validator. The factory and configure callbacks receive the key. A keyed registration of a validator type other than `ISchemaValidator` also registers `ISchemaValidator` under the same key, and that registration forwards to the validator.

```csharp
const string schema = """
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:element name="order" type="xs:int"/>
    </xs:schema>
    """;

var services = new ServiceCollection();
services.AddKeyedXsdValidator(
    "strict",
    (_, _) => new MemoryStream(Encoding.UTF8.GetBytes(schema)),
    (_, _, builder) => builder.WithVersion(XsdVersion.V10)
);
services.AddKeyedXsdValidator("default", (_, _) => new MemoryStream(Encoding.UTF8.GetBytes(schema)));

using var provider = services.BuildServiceProvider();
var strict = provider.GetRequiredKeyedService<IXsdValidator>("strict");
var other = provider.GetRequiredKeyedService<IXsdValidator>("default");
var asInterface = provider.GetRequiredKeyedService<ISchemaValidator>("strict");
```

Here `strict` and `other` are different singletons, a repeated lookup of the same key returns the same instance, and `asInterface` is the same instance as `strict`. `strict.Version` is `XsdVersion.V10`; without a `WithVersion` call the version is `XsdVersion.V11`. A lookup without a key is independent of keyed registrations and finds nothing.

### Schematron

The configure callback sets the builder options on the first resolve. This sample selects the phase `basic`, which activates the first pattern only.

```csharp
const string schema = """
    <schema xmlns="http://purl.oclc.org/dsdl/schematron">
      <ns prefix="i" uri="urn:invoice"/>
      <phase id="basic"><active pattern="p1"/></phase>
      <pattern id="p1">
        <rule context="i:invoice"><assert test="i:number">An invoice needs a number.</assert></rule>
      </pattern>
      <pattern id="p2">
        <rule context="i:invoice"><report test="i:total &gt; 1000">The invoice is large.</report></rule>
      </pattern>
    </schema>
    """;

var services = new ServiceCollection();
services.AddSchematronValidator(
    _ => new MemoryStream(Encoding.UTF8.GetBytes(schema)),
    (_, builder) => builder.WithPhase("basic")
);

using var provider = services.BuildServiceProvider();
var validator = provider.GetRequiredService<ISchematronValidator>();
var invoice = """<invoice xmlns="urn:invoice"><total>2500</total></invoice>""";
var result = validator.Validate(invoice.AsSpan(), null, cancellationToken);
```

Here `result.IsValid` is `false` with one issue: the invoice has no number. The second pattern is not active in the phase `basic`, so the large total is not reported.

### DTD

`AddDtdValidator` takes no schema, because a DTD is part of the document. The validator is created through `SchemaValidator.ForDtd()` on the first resolve. The sample shows the deny-by-default resolver: the plain registration refuses the external subset, and the keyed registration allows it through a resolver that is confined to one directory.

```csharp
var root = AppContext.BaseDirectory;
var systemId = new Uri(Path.Combine(root, "order.xml")).AbsoluteUri;

var services = new ServiceCollection();
services.AddDtdValidator();
services.AddKeyedDtdValidator("local", (_, _, builder) =>
    builder.WithResolver(new SandboxedFileSystemResourceResolver(root)).WithAllowExternalSubset(true)
);

using var provider = services.BuildServiceProvider();
var document = "<!DOCTYPE order SYSTEM \"readme-order.dtd\"><order/>";
var plain = provider.GetRequiredService<IDtdValidator>();
var local = provider.GetRequiredKeyedService<IDtdValidator>("local");
var denied = plain.Validate(document.AsSpan(), systemId, cancellationToken);
var allowed = local.Validate(document.AsSpan(), systemId, cancellationToken);
```

With a file `readme-order.dtd` that declares `<!ELEMENT order EMPTY>` in `root`, `denied.IsValid` is `false` and `allowed.IsValid` is `true`.

### Generic registration

`AddSchemaValidator<TValidator>` registers a validator that you build yourself. `TValidator` is constrained to `class, ISchemaValidator` and is also the registered service type. To resolve the interface, register it as `ISchemaValidator`, as this sample does.

```csharp
var builds = 0;
var services = new ServiceCollection();
services.AddSchemaValidator<ISchemaValidator>(_ =>
{
    builds++;
    return DtdSchemaValidatorExtensions.ForDtd().Build();
});

using var provider = services.BuildServiceProvider();
var notBuiltYet = builds == 0;

var first = provider.GetRequiredService<ISchemaValidator>();
var second = provider.GetRequiredService<ISchemaValidator>();
```

Here `notBuiltYet` is `true`, `builds` is `1`, and `second` is the same instance as `first`. The sample calls `DtdSchemaValidatorExtensions.ForDtd()`, which is the same entry point as `SchemaValidator.ForDtd()` and also compiles with compilers older than C# 14.

## Configuration

The `configure` callbacks are the only configuration surface. They run once, on the first resolve, and set the options of the builder of the format.

- **Lifetime.** Every registered validator is a singleton. Validators are immutable and thread-safe, so sharing one is safe.
- **Lazy compilation.** The first resolve opens the schema, runs the configure callback, and builds the validator. A problem of the schema surfaces from that resolve as a `SchemaCompilationException`, unwrapped. A failed build is not cached, so the next resolve runs the callbacks again.
- **Stream ownership.** The stream that the `schema` callback returns belongs to the registration and is disposed right after the build, whether the build succeeds or not. A stream that the configure callback passes to `AddSchema` belongs to that callback.
- **XSD version.** The version is XSD 1.1 unless the callback calls `WithVersion`; strict 1.0 is `WithVersion(XsdVersion.V10)`. More schema documents are added with `AddSchema` in the callback.
- **Resources are denied by default.** A registered validator has no resolver. A schema that includes another module, or a document with an external DTD subset, is refused until the callback sets a resolver with `WithResolver`; the external subset additionally needs `WithAllowExternalSubset(true)`.
- **Repeated registration.** Every method has `TryAdd` semantics. A repeated registration for the same service type (and key) is a silent no-op, and its factory is never invoked.
- **Argument checks.** A `null` service collection, a `null` factory or schema callback, and a `null` key throw `ArgumentNullException`. `KeyedService.AnyKey` is a resolution wildcard and is rejected as a key with an `ArgumentException`.

## Limitations

- **Keyed registration needs a keyed container.** The container must implement `IKeyedServiceProvider` (`Microsoft.Extensions.DependencyInjection` 8.0 or later). There is no fallback for other containers.
- **No wildcard key.** `KeyedService.AnyKey` is rejected as a key.
- **Singleton only.** There is no option for a scoped or transient validator.
- **XSD 1.1 is incomplete.** The version is selectable, but a 1.1 construct that is not wired in yet fails the build; see the readme of `NetEvolve.Schemas.Xsd`.
- **No XML 1.1.** DTD validation does not support XML 1.1 documents.
- **No conformance suite.** This package contains registration code only, so it has no conformance statement; the conformance of each format belongs to the package of that format. The registration behavior is tested by unit tests (`tests/NetEvolve.Schemas.DependencyInjection.Tests.Unit`) that cover lifetimes, keyed resolution, `TryAdd` semantics, lazy builds and failures, the configure callbacks, and the argument checks, and by a public API snapshot test (`tests/NetEvolve.Schemas.Tests.PublicApi`). Every code block of this readme is a compiled sample of `DependencyInjectionReadmeTests`.

## Requirements

- .NET Standard 2.0, .NET 10, or .NET 11 (the package targets `netstandard2.0`, `net10.0`, and `net11.0`; the tests run on `net5.0`, `net10.0`, and `net11.0`, and `net5.0` runs the `netstandard2.0` build)
- `NetEvolve.Schemas`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `NetEvolve.Arguments` as dependencies; the XSD, Schematron, and DTD registrations come with `NetEvolve.Schemas.Xsd`, `NetEvolve.Schemas.Schematron`, and `NetEvolve.Schemas.Xml`; on `netstandard2.0` also `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`
- A container that implements `IKeyedServiceProvider` for the keyed methods, for example `Microsoft.Extensions.DependencyInjection` 8.0 or later

## Related Packages

- [**NetEvolve.Schemas**](https://www.nuget.org/packages/NetEvolve.Schemas/) - The format-agnostic core: validator contract, result model, validation limits, and resource resolvers
- [**NetEvolve.Schemas.Xml**](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/) - XML 1.0 reader, document tree, datatypes, and DTD validation
- [**NetEvolve.Schemas.Xsd**](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/) - XML Schema (XSD) validation
- [**NetEvolve.Schemas.Schematron**](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/) - ISO Schematron validation with SVRL output

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
