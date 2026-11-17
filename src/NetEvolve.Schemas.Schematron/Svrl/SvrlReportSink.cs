namespace NetEvolve.Schemas.Schematron.Svrl;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xml.Writer;

/// <summary>Writes the events of a run as SVRL through the Xml package writer.</summary>
/// <remarks>
/// <para>
/// The sink writes 2016-compatible SVRL plus the attributes and elements of the public 2025 SVRL grammar. The grammar puts
/// an <c>error</c> of a pattern into its <c>active-pattern</c> or <c>active-group</c> element, which precedes the
/// <c>fired-rule</c> and finding elements, so the events of one active pattern are held in a pooled buffer and written when
/// the pattern ends. The buffer is capped by <see cref="SvrlWriterOptions.MaxBufferedBytes"/>: on overflow the fired rules and
/// findings of that pattern are dropped, an <c>error</c> with code <c>NES3500</c> takes their place, and the validation result
/// is not affected, so SVRL can lack findings that the result holds.
/// </para>
/// <para>
/// Completion: the buffer writer path is complete at <see cref="Finish"/>; the stream path is complete when
/// <see cref="CompleteAsync"/> (or <see cref="DisposeAsync"/> after a finished run) wrote the bytes. <see cref="Dispose"/>
/// never writes and never throws; it leaves <see cref="IsCompleted"/> <see langword="false"/> for an uncompleted stream run.
/// </para>
/// <para>
/// Limitations: <c>schematronEdition</c> is written for the 2025 edition only because the run information holds the edition,
/// not the attribute text, so an explicit <c>2016</c> reads as an absent attribute; an SVRL <c>id</c> is written only for an
/// author id, never for a generated or expanded one; a <c>svrl:error</c> in the output is not a failure of the run, and the
/// text of a message that holds characters XML 1.0 forbids makes the Xml writer throw.
/// </para>
/// </remarks>
public sealed class SvrlReportSink : ISchematronReportSink, IAsyncDisposable, IDisposable
{
    private const int MaxDistinctNames = 4096;
    private const int MaxDepth = 4;
    private const char PatternKind = 'P';
    private const char GroupKind = 'G';
    private const char RuleKind = 'F';
    private const char AssertKind = 'A';
    private const char ReportKind = 'R';
    private const char ErrorKind = 'E';

    // One line break and two spaces per nesting level.
    private static readonly string Layout = "\n" + new string(' ', 2 * MaxDepth);

    private readonly SpanInterningNameTable _table = new(MaxDistinctNames);
    private readonly SvrlNames _names;
    private readonly XmlWriter _xml;
    private readonly bool _isStream;
    private readonly SvrlBuffer _head = new();
    private readonly SvrlBuffer _body = new();
    private readonly bool[] _hasChild = new bool[MaxDepth];
    private int _depth;
    private int _headBase;
    private bool _begun;
    private bool _ended;
    private bool _disposed;
    private bool _patternOpen;
    private bool _overflowed;
    private bool _namespacesWritten;
    private string _document = string.Empty;
    private IReadOnlyList<SchematronNamespace> _namespaces = [];

    /// <summary>Initializes a new instance of the <see cref="SvrlReportSink"/> class that writes through a buffer writer; complete at <see cref="Finish"/>.</summary>
    /// <param name="output">The output.</param>
    /// <param name="options">The options, <see cref="SvrlWriterOptions.Default"/> when <see langword="null"/>.</param>
    public SvrlReportSink(IBufferWriter<byte> output, SvrlWriterOptions? options = null)
    {
        Argument.ThrowIfNull(output);

        Options = options ?? SvrlWriterOptions.Default;
        _names = new SvrlNames(_table);
        _xml = new XmlWriter(output, _table, new XmlWriterOptions(encoding: Options.Encoding));
    }

    /// <summary>Initializes a new instance of the <see cref="SvrlReportSink"/> class that buffers and writes to a stream at <see cref="CompleteAsync"/>.</summary>
    /// <param name="output">The output. The caller keeps ownership.</param>
    /// <param name="options">The options, <see cref="SvrlWriterOptions.Default"/> when <see langword="null"/>.</param>
    public SvrlReportSink(Stream output, SvrlWriterOptions? options = null)
    {
        Argument.ThrowIfNull(output);

        Options = options ?? SvrlWriterOptions.Default;
        _names = new SvrlNames(_table);
        _xml = new XmlWriter(output, _table, new XmlWriterOptions(encoding: Options.Encoding));
        _isStream = true;
    }

