namespace NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The validation step for the validity constraint Element Valid (XML 1.0, section 3; design #122, XML-63): every element
/// has a declaration and its content matches it. EMPTY allows nothing at all (not even a comment, a processing instruction,
/// a reference, or white space), ANY allows anything, mixed content allows text and the listed element types, element
/// content allows the children in the order of the content model with comments, processing instructions, and white space
/// between them but no character data.
/// </summary>
/// <remarks>
/// <para>
/// Every content model is compiled once at <see cref="BeginDocument"/> into a position automaton (see
/// <see cref="DtdContentModelCompiler"/>), and a non-deterministic model is reported there as <see cref="DtdValidityCodes.XML5911"/>
/// by the mode of <see cref="DtdValidationOptions.NonDeterministicContentModels"/>; the language is the same in every mode.
/// A model that exceeds <see cref="DtdValidationLimits.MaxContentModelStates"/> or the size bound of the compiler stops the
/// run with <see cref="DtdLimitCodes.XML1118"/>. Matching a document allocates nothing beyond the first growth of the
/// stacks. Each element reports at most one issue for its own content, at the first offending item, so one mistake does not
/// cascade.
/// </para>
/// <para>
/// Limitation: the reader reports a line break and a character reference to a line feed as the same event (kind
/// <see cref="XmlTextKind.Reference"/>, text <c>"\n"</c>), so <c>&amp;#10;</c> in element content is accepted as white
/// space. Every other reference text and every CDATA section is character data (design #122, D11). Declaration-time
/// reports use <see cref="DtdValidationContext.DocumentTypeLocation"/> until per-declaration locations exist (#608).
/// </para>
/// <para>Not thread-safe; one instance validates one document at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class DtdContentModelStep : IDtdValidationStep
{
    private DtdValidationContext? _context;
    private SpanInterningNameTable _names = new(int.MaxValue);
    private DtdContentElement?[] _elements = [];
    private Frame[] _frames = new Frame[16];
    private int _depth;
    private int[] _pool = new int[16];
    private int _poolTop;
    private int[] _scratch = [];
    private byte[] _mark = [];

    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _depth = 0;
        _poolTop = 0;
        _names = new SpanInterningNameTable(int.MaxValue);
        _elements = [];

        var compiler = new DtdContentModelCompiler(_names, context.Options.DtdLimits.MaxContentModelStates);
        var declared = new List<(int Symbol, DtdContentElement Element)>();
        var mostPositions = 0;
        foreach (var pair in context.Declarations.Elements)
        {
            var element = Compile(context, compiler, pair.Value);
            if (element is null)
            {
                context.Stop(DtdLimitCodes.XML1118, default);
                _context = null;

                return;
            }

            _ = compiler.TryIntern(element.Name, out var symbol);
            declared.Add((symbol, element));
            mostPositions = Math.Max(mostPositions, element.Model?.PositionCount ?? 0);
        }

        _names.Freeze();
        _elements = new DtdContentElement?[_names.Count];
        foreach (var (symbol, element) in declared)
        {
            _elements[symbol] = element;
        }

        _scratch = new int[mostPositions + 1];
        _mark = new byte[mostPositions + 1];
    }

    /// <inheritdoc />
    public void StartElement(in ValidationElement element)
    {
        if (_context is null || _context.IsStopped)
        {
            return;
        }

        var symbol = _names.TryGet(element.Name.Span, out var found) ? found.Value : -1;
        var declaration = symbol >= 0 ? _elements[symbol] : null;
        if (_depth > 0)
        {
            MatchChild(ref _frames[_depth - 1], symbol, in element);
        }

        if (declaration is null)
        {
            ReportElement(in element, "element '" + element.Name.ToString() + "' is not declared");
        }

        if (_depth == _frames.Length)
        {
            Array.Resize(ref _frames, _depth * 2);
        }

        var hasModel = declaration?.Model is not null;
        _frames[_depth++] = new Frame(declaration, _poolTop, hasModel ? 1 : 0);
        if (hasModel)
        {
            Reserve(1);
            _pool[_poolTop++] = DtdContentModel.StartPosition;
        }
    }

    /// <inheritdoc />
    // The reference itself is content only for EMPTY; its replacement events arrive as events of this element.
    public void EntityReference(in ValidationEntityReference reference) =>
        ContentOfEmpty(reference.Position, "a reference");

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)XmlTextKind.Text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)XmlTextKind.Reference);

        if (_context is null || _context.IsStopped || _depth == 0 || text.IsEmpty)
        {
            return;
        }

        ref var frame = ref _frames[_depth - 1];
        if (frame.Failed || frame.Element is null)
        {
            return;
        }

        if (frame.Element.Kind == DtdContentKind.Empty)
        {
            Fail(ref frame, position, "text");
        }
        else if (frame.Element.Kind == DtdContentKind.Children)
        {
            var at = FirstCharacterData(text, kind);
            if (at >= 0)
            {
                var offending = new XmlPosition(position.Line, position.Column + at, position.CharOffset + at);
                Fail(ref frame, offending, "character data");
            }
        }
    }

    /// <inheritdoc />
    public void Comment(in XmlPosition position) => ContentOfEmpty(position, "a comment");

    /// <inheritdoc />
    public void ProcessingInstruction(in XmlPosition position) => ContentOfEmpty(position, "a processing instruction");

    /// <inheritdoc />
    public void EndElement(in ValidationElement element)
    {
        if (_context is null || _context.IsStopped || _depth == 0)
        {
            return;
        }

        var frame = _frames[--_depth];
        _poolTop = frame.Start;
        if (
            !frame.Failed
            && frame.Element?.Model is { } model
            && !model.IsAccepting(_pool.AsSpan(frame.Start, frame.Count))
        )
        {
            ReportElement(
                in element,
                "the content of '" + frame.Element.Name + "' ends before its content model is complete"
            );
        }
    }

    /// <inheritdoc />
    public void EndDocument() { }

    // An event that is content only for EMPTY: a comment, a processing instruction, or an entity reference. The pipeline
    // forwards comments and processing instructions only from inside the root element.
    private void ContentOfEmpty(XmlPosition position, string what)
    {
        if (_context is null || _context.IsStopped || _depth == 0)
        {
            return;
        }

        ref var frame = ref _frames[_depth - 1];
        if (!frame.Failed && frame.Element is { Kind: DtdContentKind.Empty })
        {
            Fail(ref frame, position, what);
        }
    }

    // Matches a child element against the content of its parent: EMPTY allows none, ANY every one (the child needs its own
    // declaration), mixed content the listed types, element content the next position of the automaton.
    private void MatchChild(ref Frame parent, int symbol, in ValidationElement child)
    {
        var declaration = parent.Element;
        if (declaration is null || parent.Failed)
        {
            return;
        }

        var allowed = true;
        if (declaration.Kind == DtdContentKind.Empty)
        {
            allowed = false;
        }
        else if (declaration.Kind == DtdContentKind.Mixed)
        {
            allowed = symbol >= 0 && declaration.AllowedChildren!.Contains(symbol);
        }
        else if (declaration.Kind == DtdContentKind.Children)
        {
            var state = _pool.AsSpan(parent.Start, parent.Count);
            var count = declaration.Model!.Step(state, symbol, _scratch, _mark);
            allowed = count > 0;
            if (allowed)
            {
                Reserve(parent.Start + count - _poolTop);
                _scratch.AsSpan(0, count).CopyTo(_pool.AsSpan(parent.Start));
                parent.Count = count;
                _poolTop = parent.Start + count;
            }
        }

        if (!allowed)
        {
            Fail(ref parent, child.Position, "element '" + child.Name.ToString() + "'");
        }
    }

    // The first offending item of an element: the element reports once and ignores the rest of its content.
    private void Fail(ref Frame frame, XmlPosition position, string what)
    {
        frame.Failed = true;
        var name = frame.Element!.Name;
        var detail =
            frame.Element.Kind == DtdContentKind.Empty
                ? "'" + name + "' is declared EMPTY but contains " + what
                : what + " is not allowed in the content of '" + name + "'";
        _context!.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, _context.Locate(position), detail);
    }

    private void ReportElement(in ValidationElement element, string detail) =>
        _context!.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, _context.Locate(element.Position), detail);

    // The offset of the first character that is character data, -1 when there is none. White space is #x20, #x9, #xD, and
    // #xA (production 3). A CDATA section is character data, and so is the text of a reference except the single line
    // feed that the reader reports for every line break (design #122, D11 and the limitation above).
    private static int FirstCharacterData(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (kind == XmlTextKind.CData)
        {
            return 0;
        }

        if (kind == XmlTextKind.Reference)
        {
            return text.Length == 1 && text[0] == '\n' ? -1 : 0;
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is not (' ' or '\t' or '\r' or '\n'))
            {
                return i;
            }
        }

        return -1;
    }

    private void Reserve(int extra)
    {
        var needed = _poolTop + extra;
        if (needed > _pool.Length)
        {
            Array.Resize(ref _pool, Math.Max(needed, _pool.Length * 2));
        }
    }

    // Builds the matching form of one element type declaration; null when a limit stops the compilation.
    private static DtdContentElement? Compile(
        DtdValidationContext context,
        DtdContentModelCompiler compiler,
        DtdElementDeclaration declaration
    )
    {
        if (declaration.ContentKind == DtdContentKind.Mixed)
        {
            var allowed = new HashSet<int>();
            if (declaration.Content is not null)
            {
                foreach (var item in declaration.Content.Items)
                {
                    if (
                        item.Kind == DtdParticleKind.Name
                        && compiler.TryIntern(item.Name ?? string.Empty, out var symbol)
                    )
                    {
                        _ = allowed.Add(symbol);
                    }
                }
            }

            return new DtdContentElement(declaration.Name, DtdContentKind.Mixed, null, allowed);
        }

        if (declaration.ContentKind != DtdContentKind.Children)
        {
            return new DtdContentElement(declaration.Name, declaration.ContentKind, null, null);
        }

        if (!compiler.TryCompile(declaration.Content, out var model))
        {
            return null;
        }

        ReportDeterminism(context, declaration.Name, model!, compiler);

        return new DtdContentElement(declaration.Name, DtdContentKind.Children, model, null);
    }

    private static void ReportDeterminism(
        DtdValidationContext context,
        string element,
        DtdContentModel model,
        DtdContentModelCompiler compiler
    )
    {
        var mode = context.Options.NonDeterministicContentModels;
        if (model.IsDeterministic || mode == DtdNonDeterminism.Accept)
        {
            return;
        }

        var severity = mode == DtdNonDeterminism.Reject ? IssueSeverity.Error : IssueSeverity.Warning;
        var detail =
            "content model of '"
            + element
            + "': '"
            + compiler.NameOf(model.AmbiguousName)
            + "' can match more than one occurrence";
        context.Report(DtdValidityCodes.XML5911, severity, default, detail);
    }

    private struct Frame
    {
        internal Frame(DtdContentElement? element, int start, int count)
        {
            Element = element;
            Start = start;
            Count = count;
            Failed = false;
        }

        internal DtdContentElement? Element;

        internal int Start;

        internal int Count;

        internal bool Failed;
    }
}
