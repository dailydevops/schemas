namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;

internal sealed partial class StreamingValidator
{
    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        _inText = false;
        _ordinal++;
        if (_skip > 0)
        {
            _skip++;
            return;
        }

        _attributeCount = 0;
        _attributeLength = 0;
        var location = new DocumentLocation(_documentUri, position.Line, position.Column);
        var id = _binding.Bind(name, out var qname);
        if (_depth == 0)
        {
            StartRoot(name, qname, location);
        }
        else
        {
            StartChild(_frames[_depth - 1], name, id, qname, location);
        }
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        _inText = false;
        if (_skip > 0)
        {
            _skip--;
            return;
        }

        var frame = _frames[_depth - 1];
        switch (frame.Content)
        {
            case FrameContent.SimpleType:
            case FrameContent.SimpleContent:
                EndSimpleContent(frame);
                break;
            case FrameContent.ElementOnly:
            case FrameContent.Mixed:
                EndElementContent(frame);
                break;
            default:
                break;
        }

        if (_onEnd.Length > 0)
        {
            var info = new ElementEndInfo(frame.Ordinal, frame.IsValid, frame.IsNilled, frame.Type);
            for (var i = 0; i < _onEnd.Length; i++)
            {
                _onEnd[i].EndElement(info);
            }
        }

        Pop(frame);
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (_skip > 0 || _depth == 0 || text.Length == 0)
        {
            return;
        }

        var frame = _frames[_depth - 1];
        if (!_inText)
        {
            _inText = true;
            _textStart = Here(frame.Location);
            if (!frame.HasText)
            {
                frame.TextLocation = _textStart;
            }
        }