    /// <summary>Gets a value indicating whether the output was fully written.</summary>
    public bool IsCompleted { get; private set; }

    internal SvrlWriterOptions Options { get; }

    /// <inheritdoc />
    public void Begin(in SchematronRunInfo info)
    {
        Enter();
        if (_begun)
        {
            throw new InvalidOperationException("Begin was already called.");
        }

        _begun = true;
        _document = info.InstanceSystemId.ToString();
        _namespaces = info.Namespaces;

        _xml.StartDocument(new XmlDeclaration("1.0", string.Empty, null));
        Open(_names.Output);
        _xml.NamespaceDeclaration(_names.PrefixSymbol, _names.NamespaceSymbol);
        if (Options.EmitNamespaceDeclarations)
        {
            DeclareSchemaNamespaces();
        }

        Attribute(_names.Title, info.Title);
        if (!info.Phase.IsEmpty && info.Phase[0] != '#')
        {
            _xml.Attribute(_names.Phase, info.Phase);
        }

        Attribute(_names.SchemaVersion, info.SchemaVersion);
        if (info.Edition == SchematronEdition.Iso2025)
        {
            _xml.Attribute(_names.SchematronEdition, "2025");
        }

        _xml.EndAttributes();
    }

    /// <inheritdoc />
    public void ActivePattern(in SchematronPatternEvent pattern)
    {
        EnterRun();
        if (_patternOpen)
        {
            FlushPattern();
        }

        if (!_namespacesWritten)
        {
            WriteNamespaceReports();
        }

        _patternOpen = true;
        _overflowed = false;
        _head.Clear();
        _body.Clear();
        _head.PutKind(pattern.IsGroup ? GroupKind : PatternKind);
        _head.PutText(pattern.Id);
        _head.PutText(pattern.Documents);
        _head.PutText(pattern.Name);
        _head.PutText(pattern.Role);
        _headBase = _head.ByteCount;
    }

    /// <inheritdoc />
    public void FiredRule(in SchematronRuleEvent rule)
    {
        EnterPattern();
        if (_overflowed)
        {
            return;
        }

        _body.PutKind(RuleKind);
        _body.PutText(rule.Id);
        _body.PutText(rule.Context);
        _body.PutText(rule.Role);
        _body.PutText(rule.Flags);
        _body.PutText(rule.Document);
        CheckBuffer();
    }

    /// <inheritdoc />
    public void Finding(in SchematronFinding finding)
    {
        EnterPattern();
        if (_overflowed)
        {
            return;
        }

        _body.PutKind(finding.IsReport ? ReportKind : AssertKind);
        _body.PutText(finding.Id);
        _body.PutText(finding.Location);
        _body.PutText(finding.Test);
        _body.PutText(finding.Role);
        _body.PutText(finding.Flags);
        _body.PutText(finding.Severity);
        _body.PutText(finding.Message);
        PutReferences(finding.Diagnostics);
        PutReferences(finding.Properties);
        CheckBuffer();
    }

    /// <inheritdoc />
    public void ErrorOccurred(in SchematronErrorEvent errorEvent)
    {
        EnterRun();
        if (!_patternOpen)
        {
            WriteError(errorEvent.Document, errorEvent.Location, errorEvent.Code, errorEvent.Message);
            return;
        }

        if (_overflowed)
        {
            return;
        }

        PutError(errorEvent.Document, errorEvent.Location, errorEvent.Code, errorEvent.Message);
        CheckBuffer();
    }

    /// <inheritdoc />
    public void Finish(in SchematronRunSummary summary)
    {
        EnterRun();
        if (_patternOpen)
        {
            FlushPattern();
        }

        Close(_names.Output);
        if (summary.IsTruncated)
        {
            _xml.Comment(" evaluation stopped, code " + summary.StopCode.ToString() + " ");
        }

        _xml.EndDocument();
        _ended = true;
        IsCompleted = !_isStream;
    }

