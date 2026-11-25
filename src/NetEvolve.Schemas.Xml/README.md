# NetEvolve.Schemas.Xml

[![NuGet Version](https://img.shields.io/nuget/v/NetEvolve.Schemas.Xml.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetEvolve.Schemas.Xml.svg)](https://www.nuget.org/packages/NetEvolve.Schemas.Xml/)
[![License](https://img.shields.io/github/license/dailydevops/schemas.svg)](https://github.com/dailydevops/schemas/blob/main/LICENSE)

An XML 1.0 (Fifth Edition) and Namespaces in XML 1.0 (Third Edition) reader for .NET that builds an immutable document tree with a line and column on every node, and a validator for the document type declaration. The defaults refuse a document type declaration and bound every resource that hostile input can inflate. It is the XML foundation of the other packages of this repository, and it is usable on its own.

## Features

- Every input kind, one result - read a span, memory, a sequence, or a stream into the same tree, with the same positions and, for a malformed document, the same error code and position, see [Input kinds](#input-kinds)
- Immutable document tree with positions - a line and column on every node, and a name table that is frozen once the document is loaded
- Events without a tree - the readers push events into an `IXmlSink`, so a caller that only scans a document pays for no tree
- Secure by default - a document type declaration is rejected, the reader never fetches anything external, and every limit is finite, see [Security defaults](#security-defaults)
- DTD validation - `SchemaValidator.ForDtd()` checks a document against its document type declaration and reports every validity constraint of XML 1.0 with a code, see [DTD validation](#dtd-validation)
- Conformance measured - checked against the W3C XML Conformance Test Suite, see [Conformance](#conformance)
- XML writer - `XmlWriter` writes the events of the sink contract as well-formed XML 1.0 to a stream or a buffer writer, optionally canonical
- XSD datatypes and regular expressions - the built-in datatypes of XSD 1.0 and 1.1, and a regular expression engine with an XSD and an XPath dialect

## Installation

### NuGet Package Manager

```powershell
Install-Package NetEvolve.Schemas.Xml
```

### .NET CLI

```bash
dotnet add package NetEvolve.Schemas.Xml
```

### PackageReference

```xml
<PackageReference Include="NetEvolve.Schemas.Xml" Version="x.y.z" />
```

No package has been released yet; the lines apply from the first release, and `x.y.z` stands for the released version.

## Quick Start

`XmlDocumentLoader` is the one entry for building an `XmlDocumentTree`. The samples in this readme need these namespaces. `cancellationToken` is a `CancellationToken`.

```
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
```

```csharp
var tree = XmlDocumentLoader.Load("<order id=\"7\">\n  <item>tea</item>\n</order>".AsSpan());

var order = tree.Document.FirstChild;
var name = tree.Names.GetString(order.Name.Local);
var item = order.FirstChild.NextSibling;
var text = item.StringValue;
var position = tree.GetPosition(item.Index);
```

Here `name` is `order`, `text` is `tea`, and `position` is line 2, column 3. Lines and columns are one-based. The document node is `tree.Document`; navigate from a node with `FirstChild`, `NextSibling`, `FirstAttribute`, and `NextAttribute`. A name holds symbols of the name table of the tree, so read its text through `tree.Names`.

## Usage

### Input kinds

Every input kind gives the same tree, the same positions, and, for a malformed document, the same error code and position. A byte input is decoded from its byte order mark and its encoding declaration (XML 1.0, Appendix F).

| Input | Method | Reader behind it |
| --- | --- | --- |
| `ReadOnlySpan<char>`, `ReadOnlySpan<byte>` | `Load` | `XmlPipelineReader` |
| `ReadOnlyMemory<char>`, `ReadOnlyMemory<byte>` | `LoadFromMemory` | `XmlPipelineReader` |
| `ReadOnlySequence<char>`, `ReadOnlySequence<byte>` | `Load` | `XmlSequenceReader` |
| `Stream` | `LoadAsync` | `XmlStreamReader<TSink>` |

Every method takes optional `XmlReaderOptions` and an optional `baseUri`, which becomes the base URI of the document node. A document that is not well formed, that uses an unsupported encoding, or that exceeds a limit throws `XmlParseException`. Its `Diagnostic` carries the error code and the position.

The readers are public as well. They push events into an `IXmlSink` and build no tree, so a caller that only scans a document pays for no tree.

#### Memory

```csharp
ReadOnlyMemory<byte> utf8 = Encoding.UTF8.GetBytes("<order id=\"7\"><item>tea</item></order>");

var tree = XmlDocumentLoader.LoadFromMemory(utf8);
var text = tree.Document.FirstChild.FirstChild.StringValue;
```

Here `text` is `tea`.

#### Sequence

A `ReadOnlySequence<T>` is read segment by segment, without flattening it first. The sample needs a segment type, which is a few lines:

```csharp
internal sealed class Segment : ReadOnlySequenceSegment<byte>
{
    public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

    public Segment Append(ReadOnlyMemory<byte> memory)
    {
        var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
        Next = next;
        return next;
    }
}
```

The document below is split inside the name `item`, so a token spans the segment edge:

```csharp
var first = new Segment("<order id=\"7\"><it"u8.ToArray());
var last = first.Append("em>tea</item></order>"u8.ToArray());
var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

var tree = XmlDocumentLoader.Load(sequence);
var text = tree.Document.FirstChild.FirstChild.StringValue;
```

Here `text` is `tea`.

#### Async stream

```csharp
await using var stream = new MemoryStream("<order id=\"7\"><item>tea</item></order>"u8.ToArray());

var tree = await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken);
var text = tree.Document.FirstChild.FirstChild.StringValue;
```

Here `text` is `tea`. The stream is read from its current position to its end and stays open. A canceled token throws `OperationCanceledException`.

### DTD validation

`SchemaValidator.ForDtd()` returns a builder for a validator that checks a document against its document type declaration (XML 1.0, section 5.1). It is a C# 14 extension member of `SchemaValidator` and is visible when this package is referenced. `Build` and `BuildAsync` create an immutable, thread-safe validator, which takes every input kind of `ISchemaValidator`: `Validate` takes a `string`, a `ReadOnlySpan<char>` or `ReadOnlySpan<byte>`, a `ReadOnlySequence<char>` or `ReadOnlySequence<byte>`, or a `Stream`; `ValidateAsync` takes a `Stream`, a `ReadOnlyMemory<char>` or `ReadOnlyMemory<byte>`, or a sequence. The optional `systemId` is the base URI of the document and the source in the locations of the issues. Every input kind gives the same issues with the same codes and locations.

The samples in this section need these namespaces. `directory` is the path of a folder.

```
using System;
using System.IO;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources.FileSystem;
```

```csharp
var validator = SchemaValidator.ForDtd().Build();

var result = validator.Validate("<!DOCTYPE note [<!ELEMENT note EMPTY>]>\n<note>text</note>");

var issue = result.Issues[0];
var isValid = result.IsValid;
var code = issue.Code;
var constraint = issue.Constraint;
var line = issue.Location.Line;
```

Here `isValid` is `false`, `code` is `NES1504`, `constraint` is `Element Valid`, and `line` is 2. `NES1504` is the code of the validity constraint Element Valid, which the Xml package also names `XML5004`. Every validity constraint of XML 1.0 has one code, `XML5001` to `XML5026`, and `DtdValidityCodes` lists them with the name of the constraint and the step that checks it. An optional report, such as a non-deterministic content model, is a warning with a code from `XML5901` to `XML5911`.

A validity error is an issue of severity error and never stops the run, so one run reports every error of the document. A document that is not well formed ends the run: the error of the reader is the last issue and the result is truncated. A limit that the core names, such as the depth or the entity expansion, is reported with the core code (for example `NES0002`).

#### Compilers older than C# 14

A compiler older than C# 14 has no extension members. It calls the public static extension class directly. The class is `DtdSchemaValidatorExtensions`, and it is declared in the namespace `NetEvolve.Schemas`:

```csharp
var validator = DtdSchemaValidatorExtensions.ForDtd().Build();

var result = validator.Validate("<!DOCTYPE note [<!ELEMENT note EMPTY>]>\n<note>text</note>");
```

The builder and the result are the same as with `SchemaValidator.ForDtd()`. A project with `LangVersion` 13 compiles this sample in the tests of the repository.

#### Deny by default

The validator opens nothing external unless the caller opts in. The default builder uses `DenyAllResourceResolver` and every flag is off, so no file and no socket is touched. A document that names an external subset ends the run with `XML2002`. It is a fatal status and not a validity error, because continuing without the subset would report many false validity errors.

```csharp
var validator = SchemaValidator.ForDtd().Build();

var result = validator.Validate("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>");

var code = result.Issues[0].Code;
```

Here `code` is `NES1202`, the code of `XML2002`. Two things enable an external resource: a resolver that the caller sets, and the flag of the kind of resource. A resolver alone enables nothing, and a flag without a resolver reaches `DenyAllResourceResolver` and is refused. A refused or failing request is `XML2002` with the kind, the requested URI, and the base URI in the message.

```csharp
var validator = SchemaValidator
    .ForDtd()
    .WithResolver(new SandboxedFileSystemResourceResolver(directory))
    .WithAllowExternalSubset(true)
    .Build();

var systemId = new Uri(Path.Combine(directory, "note.xml")).AbsoluteUri;
var result = validator.Validate("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>", systemId);
```

Here `directory` holds `note.dtd` with `<!ELEMENT note EMPTY>`, and `result` is valid. The resolver serves files below `directory` only. The relative system identifier `note.dtd` resolves against `systemId`.

## Configuration

### Security defaults

`XmlReaderOptions.Default` is the secure choice. Every other setting is an explicit choice of the caller.

#### A document type declaration is rejected

`DtdProcessing` is off by default. A document with a document type declaration throws `XmlParseException` with the code `XML2001`, so no entity is declared or expanded and no external resource is named.

```csharp
string? code = null;
try
{
    XmlDocumentLoader.Load("<!DOCTYPE order><order/>".AsSpan());
}
catch (XmlParseException exception)
{
    code = exception.Diagnostic?.Code.Alias;
}
```

Here `code` is `XML2001`.

`WithDtdProcessing(true)` parses the declaration instead. References to the internal general entities it declares are expanded in content and in attribute values, bounded by `MaxEntityExpansionCharacters` and `MaxEntityDepth`. The attribute definitions of the declaration apply: a defaulted or fixed attribute is added to the elements that omit it, and the value of an attribute with a declared type other than `CDATA` is normalized. The reader stays non-validating.

```csharp
var options = XmlReaderOptions.Default.WithDtdProcessing(true);
var tree = XmlDocumentLoader.Load(
    "<!DOCTYPE note [<!ENTITY who \"Ada\">]><note>&who;</note>".AsSpan(),
    options
);

var text = tree.Document.FirstChild.StringValue;
```

Here `text` is `Ada`.

#### No external access

The public options of the reader, `XmlReaderOptions`, have no resolver and no switch for external access. (The DTD validator has both; see [DTD validation](#dtd-validation).) The external subset, the external parameter entities, and the external general entities are never read, and nothing is fetched or opened, whatever the document says. With `DtdProcessing` on:

- A reference to an external parsed entity in content throws `XmlParseException` with the code `XML2002`.
- A reference to an external entity in an attribute value always throws, with the code `XML0022`.
- A document type declaration that names an external subset is accepted, and the subset is skipped. A reference to an entity that such a subset could have declared is skipped, not an error.

```csharp
var options = XmlReaderOptions.Default.WithDtdProcessing(true);
string? code = null;
try
{
    XmlDocumentLoader.Load(
        "<!DOCTYPE note [<!ENTITY who SYSTEM \"who.xml\">]><note>&who;</note>".AsSpan(),
        options
    );
}
catch (XmlParseException exception)
{
    code = exception.Diagnostic?.Code.Alias;
}
```

Here `code` is `XML2002`. A public way to allow external access is tracked in [#698](https://github.com/dailydevops/schemas/issues/698).

#### Limits

`XmlReaderLimits.Default` applies unless `XmlReaderOptions.WithLimits` gets other limits. The limits are immutable, and a `With...` method returns a copy. Every limit is finite: a limit of zero never disables a limit, because the `With...` methods reject values below one with an `ArgumentOutOfRangeException`. An exceeded limit throws `XmlParseException` with the code of the limit. The length limits count characters, not bytes.

| Limit | Default | Error code | Bounds |
| --- | --- | --- | --- |
| `MaxDocumentCharacters` | 134,217,728 | XML1101 | Decoded characters of one document. |
| `MaxElementDepth` | 256 | XML1102 | Element nesting depth. |
| `MaxAttributesPerElement` | 256 | XML1103 | Attributes of one element. |
| `MaxNamespaceDeclarationsPerElement` | 256 | XML1104 | Namespace declarations of one element. |
| `MaxNameLength` | 1,024 | XML1105 | Length of a name and of the body of a reference. |
| `MaxTokenLength` | 1,048,576 | XML1106 | Length of a comment or a processing instruction. |
| `MaxTextNodeLength` | 16,777,216 | XML1107 | Length of a coalesced text node. |
| `MaxNodeCount` | 10,000,000 | XML1108 | Nodes of one document. |
| `MaxEntityExpansionCharacters` | 10,485,760 | XML1109 | Characters that entity expansion produces, charged for every reference. |
| `MaxEntityDepth` | 16 | XML1110 | Nesting depth of entity references. |
| `MaxDistinctNames` | 1,048,576 | XML1111 | Distinct names in a name table. |
| `MaxAttributeValueLength` | 1,048,576 | XML1112 | Length of a normalized attribute value. |
| `MaxStartTagCharacters` | 4,194,304 | XML1113 | Characters of a buffered start tag. |
| `MaxXmlDeclarationLength` | 1,024 | XML1114 | Length of the XML declaration. |
| `MaxDoctypeLength` | 1,048,576 | XML1115 | Length of the document type declaration. |

```csharp
var limits = XmlReaderLimits.Default.WithMaxElementDepth(2);
var options = XmlReaderOptions.Default.WithLimits(limits);
string? code = null;
try
{
    XmlDocumentLoader.Load("<a><b><c/></b></a>".AsSpan(), options);
}
catch (XmlParseException exception)
{
    code = exception.Diagnostic?.Code.Alias;
}
```

Here `code` is `XML1102`.

By default every document owns its name table, which is frozen once the document is loaded. A table passed with `WithNameTable` is shared across documents and stays open; `MaxDistinctNames` bounds it.

### DTD validator settings

| Member | Default | Meaning |
| --- | --- | --- |
| `WithResolver` | `DenyAllResourceResolver` | Resolves the external subset and the external entities. |
| `WithAllowExternalSubset` | `false` | Lets the external subset be requested. |
| `WithAllowExternalGeneralEntities` | `false` | Lets external general entities be requested. |
| `WithAllowExternalParameterEntities` | `false` | Lets external parameter entities be requested. |
| `WithNonDeterministicContentModels` | `Warn` | `Accept` reports nothing, `Warn` reports `XML5911` as a warning, `Reject` reports it as an error. The accepted language is the same in every mode. |
| `WithLimits` | `ValidationLimits.Default` | `MaxIssues` and `MaxInputSize` apply to the result and the input. `MaxDepth`, `MaxAttributesPerElement`, `MaxEntityExpansion`, and `MaxEntityDepth` apply to the reader. |
| `WithDtdLimits` | `DtdValidationLimits.Default` | `MaxExternalEntityRequests` (32, error `XML1117`), `MaxContentModelStates` (65,536, error `XML1118`), and `MaxDeclarations` (100,000, error `XML1119`). |

Exceeding a limit is a fatal status and never a validity error. `Build` takes a snapshot of the builder, so a later change of the builder does not reach a validator that exists.

## Conformance

The package is checked against the W3C XML Conformance Test Suite (`xmlts20130923`), pinned by checksum in [`tests/conformance/suites/xmlconf.json`](https://github.com/dailydevops/schemas/blob/main/tests/conformance/suites/xmlconf.json). No part of the suite is committed; the conformance tests of the repository fetch it. The suite has 2,585 cases. 2,001 of them apply to XML 1.0 (Fifth Edition) and Namespaces in XML 1.0, and 584 cases are excluded:

| Excluded class | Cases | Reason |
| --- | --- | --- |
| XML 1.1 | 266 | XML 1.1 is not supported. |
| Namespaces in XML 1.1 | 8 | XML 1.1 is not supported. |
| XML 1.0 editions one to four only | 310 | The reader follows the name rules of the Fifth Edition. |

The rate is passed / (passed + failed); excluded and skipped cases are not counted, and the rate is truncated, never rounded up, to one decimal. Both runs read the external subset and the external entities through a resolver that is confined to the suite directory. Reproduce the numbers on a clone with the suite tests of the Xml integration project; the tests fail when a count differs from the committed counts:

```bash
dotnet test --project tests/NetEvolve.Schemas.Xml.Tests.Integration --configuration Release --framework net10.0 --treenode-filter "/*/*/*XmlConfSuiteTests/*"
```

### Reader

**Rate: 98.8%** (1,978 of 2,001 applicable cases). A not-well-formed case passes when the reader reports a fatal error. A valid or invalid case passes when the reader accepts the document, because the reader does not validate. The run reads with DTD processing on, which the public options allow, and with the external access that the public options do not expose.

23 cases fail. They are listed in the committed baseline `tests/conformance/baselines/xmlconf` (one file per test set), and a change that adds a failure fails the build:

| Failure class | Cases | Reason |
| --- | --- | --- |
| Namespace processing off | 9 | The case needs namespace processing switched off; the reader has no such mode. |
| Document missing from the archive | 9 | The case names a document that the pinned archive does not contain. |
| Name above the default limit | 2 | The document has a name longer than the default `MaxNameLength` and is rejected with XML1105. |
| Version other than 1.0 | 1 | The document declares version 1.7, which the Fifth Edition allows; the reader accepts 1.0 only and reports XML0010. |
| Entity used before its declaration | 2 | A default value refers to an entity that is declared later and is accepted ([#878](https://github.com/dailydevops/schemas/issues/878)). |

### DTD validation conformance

**Validation rate: 98.3%** (927 of 943 applicable cases). The run feeds the 957 valid and invalid cases of the selection to the DTD validation, for every value of the `ENTITIES` tag of the suite. A valid case passes when the run reports no validity error and no fatal status. An invalid case passes when it reports at least one validity error and no fatal status. The not-well-formed and error cases belong to the reader run above.

14 cases are not run, because the reader cannot read them; they are skipped, count neither as passed nor as failed, and stay in the reader baseline:

| Not run | Cases | Reason |
| --- | --- | --- |
| Namespace processing off | 9 | The case needs namespace processing switched off; the reader has no such mode. |
| Document missing from the archive | 2 | The case names a document that the pinned archive does not contain. |
| Name above the default limit | 2 | The document has a name longer than the default `MaxNameLength` and is rejected with XML1105. |
| Version other than 1.0 | 1 | The document declares version 1.7, which the Fifth Edition allows; the reader accepts 1.0 only and reports XML0010. |

16 cases fail, all of them invalid documents for which the run reports no validity error. They are the known gaps of the validation. Unlike the reader, the validation run has no baseline directory: the known gaps are the table `Known` in `tests/NetEvolve.Schemas.Xml.Tests.Integration/XmlConf/Validating/ValidatingXmlConfGaps.cs`, and the run fails on a new failure and on a listed case that passes.

| Failure class | Cases | Reason |
| --- | --- | --- |
| Parse-time anomaly not recorded | 10 | The declaration parser records no parse-time anomaly ([#608](https://github.com/dailydevops/schemas/issues/608)), so the nesting and uniqueness constraints are not reported. |
| Skipped entity reference | 2 | The reader has no event for an entity reference ([#647](https://github.com/dailydevops/schemas/issues/647)), so the constraint is not reported. |
| Colon in an ID value | 2 | ID values are checked against Name only ([#925](https://github.com/dailydevops/schemas/issues/925)): a colon is accepted. |
| Standalone normalization clause | 2 | The clause skips enumerated and notation attributes ([#924](https://github.com/dailydevops/schemas/issues/924)), so Standalone Document Declaration is not reported. |

## Limitations

- **XML 1.1 is not supported.** A document that declares another version than 1.0 is rejected with `XML0010`. That includes versions such as 1.7 that the Fifth Edition of XML 1.0 allows.
- **DTD processing is off by default.** A document type declaration is rejected with `XML2001` unless `WithDtdProcessing(true)` is set. Even then the reader reads only the internal subset, and it never opens an external resource (see [No external access](#no-external-access)). The notations and the unparsed entities of the declaration are not added to the tree ([#731](https://github.com/dailydevops/schemas/issues/731)).
- **The reader does not validate.** `XmlDocumentLoader` and the readers check well-formedness only. The validator of `SchemaValidator.ForDtd()` checks validity (see [DTD validation](#dtd-validation)).
- **External access is off by default.** A validating processor should read the external subset and the external entities (XML 1.0, section 5.1). The DTD validator does so only when the caller sets a resolver and the flag of the kind of resource. A document that depends on an external subset ends with `XML2002` until access is enabled.
- **A stream and a sequence are buffered.** The validating reader reads complete input. `Validate` and `ValidateAsync` read a `Stream` to its end into a pooled buffer, and a sequence of several segments is copied once into a pooled buffer, both bounded by `MaxInputSize` ([#926](https://github.com/dailydevops/schemas/issues/926)). The run observes the cancellation token only before it starts.
- **Two core limits are not applied to external resources.** `MaxExternalResources` and `MaxResourceSize` of `ValidationLimits` do not reach the DTD validator. `DtdValidationLimits.MaxExternalEntityRequests` bounds the resolver calls ([#938](https://github.com/dailydevops/schemas/issues/938)), and the entity expansion budget bounds the bytes of a resource ([#674](https://github.com/dailydevops/schemas/issues/674)).
- **Some validity checks are not complete.** The reader reports no event for a skipped entity reference, so the checks that depend on it are not reported ([#647](https://github.com/dailydevops/schemas/issues/647)). The location of an issue about text, a comment, or an end tag is the position after the token ([#927](https://github.com/dailydevops/schemas/issues/927)).
- **Span inputs are synchronous.** `Load` and `LoadFromMemory` run on the calling thread, and so does the load of a sequence. Only `LoadAsync` is asynchronous. A span is copied or decoded during the call; memory and a sequence are read during the call only, so they must stay valid and unchanged until it returns.
- **The stream path buffers.** `LoadAsync` reads the stream to its end through pooled buffers in chunks and decodes them as it goes. A token that spans a chunk edge waits in the parser until it is complete, bounded by the limits. A sequence of several segments is copied once into pooled chunk buffers. Whatever the input kind, the whole tree is held in memory, so `MaxDocumentCharacters` and `MaxNodeCount` bound its size.
- **Namespace processing cannot be switched off.** An unbound prefix is an error.

## Requirements

- .NET Standard 2.0, .NET 10, or .NET 11 (the package targets `netstandard2.0`, `net10.0`, and `net11.0` and behaves the same on all of them; the tests run on `net5.0`, `net10.0`, and `net11.0`, and `net5.0` runs the `netstandard2.0` build)
- `NetEvolve.Schemas` and `NetEvolve.Arguments` as dependencies; on `netstandard2.0` also `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`
- The extension member `SchemaValidator.ForDtd()` needs C# 14; an older compiler calls `DtdSchemaValidatorExtensions.ForDtd()`

## Related Packages

- [**NetEvolve.Schemas**](https://www.nuget.org/packages/NetEvolve.Schemas/) - Core abstractions: validation limits, resource resolvers, and the validator anchor
- [**NetEvolve.Schemas.XPath**](https://www.nuget.org/packages/NetEvolve.Schemas.XPath/) - XPath 2.0 engine that evaluates expressions on the documents of this package
- [**NetEvolve.Schemas.Xsd**](https://www.nuget.org/packages/NetEvolve.Schemas.Xsd/) - XML Schema (XSD) validation
- [**NetEvolve.Schemas.Schematron**](https://www.nuget.org/packages/NetEvolve.Schemas.Schematron/) - ISO Schematron validation

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