        frame.HasText = true;
        switch (frame.Content)
        {
            case FrameContent.SimpleType:
            case FrameContent.SimpleContent:
                if (!frame.ContentFailed)
                {
                    AppendText(text);
                }

                break;
            case FrameContent.Empty:
                if (!frame.TextReported)
                {
                    frame.TextReported = true;
                    frame.ContentFailed = true;
                    Invalid(
                        frame,
                        _textStart,
                        ValidationRules.ComplexType,
                        "The content type of element "
                            + Display(frame.QName)
                            + " is empty, so it must not have character data.",
                        "cvc-complex-type.2.1"
                    );
                }

                break;
            case FrameContent.Nilled:
                ReportNilledText(frame);
                break;
            case FrameContent.ElementOnly:
                if (!frame.TextReported && !IsWhitespace(text))
                {
                    frame.TextReported = true;
                    Invalid(
                        frame,
                        _textStart,
                        ValidationRules.ComplexType,
                        "The content type of element "
                            + Display(frame.QName)
                            + " is element-only, so it must not have character data other than white space.",
                        "cvc-complex-type.2.3"
                    );
                }

                break;
            default:
                break;
        }
    }

    private void StartRoot(XmlName name, XPathQName qname, DocumentLocation location)
    {
        if (TryGetGlobalElement(qname, out var declaration))
        {
            Open(name, qname, location, declaration);
            return;
        }

        // The root is an error unless its xsi:type names a type that assesses it; that is known at the end of the attributes.
        Push(name, qname, location).UndeclaredRoot = true;
    }

    private void StartChild(ElementFrame parent, XmlName name, int id, XPathQName qname, DocumentLocation location)
    {
        if (parent.ContentFailed)
        {
            _skip = 1;
            return;
        }

        switch (parent.Content)
        {
            case FrameContent.Unassessed:
                if (TryGetGlobalElement(qname, out var declaration))
                {
                    Open(name, qname, location, declaration);
                }
                else
                {
                    _ = Push(name, qname, location);
                }

                break;
            case FrameContent.Empty:
            case FrameContent.SimpleContent:
            case FrameContent.SimpleType:
            case FrameContent.Nilled:
                RejectChild(parent, qname, location);
                break;
            default:
                MatchChild(parent, name, id, qname, location);
                break;
        }
    }

    private void RejectChild(ElementFrame parent, XPathQName qname, DocumentLocation location)
    {
        var (rule, clause, reason) = parent.Content switch
        {
            FrameContent.Empty => (ValidationRules.ComplexType, "cvc-complex-type.2.1", "is empty"),
            FrameContent.SimpleContent => (ValidationRules.ComplexType, "cvc-complex-type.2.2", "is a simple type"),
            FrameContent.Nilled => (ValidationRules.Elt, "cvc-elt.3.2.1", "has xsi:nil true"),
            _ => (ValidationRules.Type, "cvc-type.3.1.2", "is a simple type"),
        };
        var subject = parent.Content switch
        {
            FrameContent.SimpleType => "The type of element ",
            FrameContent.Nilled => "The element ",
            _ => "The content type of element ",
        };
        Reject(
            parent,
            location,
            rule,
            subject
                + Display(parent.QName)
                + " "
                + reason
                + ", so it must not have the child element "
                + Display(qname)
                + ".",
            clause
        );
    }

    private void MatchChild(ElementFrame parent, XmlName name, int id, XPathQName qname, DocumentLocation location)
    {
        var program = parent.Program;
        if (program is null)
        {
            _skip = 1;
            return;
        }

        if (!TryStep(parent, id, qname, out var matched, out var byOpenContent))
        {
            // Limitation: the run ceiling has no catalog code of its own; the nearest engine limit is reported (#610).
            if (parent.Counting is { CeilingExceeded: true })
            {
                Reject(
                    parent,
                    location,
                    ValidationRules.RunCeiling,
                    "The content of element "
                        + Display(parent.QName)
                        + " could not be assessed at the child element "
                        + Display(qname)
                        + ": the counted occurrences nest deeper than the internal ceiling of "
                        + CountingRun.MaxConfigurations.ToString(CultureInfo.InvariantCulture)
                        + " simultaneous configurations.",
                    null
                );
                return;
            }

            Reject(
                parent,
                location,
                ValidationRules.ComplexType,
                "Invalid content was found starting with element " + Display(qname) + ". " + Expected(parent, false),
                "cvc-complex-type.2.4"
            );
            return;
        }

        if (byOpenContent)
        {
            OpenWildcard(parent.Open.Wildcard, name, qname, location);
            return;
        }

        var term = program.Terms[matched.ParticleIndex];
        if (term is ElementDeclaration element)
        {
            // A member of the substitution group of the declaration at this position has a declaration of its own.
            Open(
                name,
                qname,
                location,
                element.Name != qname && TryGetGlobalElement(qname, out var member) ? member : element
            );
        }
        else
        {
            OpenWildcard((IWildcard)term, name, qname, location);
        }
    }

    private static bool TryStep(
        ElementFrame frame,
        int id,
        XPathQName qname,
        out MatchedParticle matched,
        out bool byOpenContent
    )
    {
        byOpenContent = false;
        if (frame.Open.IsActive)
        {
            return frame.Open.TryStep(id, qname, out matched, out byOpenContent);
        }

        if (frame.Counting is { } counting)
        {
            return counting.TryStep(id, qname, out matched);
        }

        return frame.AllRun is { } all ? all.TryStep(id, out matched) : frame.AllRun11!.TryStep(id, qname, out matched);
    }

    private void OpenWildcard(IWildcard wildcard, XmlName name, XPathQName qname, DocumentLocation location)
    {
        if (wildcard.ProcessContents == XsdProcessContents.Skip)
        {
            _skip = 1;
        }
        else if (TryGetGlobalElement(qname, out var declaration))
        {
            Open(name, qname, location, declaration);
        }
        else if (wildcard.ProcessContents == XsdProcessContents.Lax)
        {
            _ = Push(name, qname, location);
        }
        else
        {
            // A strict wildcard still assesses the element when its xsi:type names a type; that is known at the end of the
            // attributes (BeginContent), which reports cvc-wildcard.2 otherwise.
            Push(name, qname, location).UndeclaredStrict = true;
        }
    }

    // The type is chosen at the end of the attributes (BeginContent), because xsi:type may replace the declared one.
    private void Open(XmlName name, XPathQName qname, DocumentLocation location, ElementDeclaration declaration) =>
        Declare(Push(name, qname, location), declaration);

    private void Declare(ElementFrame frame, ElementDeclaration declaration)
    {
        frame.Declaration = declaration;
        if (declaration.IsAbstract)
        {
            Invalid(
                frame,
                ValidationRules.Elt,
                "The element declaration " + Display(declaration.Name) + " is abstract.",
                "cvc-elt.2"
            );
        }
    }

    // Sets up the content of a frame for its type; the content model run is acquired only for an element that is not nilled.
    private void OpenType(ElementFrame frame, IXsdComponent type)
    {
        frame.Type = type;
        switch (type)
        {
            case ComplexTypeDefinition complex:
                OpenComplex(frame, complex);
                break;
            case XsdBuiltInType { Kind: XsdComponentKind.ComplexType }:
                OpenComplex(frame, ComplexTypeDefinition.AnyType);
                break;
            default:
                frame.Content = FrameContent.SimpleType;
                frame.SimpleType = type;
                _textLength = 0;
                break;
        }
    }

    private void OpenComplex(ElementFrame frame, ComplexTypeDefinition complex)
    {
        frame.Complex = complex;
        switch (complex.ContentKind)
        {
            case ComplexContentKind.Empty:
                frame.Content = FrameContent.Empty;
                break;
            case ComplexContentKind.Simple:
                frame.Content = FrameContent.SimpleContent;
                frame.SimpleType = complex.SimpleContentType;
                _textLength = 0;
                break;
            default:
                frame.Content =
                    complex.ContentKind == ComplexContentKind.Mixed ? FrameContent.Mixed : FrameContent.ElementOnly;
                if (!frame.IsNilled)
                {
                    AcquireRun(frame, complex);
                }

                break;
        }
    }

    private void AcquireRun(ElementFrame frame, ComplexTypeDefinition complex)
    {
        if (_model.GetProgram(complex) is not { IsUsable: true } program)
        {
            return;
        }

        frame.Program = program;
        var index = program.PoolIndex;
        if (program.Automaton is { } automaton)
        {
            var pool = _countingPool[index];
            var run = pool is { Count: > 0 } ? pool.Pop() : automaton.CreateRun();
            run.Reset();
            frame.Counting = run;
            frame.Open = complex.OpenContent is { } countingOpen ? new OpenContentRun(countingOpen, run) : default;
        }
        else if (program.AllGroup is { } allGroup)
        {
            var pool = _allPool?[index];
            var run = pool is { Count: > 0 } ? pool.Pop() : allGroup.CreateRun();
            run.Reset();
            frame.AllRun = run;
            frame.Open = complex.OpenContent is { } allOpen ? new OpenContentRun(allOpen, run) : default;
        }
        else
        {
            var pool = _all11Pool?[index];
            var run = pool is { Count: > 0 } ? pool.Pop() : program.AllGroup11!.CreateRun();
            run.Reset();
            frame.AllRun11 = run;
            frame.Open = complex.OpenContent is { } all11Open ? new OpenContentRun(all11Open, run) : default;
        }
    }

    private ElementFrame Push(XmlName name, XPathQName qname, DocumentLocation location)
    {
        if (_depth == _frames.Length)
        {
            Array.Resize(ref _frames, _depth * 2);
        }

        var frame = _frames[_depth] ??= new ElementFrame();
        _depth++;
        frame.Start(name, qname, location, _ordinal, _scope.Count);
        return frame;
    }

    private void Pop(ElementFrame frame)
    {
        _scope.Truncate(frame.NamespaceMark);
        if (frame.Counting is { } counting && frame.Program is { } countingProgram)
        {
            (_countingPool[countingProgram.PoolIndex] ??= new()).Push(counting);
        }
        else if (frame.AllRun is { } all && frame.Program is { } allProgram)
        {
            var pools = _allPool ??= new Stack<AllGroupRun>?[_model.ProgramCount];
            (pools[allProgram.PoolIndex] ??= new()).Push(all);
        }
        else if (frame.AllRun11 is { } all11 && frame.Program is { } all11Program)
        {
            var pools = _all11Pool ??= new Stack<AllGroup11Run>?[_model.ProgramCount];
            (pools[all11Program.PoolIndex] ??= new()).Push(all11);
        }

        _depth--;
        if (_depth > 0 && !frame.IsValid)
        {
            _frames[_depth - 1].IsValid = false;
        }
    }

    private void EndElementContent(ElementFrame frame)
    {
        if (frame.ContentFailed || frame.Program is null)
        {
            return;
        }

        var complete =
            frame.Counting is { } counting ? counting.CanEnd()
            : frame.AllRun is { } all ? all.CanEnd()
            : frame.AllRun11!.CanEnd();
        if (!complete)
        {
            Invalid(
                frame,
                Here(frame.Location),
                ValidationRules.ComplexType,
                "The content of element " + Display(frame.QName) + " is not complete. " + Expected(frame, true),
                "cvc-complex-type.2.4"
            );
        }
    }

    private void Reject(ElementFrame parent, DocumentLocation location, XsdRule rule, string message, string? clause)
    {
        Report(rule, location, message, clause);
        parent.IsValid = false;
        parent.ContentFailed = true;
        _skip = 1;
    }

    private string Expected(ElementFrame frame, bool incomplete)
    {
        Span<int> ids = stackalloc int[MaxExpectedNames];
        var wildcard = false;
        var written = 0;
        // Once the suffix began the content model no longer accepts a child, only the wildcard does.
        var inSuffix = frame.Open.InSuffix;
        if (!inSuffix && frame.Counting is { } counting)
        {
            written = counting.CollectExpected(ids, out wildcard);
        }
        else if (!inSuffix && frame.AllRun is { } all)
        {
            written = incomplete ? all.CollectMissing(ids) : all.CollectExpected(ids, out wildcard);
        }
        else if (!inSuffix && frame.AllRun11 is { } all11)
        {
            written = incomplete ? all11.CollectMissing(ids, out wildcard) : all11.CollectExpected(ids, out wildcard);
        }

        if (!incomplete)
        {
            // At the end of the content only the content model can complete it; open content never does.
            wildcard |= frame.Open.WildcardExpected;
        }

        if (written == 0 && !wildcard)
        {
            return "No child element is expected.";
        }

        var builder = new StringBuilder("One of {");
        for (var i = 0; i < written; i++)
        {
            _ = builder.Append(i == 0 ? string.Empty : ", ").Append(Display(_model.NameOf(ids[i])));
        }

        if (wildcard)
        {
            _ = builder.Append(written == 0 ? string.Empty : ", ").Append("any element the wildcard allows");
        }

        return builder.Append("} is expected.").ToString();
    }

    private bool TryGetGlobalElement(XPathQName name, out ElementDeclaration declaration)
    {
        if (
            _model.Schema.TryGet(XsdComponentKind.ElementDeclaration, name, out var component)
            && component is ElementDeclaration found
        )
        {
            declaration = found;
            return true;
        }

        declaration = null!;
        return false;
    }

    private void Invalid(ElementFrame frame, XsdRule rule, string message, string clause) =>
        Invalid(frame, frame.Location, rule, message, clause);

    private void Invalid(ElementFrame frame, DocumentLocation location, XsdRule rule, string message, string clause)
    {
        Report(rule, location, message, clause);
        frame.IsValid = false;
    }

    private void Report(XsdRule rule, DocumentLocation location, string message, string? clause) =>
        _reporter.Report(rule, location, message, clause);

    internal static string Display(XPathQName name) =>
        name.LocalName.Length == 0 ? "(anonymous)"
        : name.NamespaceName.Length == 0 ? "'" + name.LocalName + "'"
        : "'{" + name.NamespaceName + "}" + name.LocalName + "'";

    private static bool IsWhitespace(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!WhitespaceNormalizer.IsXmlSpace(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void AppendText(ReadOnlySpan<char> text)
    {
        if (_textLength + text.Length > _text.Length)
        {
            Array.Resize(ref _text, Math.Max(256, (_textLength + text.Length) * 2));
        }

        text.CopyTo(_text.AsSpan(_textLength));
        _textLength += text.Length;
    }
}
