# NetEvolve.Schemas

[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](LICENSE)
[![Build Status](https://img.shields.io/github/actions/workflow/status/dailydevops/schemas/cicd.yml?branch=main)](https://github.com/dailydevops/schemas/actions)
[![Contributors](https://img.shields.io/github/contributors/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/graphs/contributors)

Format-agnostic, span-based .NET libraries for reading, validating, and querying XML-based schema formats such as XSD and Schematron. The solution provides an XML reader and document model, an XPath evaluator, a Schematron validator, and an XSD validator behind one validator contract, for developers who need schema validation in managed code.

> [!NOTE]
> The solution is in development and no package has been released yet. The conformance rates below describe the state of the `main` branch.

## Overview

The packages form layers; each package depends only on the packages to its left:

```txt
NetEvolve.Schemas <- DependencyInjection <- Xml <- XPath <- { Schematron, Xsd }
```

- **Core**: `NetEvolve.Schemas` defines the validator contract, the result model, validation limits, and external resource resolvers.
- **XML and XPath**: `NetEvolve.Schemas.Xml` reads documents and `NetEvolve.Schemas.XPath` evaluates expressions over them.
- **Formats**: `NetEvolve.Schemas.Schematron` and `NetEvolve.Schemas.Xsd` each add a `SchemaValidator.For<Format>()` entry point, a C# 14 extension member that returns a fluent builder.
- **Integration**: `NetEvolve.Schemas.DependencyInjection` is the generic registration core (`AddSchemaValidator`, `AddKeyedSchemaValidator`) for `Microsoft.Extensions.DependencyInjection` and references no format package. Each format package (`Xml` for DTD, `Xsd`, `Schematron`) carries its own `Add<Format>Validator` and `AddKeyed<Format>Validator` extensions in the namespace `Microsoft.Extensions.DependencyInjection`, so referencing one format brings no other format's assembly.

All packages target `netstandard2.0`, `net10.0`, and `net11.0`. Beyond the layers above, every package has one runtime dependency, `NetEvolve.Arguments`, and the `netstandard2.0` builds add three Microsoft polyfill packages.

## Projects

### Packages

| Package | Purpose | Depends on | Target frameworks | Status |
| --- | --- | --- | --- | --- |
| [`NetEvolve.Schemas`](src/NetEvolve.Schemas/README.md) | Validator contract, result model, limits, and resolvers | none | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released |
| [`NetEvolve.Schemas.DependencyInjection`](src/NetEvolve.Schemas.DependencyInjection/README.md) | Generic dependency injection registration core | `NetEvolve.Schemas`, `Microsoft.Extensions.DependencyInjection.Abstractions` | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released |
| [`NetEvolve.Schemas.Xml`](src/NetEvolve.Schemas.Xml/README.md) | XML reader, document model, and DTD validation, with the DTD registration extensions | `NetEvolve.Schemas`, `NetEvolve.Schemas.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Abstractions` | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released |
| `NetEvolve.Schemas.XPath` | XPath evaluation (XPath 2.0; XPath 3.0 and 3.1 partial) | `NetEvolve.Schemas.Xml` | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released |
| [`NetEvolve.Schemas.Schematron`](src/NetEvolve.Schemas.Schematron/README.md) | Schematron validation with SVRL output, with the Schematron registration extensions | `NetEvolve.Schemas.XPath`, `NetEvolve.Schemas.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Abstractions` | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released |
| `NetEvolve.Schemas.Xsd` | XSD validation, with the XSD registration extensions | `NetEvolve.Schemas.XPath`, `NetEvolve.Schemas.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Abstractions` | `netstandard2.0`, `net10.0`, `net11.0` | Not yet released, partial |

The target frameworks are set once in `src/Directory.Build.props`. Only `NetEvolve.Schemas` is packable today; the other projects become packable when their package lands (`Directory.Build.props`). The XPath and XSD packages have no package readme yet.

### Tests and tooling

- **tests/** - one `Tests.Unit` project per package, `Tests.Integration` projects for XML, XPath, XSD, and Schematron, and the architecture, public API, packaging, and legacy compiler checks.
- **tests/NetEvolve.Schemas.Conformance** - the shared harness that fetches the pinned conformance suites, compares results with the known-failure baselines, and reports the rates.
- **benchmarks/** - benchmarks of the hot paths.

## Features

- Span-based input - validate a `Stream`, `ReadOnlySpan<byte>`, `ReadOnlySpan<char>`, `ReadOnlySequence<byte>`, or `ReadOnlySequence<char>`.
- One validator contract - every format returns an immutable, thread-safe `ISchemaValidator` with the same result model, issue codes, and locations.
- Schematron validation - ISO/IEC 19757-3:2025 with phases, parameters, includes, and SVRL output.
- XML reading - an XML 1.0 (Fifth Edition) reader with namespaces, optional DTD processing, and DTD validation.
- XPath evaluation - XPath 2.0; XPath 3.0 and 3.1 are partial.
- XSD validation - XSD 1.0 schema compilation and instance validation; XSD 1.1 is partial.
- Secure defaults - bounded input, depth, and entity expansion, no document type declaration, and no external access unless a resolver is set.
- Dependency injection - plain and keyed registration of validators as lazily built singletons.

## Getting Started

### Prerequisites

- [.NET SDK 11.0](https://dotnet.microsoft.com/download) or higher (the solution builds the `net11.0` target; the SDK also builds `net10.0` and `netstandard2.0`)
- [Git](https://git-scm.com/) for version control
- [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- A compiler with C# 14 for the `SchemaValidator.For<Format>()` extension members; older compilers call the static extension classes directly (see the package readmes)

### Installation

No package has been released yet, so the packages are built from source:

1. Clone the repository:

   ```bash
   git clone https://github.com/dailydevops/schemas.git
   cd schemas
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Build the solution:

   ```bash
   dotnet build --configuration Release
   ```

4. Run tests to verify installation (the conformance suites are fetched on first use, so this needs network access):

   ```bash
   dotnet test --solution Schemas.slnx --configuration Release
   ```

### Usage

The Schematron entry point compiles a schema into an immutable, thread-safe validator. The sample runs in an async method that has a `CancellationToken cancellationToken`. A failed `assert` is an issue with severity `Error`: here `result.IsValid` is `false`, and the single issue carries the code `NES3400` and the message `The total 150 exceeds 100.` See the [Schematron package readme](src/NetEvolve.Schemas.Schematron/README.md) for phases, parameters, includes, and SVRL output.

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

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
# Run all tests
dotnet test --solution Schemas.slnx --configuration Release

# Run one test project on one target framework
dotnet test --project tests/NetEvolve.Schemas.Tests.Unit --configuration Release --framework net10.0
```

The test projects run on three legs: `net9.0` (runs the `netstandard2.0` build of the libraries), `net10.0`, and `net11.0`. Every behavior change starts with a failing TUnit test; see the [development workflow](docs/development/workflow.md).

### Code Formatting

```bash
# Format code using CSharpier
csharpier format .
```

### Project Structure

```txt
src/                    # Production code, one folder per package
├── NetEvolve.Schemas/
├── NetEvolve.Schemas.Xml/
├── NetEvolve.Schemas.XPath/
├── NetEvolve.Schemas.Schematron/
├── NetEvolve.Schemas.Xsd/
├── NetEvolve.Schemas.DependencyInjection/
└── Shared/            # Polyfills shared by the netstandard2.0 builds

tests/                 # Test projects and the conformance harness
├── conformance/       # Suite manifests (suites/) and known-failure baselines (baselines/)
└── NetEvolve.Schemas.*

benchmarks/            # Benchmarks
docs/                  # Development workflow and XSD deviations
specs/                 # Specification notes
templates/             # Documentation and file templates
```

## Architecture

- **Layered packages**: the dependency direction is fixed (see [Overview](#overview)).
- **Format-agnostic core**: format packages contribute a builder and a validator; the contract, result model, limits, and resolvers live in `NetEvolve.Schemas`.
- **Immutable validators**: a builder is not thread-safe, the validator it builds is.
- **Deny by default**: external resources are refused unless a resolver is configured (see [Security Defaults](#security-defaults)).
- **Argument validation**: guards use `NetEvolve.Arguments`, which ships as a runtime dependency of every package.

## Conformance

Each rate is passed / (passed + failed), truncated to one decimal. Skipped and excluded cases are not counted. A failure is either fixed or listed in a committed known-failure baseline under `tests/conformance/baselines/`; a failure outside the baseline and a baselined case that passes both fail the run. The suites are pinned by checksum in `tests/conformance/suites/` and are never committed.

The rates were measured on `main` at commit `a2d4119` with `net10.0`. The `Conformance` workflow (`.github/workflows/conformance.yml`) publishes the same rate rows in its job summary: the runners append them to the file named by the `GITHUB_STEP_SUMMARY` environment variable, so set that variable to a file path to get the rows locally. The rows below that name a test project and a filter come from that summary: run `dotnet test --project tests/<project> --configuration Release --framework net10.0 --treenode-filter "<filter>"`.

| Suite | Package | Applicable | Passed | Rate | Measured with |
| --- | --- | ---: | ---: | ---: | --- |
| W3C XML Conformance Test Suite (xmlconf), non-validating reader | `NetEvolve.Schemas.Xml` | 2,001 | 1,980 | 98.9% | [Xml package readme](src/NetEvolve.Schemas.Xml/README.md#conformance); 21 baselined cases in `tests/conformance/baselines/xmlconf` |
| xmlconf, DTD validation | `NetEvolve.Schemas.Xml` | 943 | 931 | 98.7% | `NetEvolve.Schemas.Xml.Tests.Integration`, filter `/*/*/*Xml*Conf*SuiteTests/*`; 12 baselined, 14 skipped |
| W3C QT3 test suite, XPath 2.0 selection | `NetEvolve.Schemas.XPath` | 14,550 | 14,454 | 99.3% | `NetEvolve.Schemas.XPath.Tests.Integration`, filter `/*/*Conformance*/*/*`; 96 baselined cases in `tests/conformance/baselines/qt3`, 738 skipped |
| W3C XML Schema Test Suite (XSTS) 1.0, schema tests | `NetEvolve.Schemas.Xsd` | 14,357 | 14,236 | 99.1% | `NetEvolve.Schemas.Xsd.Tests.Integration`, filter `/*/*Conformance*/*/*`; 121 baselined cases |
| XSTS 1.0, instance tests | `NetEvolve.Schemas.Xsd` | 24,914 | 24,017 | 96.3% | same run; 897 baselined cases |
| XSTS 1.1, schema and instance tests | `NetEvolve.Schemas.Xsd` | 41,389 | 0 | 0.0% | derived from the same run: the row for all of XSTS (80,660 applicable, 38,253 passed, 47.4%) minus both XSTS 1.0 rows |
| Schematron conformance suite 2.0, bindings `xslt-1` and `xslt-2` | `NetEvolve.Schemas.Schematron` | 84 | 55 | 65.4% | `NetEvolve.Schemas.Schematron.Tests.Integration`, filter `/*/*Conformance*/*/*`; 29 baselined cases in `tests/conformance/baselines/schematron-conformance`, 3 excluded |
| EN 16931 (CEN) validation artefacts 1.3.17, golden tests | `NetEvolve.Schemas.Schematron` | 1,174 | 1,174 | 100.0% | `NetEvolve.Schemas.Schematron.Tests.Integration`, filter `/*/*/En16931GoldenTests/*`; no baselined cases |
| EN 16931 (CEN) 1.3.17, EDIFACT binding, ISO 20625 XML examples | `NetEvolve.Schemas.Schematron` | 9 | 9 | 100.0% | `NetEvolve.Schemas.Schematron.Tests.Integration`, filter `/*/*/En16931ExtendedGoldenTests/*`; no baselined cases |
| EN 16931 (CEN) 1.3.17, UBL rule set, complete invoices of `test/testfiles` | `NetEvolve.Schemas.Schematron` | 29 | 29 | 100.0% | same run; no baselined cases |

Not yet reported:

- **QT3 XPath 3.0 and 3.1.** The tiers run in `Qt3Xp3ConformanceTests` against the baselines `tests/conformance/baselines/qt3-xp30` (2,903 known-failure entries) and `qt3-xp31` (1,477 entries), but no rate is published yet, and the QT3 runner has no XPath 3.0 mode. A case that needs XPath 3.x syntax or functions is a known failure until the XPath package supports it ([#328](https://github.com/dailydevops/schemas/issues/328), [#353](https://github.com/dailydevops/schemas/issues/353), [#355](https://github.com/dailydevops/schemas/issues/355), [#356](https://github.com/dailydevops/schemas/issues/356)).
- **Further Schematron rule sets.** Manifests for more rule sets are pinned in `tests/conformance/suites/`; no rate is published for them.

The table is updated by hand and can lag behind CI between releases.

## Security Defaults

- **Bounded input**: `ValidationLimits.Default` caps input size, element depth, entity expansion, and resolved resources. Every limit is finite and can be changed per builder with `WithLimits`. The defaults are provisional until they are benchmarked.
- **No document type declaration by default**: the XML reader rejects a document type declaration (`XML2001`) unless DTD processing is switched on, and it never opens an external resource on its own. The Schematron validator rejects a document type declaration in a schema or a document.
- **External resources are deny by default**: unless a resolver is configured, every request for an external resource is refused (`NES0100`). `SandboxedFileSystemResourceResolver` serves `file` URIs below one root directory and refuses everything else.

See the [core](src/NetEvolve.Schemas/README.md#limits) and [XML](src/NetEvolve.Schemas.Xml/README.md#security-defaults) package readmes for the details.

## Known Limitations

- **No release yet.** Names and APIs are provisional until the first release.
- **XPath 3.0 and 3.1 evaluation is partial.** Syntax with version gating ([#328](https://github.com/dailydevops/schemas/issues/328)), function items and dynamic calls ([#353](https://github.com/dailydevops/schemas/issues/353)), `let`, simple map, and string concatenation ([#355](https://github.com/dailydevops/schemas/issues/355)), and maps, arrays, lookup, and arrow ([#356](https://github.com/dailydevops/schemas/issues/356)) are open. XPath 2.0 is the supported level.
- **XSD 1.1 is partial.** The XSD 1.1 features are not wired into compilation and validation ([#360](https://github.com/dailydevops/schemas/issues/360)), except the assertions on complex types, which run in the default builder ([#318](https://github.com/dailydevops/schemas/issues/318)). None of the XSTS 1.1 cases passes yet.
- **XSD 1.0 instance validation is incomplete.** 60.2% of the XSTS 1.0 instance cases pass; the rest are baselined.
- **XML 1.1 is not supported.** The reader accepts XML 1.0 only and cannot switch namespace processing off.
- **Schematron supports a subset of XSLT.** `xsl:template`, `xsl:apply-templates`, `xsl:call-template`, `xsl:iterate`, `xsl:try`, and other elements are rejected with `NES3204`, and stylesheet functions and keys are not callable from a rule yet. The pattern attribute `documents` and the rule attribute `visit-each` are accepted and ignored. See the [Schematron package readme](src/NetEvolve.Schemas.Schematron/README.md#limitations) for the full list with issue links.
- **Compilers older than C# 14** call the static extension classes directly instead of `SchemaValidator.For<Format>()`.

## Contributing

We welcome contributions from the community! Please read our [Contributing Guidelines](CONTRIBUTING.md) before submitting a pull request.

Key points:

- Follow the [Conventional Commits](https://www.conventionalcommits.org/) format for commit messages
- Write the failing test first; see the [development workflow](docs/development/workflow.md)
- Follow existing code style and conventions
- Update documentation as needed

## Code of Conduct

This project adheres to the Contributor Covenant [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to [info@daily-devops.net](mailto:info@daily-devops.net).

## Documentation

- **[Contributing Guidelines](CONTRIBUTING.md)** - How to contribute to this project
- **[Development workflow](docs/development/workflow.md)** - Test-first workflow and bug handling
- **[XSD deviations](docs/xsd/system-xml-schema-deviations.md)** - Where the XSD validator deviates from the standard
- **[Conformance suites](tests/conformance/suites/README.md)** - How the suites are pinned and fetched
- **[Code of Conduct](CODE_OF_CONDUCT.md)** - Community standards and expectations
- **[License](LICENSE)** - Project licensing information

## Versioning

This project uses [GitVersion](https://gitversion.net/) for automated semantic versioning based on Git history and [Conventional Commits](https://www.conventionalcommits.org/). Version numbers are automatically calculated during the build process.

## Support

- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/dailydevops/schemas/issues)
- **Documentation**: Read the full documentation in this repository

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

> [!NOTE]
> **Made with ❤️ by the NetEvolve Team**
> Visit us at [https://www.daily-devops.net](https://www.daily-devops.net) for more information about our services and solutions.