    /// <summary>Flushes the buffered output of a <see cref="Stream"/> writer asynchronously.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the output was written.</returns>
    /// <exception cref="InvalidOperationException">The run has not ended.</exception>
    /// <exception cref="ObjectDisposedException">The sink is disposed.</exception>
    public async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (IsCompleted)
        {
            return;
        }

        if (!_ended)
        {
            throw new InvalidOperationException("The run has not ended; there is nothing to complete.");
        }

        await _xml.FlushAsync(cancellationToken).ConfigureAwait(false);
        IsCompleted = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_ended && !IsCompleted)
            {
                await CompleteAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _xml.Dispose();
        _head.Dispose();
        _body.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw Disposed();
        }
    }

    // ObjectDisposedException.ThrowIf does not exist on netstandard (decision #449), so the throw stays explicit.
    private static ObjectDisposedException Disposed() => new(nameof(SvrlReportSink));

    private void Enter()
    {
        ThrowIfDisposed();
        if (_ended)
        {
            throw new InvalidOperationException("The run has ended.");
        }
    }

    private void EnterRun()
    {
        Enter();
        if (!_begun)
        {
            throw new InvalidOperationException("Begin was not called.");
        }
    }

    private void EnterPattern()
    {
        EnterRun();
        if (!_patternOpen)
        {
            throw new InvalidOperationException("A rule or finding needs an active pattern.");
        }
    }

    private void CheckBuffer()
    {
        if (_head.ByteCount - _headBase + _body.ByteCount <= Options.MaxBufferedBytes)
        {
            return;
        }

        _overflowed = true;
        _body.Clear();
        PutError(
            _document,
            "/",
            SchematronDiagnosticCodes.SvrlBufferLimit.ToString(),
            "The SVRL buffer of this pattern exceeded MaxBufferedBytes; its fired rules and findings are not written."
        );
    }

    private void PutError(
        ReadOnlySpan<char> document,
        ReadOnlySpan<char> location,
        ReadOnlySpan<char> code,
        ReadOnlySpan<char> message
    )
    {
        _head.PutKind(ErrorKind);
        _head.PutText(document);
        _head.PutText(location);
        _head.PutText(code);
        _head.PutText(message);
    }

    private void PutReferences(SchematronReferenceList references)
    {
        _body.PutNumber(references.Count);
        for (var i = 0; i < references.Count; i++)
        {
            _body.PutText(references.IdAt(i));
            _body.PutText(references.TextAt(i));
        }
    }

    private void DeclareSchemaNamespaces()
    {
        for (var i = 0; i < _namespaces.Count; i++)
        {
            var prefix = _namespaces[i].Prefix;
            if (!IsReportable(_namespaces[i]) || prefix is SvrlNames.Prefix or "xml" or "xmlns" || IsRepeated(i))
            {
                continue;
            }

            _xml.NamespaceDeclaration(_table.Intern(prefix), _table.Intern(_namespaces[i].NamespaceName));
        }
    }

    private bool IsRepeated(int index)
    {
        for (var i = 0; i < index; i++)
        {
            if (string.Equals(_namespaces[i].Prefix, _namespaces[index].Prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReportable(SchematronNamespace ns) => ns.Prefix.Length > 0 && ns.NamespaceName.Length > 0;

    // Only namespaces from the schema need to be reported; the grammar places them after the top-level errors.
    private void WriteNamespaceReports()
    {
        _namespacesWritten = true;
        for (var i = 0; i < _namespaces.Count; i++)
        {
            if (!IsReportable(_namespaces[i]))
            {
                continue;
            }

            Open(_names.NamespaceReport);
            _xml.Attribute(_names.PrefixAttribute, _namespaces[i].Prefix);
            _xml.Attribute(_names.Uri, _namespaces[i].NamespaceName);
            _xml.EndAttributes();
            Close(_names.NamespaceReport);
        }
    }

    private void FlushPattern()
    {
        var head = new SvrlBufferReader(_head.Written);
        var name = head.ReadKind() == GroupKind ? _names.ActiveGroup : _names.ActivePattern;
        Open(name);
        WriteId(head.ReadText());
        Attribute(_names.Documents, head.ReadText());
        Attribute(_names.Name, head.ReadText());
        Attribute(_names.Role, head.ReadText());
        _xml.EndAttributes();
        while (!head.AtEnd)
        {
            _ = head.ReadKind();
            var document = head.ReadText();
            var location = head.ReadText();
            var code = head.ReadText();
            WriteError(document, location, code, head.ReadText());
        }

        Close(name);

        var body = new SvrlBufferReader(_body.Written);
        while (!body.AtEnd)
        {
            var kind = body.ReadKind();
            if (kind == RuleKind)
            {
                WriteFiredRule(ref body);
            }
            else
            {
                WriteFinding(kind == ReportKind ? _names.SuccessfulReport : _names.FailedAssert, ref body);
            }
        }

        _head.Clear();
        _body.Clear();
    }

    private void WriteFiredRule(ref SvrlBufferReader reader)
    {
        Open(_names.FiredRule);
        WriteId(reader.ReadText());
        _xml.Attribute(_names.Context, reader.ReadText());
        Attribute(_names.Role, reader.ReadText());
        Attribute(_names.Flag, reader.ReadText());
        Attribute(_names.Document, reader.ReadText());
        _xml.EndAttributes();
        Close(_names.FiredRule);
    }

    private void WriteFinding(XmlName element, ref SvrlBufferReader reader)
    {
        Open(element);
        WriteId(reader.ReadText());
        _xml.Attribute(_names.Location, reader.ReadText());
        _xml.Attribute(_names.Test, reader.ReadText());
        Attribute(_names.Role, reader.ReadText());
        Attribute(_names.Flag, reader.ReadText());
        Attribute(_names.Severity, reader.ReadText());
        var message = reader.ReadText();
        _xml.EndAttributes();
        WriteReferences(_names.DiagnosticReference, _names.Diagnostic, ref reader);
        WriteReferences(_names.PropertyReference, _names.Property, ref reader);
        WriteText(message);
        Close(element);
    }

    private void WriteReferences(XmlName element, XmlName attribute, ref SvrlBufferReader reader)
    {
        for (var count = reader.ReadNumber(); count > 0; count--)
        {
            Open(element);
            _xml.Attribute(attribute, reader.ReadText());
            _xml.EndAttributes();
            WriteText(reader.ReadText());
            Close(element);
        }
    }

    private void WriteText(ReadOnlySpan<char> text)
    {
        Open(_names.Text);
        _xml.EndAttributes();
        _xml.Text(text, XmlTextKind.Text);
        Close(_names.Text);
    }

    private void WriteError(
        ReadOnlySpan<char> document,
        ReadOnlySpan<char> location,
        ReadOnlySpan<char> code,
        ReadOnlySpan<char> message
    )
    {
        Open(_names.Error);
        _xml.Attribute(_names.Document, document);
        _xml.Attribute(_names.Location, location);
        Attribute(_names.Code, code);
        _xml.EndAttributes();
        _xml.Text(message, XmlTextKind.Text);
        Close(_names.Error);
    }

    // A generated or expanded identifier holds a hash sign, which no NCName holds, so it never reaches the id attribute.
    private void WriteId(ReadOnlySpan<char> id)
    {
        if (!id.IsEmpty && id.IndexOf('#') < 0)
        {
            _xml.Attribute(_names.Id, id);
        }
    }

    private void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        if (!value.IsEmpty)
        {
            _xml.Attribute(name, value);
        }
    }

    private void Open(XmlName name)
    {
        if (_depth > 0)
        {
            _hasChild[_depth - 1] = true;
            NewLine(_depth);
        }

        _xml.StartElement(name, default);
        _depth++;
    }

    private void Close(XmlName name)
    {
        _depth--;
        if (_hasChild[_depth])
        {
            _hasChild[_depth] = false;
            NewLine(_depth);
        }

        _xml.EndElement(name);
    }

    private void NewLine(int depth)
    {
        if (Options.Indent)
        {
            _xml.Text(Layout.AsSpan(0, 1 + (2 * depth)), XmlTextKind.Text);
        }
    }
}
