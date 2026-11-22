# NetEvolve.Schemas.Xml

Reads XML 1.0 (Fifth Edition) and Namespaces in XML 1.0 (Third Edition) from characters or bytes into an immutable document tree with a line and column on every node. The input can be a span, memory, a sequence, or a stream. The defaults refuse a document type declaration and bound every resource that hostile input can inflate.

The package builds on `NetEvolve.Schemas` and `NetEvolve.Arguments`. It targets `netstandard2.0`, `netstandard2.1`, `net10.0`, and `net11.0` and behaves the same on all of them. On `netstandard2.0` it also depends on `Microsoft.Bcl.AsyncInterfaces`, `System.Memory`, and `System.Threading.Tasks.Extensions`.

## Install

```shell
dotnet add package NetEvolve.Schemas.Xml
```

No package has been released yet; the line applies from the first release.

## Input kinds

`XmlDocumentLoader` is the one entry for building an `XmlDocumentTree`. Every input kind gives the same tree, the same positions, and, for a malformed document, the same error code and position. A byte input is decoded from its byte order mark and its encoding declaration (XML 1.0, Appendix F).

| Input | Method | Reader behind it |
| --- | --- | --- |
| `ReadOnlySpan<char>`, `ReadOnlySpan<byte>` | `Load` | `XmlPipelineReader` |
| `ReadOnlyMemory<char>`, `ReadOnlyMemory<byte>` | `LoadFromMemory` | `XmlPipelineReader` |
| `ReadOnlySequence<char>`, `ReadOnlySequence<byte>` | `Load` | `XmlSequenceReader` |
| `Stream` | `LoadAsync` | `XmlStreamReader<TSink>` |

Every method takes optional `XmlReaderOptions` and an optional `baseUri`, which becomes the base URI of the document node. A document that is not well formed, that uses an unsupported encoding, or that exceeds a limit throws `XmlParseException`. Its `Diagnostic` carries the error code and the position.

The readers are public as well. They push events into an `IXmlSink` and build no tree, so a caller that only scans a document pays for no tree.

The samples in this readme need these namespaces. `cancellationToken` is a `CancellationToken`.

```
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
```

### Span

```csharp
var tree = XmlDocumentLoader.Load("<order id=\"7\">\n  <item>tea</item>\n</order>".AsSpan());

var order = tree.Document.FirstChild;
var name = tree.Names.GetString(order.Name.Local);
var item = order.FirstChild.NextSibling;
var text = item.StringValue;
var position = tree.GetPosition(item.Index);
```

Here `name` is `order`, `text` is `tea`, and `position` is line 2, column 3. Lines and columns are one-based. The document node is `tree.Document`; navigate from a node with `FirstChild`, `NextSibling`, `FirstAttribute`, and `NextAttribute`. A name holds symbols of the name table of the tree, so read its text through `tree.Names`.

### Memory

```csharp
ReadOnlyMemory<byte> utf8 = Encoding.UTF8.GetBytes("<order id=\"7\"><item>tea</item></order>");

var tree = XmlDocumentLoader.LoadFromMemory(utf8);
var text = tree.Document.FirstChild.FirstChild.StringValue;
```

Here `text` is `tea`.

### Sequence

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

### Async stream

```csharp
await using var stream = new MemoryStream("<order id=\"7\"><item>tea</item></order>"u8.ToArray());

var tree = await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken);
var text = tree.Document.FirstChild.FirstChild.StringValue;
```

Here `text` is `tea`. The stream is read from its current position to its end and stays open. A canceled token throws `OperationCanceledException`.

## Security defaults

`XmlReaderOptions.Default` is the secure choice. Every other setting is an explicit choice of the caller.

### A document type declaration is rejected

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

### No external access

The public options have no resolver and no switch for external access. The external subset, the external parameter entities, and the external general entities are never read, and nothing is fetched or opened, whatever the document says. With `DtdProcessing` on:

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

### Limits

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

## Conformance

The reader is checked against the W3C XML Conformance Test Suite (`xmlts20130923`), which the conformance tests of the repository fetch by checksum. The suite has 2,585 cases. 2,001 of them apply to XML 1.0 (Fifth Edition) and Namespaces in XML 1.0, and 584 cases are excluded:

| Excluded class | Cases | Reason |
| --- | --- | --- |
| XML 1.1 | 266 | XML 1.1 is not supported. |
| Namespaces in XML 1.1 | 8 | XML 1.1 is not supported. |
| XML 1.0 editions one to four only | 310 | The reader follows the name rules of the Fifth Edition. |

**Rate: 98.8%** (1,977 of 2,001 applicable cases) at the milestone Phase 1 – XML core. The rate is passed / (passed + failed); excluded cases are not counted, and the rate is truncated, never rounded up, to one decimal. A not-well-formed case passes when the reader reports a fatal error. A valid or invalid case passes when the reader accepts the document, because the reader does not validate. The run reads with DTD processing on, and it reads the external subset and the external entities through a resolver that is confined to the suite directory, which the public options do not expose.

24 cases fail. They are listed in the committed baseline `tests/conformance/baselines/xmlconf`, and a change that adds a failure fails the build:

| Failure class | Cases | Reason |
| --- | --- | --- |
| Namespace processing off | 9 | The case needs namespace processing switched off; the reader has no such mode. |
| Document missing from the archive | 9 | The case names a document that the pinned archive does not contain. |
| Name above the default limit | 2 | The document has a name longer than the default `MaxNameLength` and is rejected with XML1105. |
| Version other than 1.0 | 1 | The document declares version 1.7, which the Fifth Edition allows; the reader accepts 1.0 only and reports XML0010. |
| Entity used before its declaration | 2 | A default value refers to an entity that is declared later and is accepted ([#878](https://github.com/dailydevops/schemas/issues/878)). |
| Invalid public identifier | 1 | A public identifier with a character outside `PubidChar` is accepted ([#931](https://github.com/dailydevops/schemas/issues/931)). |

## Limitations

- **XML 1.1 is not supported.** A document that declares another version than 1.0 is rejected with `XML0010`. That includes versions such as 1.7 that the Fifth Edition of XML 1.0 allows.
- **DTD processing is off by default.** A document type declaration is rejected with `XML2001` unless `WithDtdProcessing(true)` is set. Even then only the internal subset is read, and no external resource is ever opened (see [No external access](#no-external-access)). The notations and the unparsed entities of the declaration are not added to the tree ([#731](https://github.com/dailydevops/schemas/issues/731)).
- **The reader does not validate.** `SchemaValidator.ForDtd()` is declared for DTD validation, but its `Build` and `BuildAsync` throw `NotSupportedException` until DTD validation is available.
- **Span inputs are synchronous.** `Load` and `LoadFromMemory` run on the calling thread, and so does the load of a sequence. Only `LoadAsync` is asynchronous. A span is copied or decoded during the call; memory and a sequence are read during the call only, so they must stay valid and unchanged until it returns.
- **The stream path buffers.** `LoadAsync` reads the stream to its end through pooled buffers in chunks and decodes them as it goes. A token that spans a chunk edge waits in the parser until it is complete, bounded by the limits. A sequence of several segments is copied once into pooled chunk buffers. Whatever the input kind, the whole tree is held in memory, so `MaxDocumentCharacters` and `MaxNodeCount` bound its size.
- **Namespace processing cannot be switched off.** An unbound prefix is an error.
