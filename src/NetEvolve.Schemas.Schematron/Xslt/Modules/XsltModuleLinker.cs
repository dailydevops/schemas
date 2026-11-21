namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The import-tree linker (XSLT 2.0 section 3.10, design #150 EP-14): it loads the modules that <c>xsl:include</c> and
/// <c>xsl:import</c> name, through the shared <see cref="IModuleLoader"/> and so through the core resolver, and gives every
/// declaration the import precedence of its level. A level is a module with everything it includes; an include is spliced at its
/// position and shares the rank, an import opens a child level, and the rank counts the completed levels of a depth-first walk in
/// document order, so a child ranks below the level that imports it and the principal module ranks highest.
/// </summary>
/// <remarks>
/// <para>
/// One instance per link, not thread-safe. The walk keeps an explicit stack and never recurses, so the depth of a chain of modules
/// is bounded by <c>MaxExternalResources</c> and not by the stack. A module is identified by the system identifier the resolver
/// returns, and also by the resolved URI of the reference (the core <c>UriReference</c>, the helper of the loader): a module that
/// is reached again is neither fetched nor parsed nor counted again and gets one more placement over the same tree. A module that
/// includes or imports itself, directly or through others, is the error XTSE0180 or XTSE0210 at the reference that closes the
/// cycle. The <c>xsl:include</c> and <c>xsl:import</c> elements are consumed here and never reach the declarations.
/// </para>
/// <para>
/// The top-level children of a module root are the declarations: XSLT elements are kept, an element of another namespace is a
/// user-defined data element and is ignored, an element without a namespace (XTSE0130) and character data (XTSE0120) are errors, and
/// an <c>xsl:import</c> after any other element is XTSE0200 (an include counts as another element). A principal module is given as
/// the list of the top-level elements of the schema, so its Schematron elements neither precede nor follow an import.
/// </para>
/// <para>
/// Limitations: the sum of the sizes of all modules of a build is not bounded by <c>MaxInputSize</c> (the loaded module carries no
/// size; each module is bounded by <c>MaxResourceSize</c>, the number of modules by <c>MaxExternalResources</c>); <c>xml:base</c>
/// is not applied; the version attribute of the module root is not checked.
/// </para>
/// </remarks>
internal sealed class XsltModuleLinker
{
    private const string IncludeCycleConstraint = "XTSE0180";
    private const string ImportCycleConstraint = "XTSE0210";
    private const string ImportPlacementConstraint = "XTSE0200";
    private const string NoNamespaceConstraint = "XTSE0130";
    private const string TextConstraint = "XTSE0120";
    private const string StructureConstraint = "XTSE0010";

    private readonly IModuleLoader _loader;
    private readonly IProblemSink _problems;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, Module> _byUri = new(StringComparer.Ordinal);
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);
    private readonly List<Placement> _placements = [];
    private bool _failed;
    private int _rank;

    /// <summary>Creates a linker.</summary>
    /// <param name="loader">The loader of the build; its counts are shared with the Schematron <c>include</c>.</param>
    /// <param name="problems">The sink of the problems.</param>
    /// <param name="cancellationToken">The token that the loads observe.</param>
    /// <exception cref="ArgumentNullException"><paramref name="loader"/> or <paramref name="problems"/> is <see langword="null"/>.</exception>
    internal XsltModuleLinker(IModuleLoader loader, IProblemSink problems, CancellationToken cancellationToken)
    {
        Argument.ThrowIfNull(loader);
        Argument.ThrowIfNull(problems);

        _loader = loader;
        _problems = problems;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Links the principal module with the modules it includes and imports.</summary>
    /// <param name="principal">The top-level elements of the principal module, in document order.</param>
    /// <param name="principalSystemId">The system identifier of the principal module, or <see langword="null"/>.</param>
    /// <param name="set">The declarations with their precedence, also when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    internal bool TryLink(IReadOnlyList<XPathNode> principal, string? principalSystemId, out XsltModuleSet set)
    {
        Argument.ThrowIfNull(principal);

        var items = new List<XPathNode>();
        for (var i = 0; i < principal.Count; i++)
        {
            Argument.ThrowIfNull(principal[i].Tree);
            Collect(principal[i], principalSystemId, items);
        }

        var root = new Module(principalSystemId, Seal(items, principalSystemId));
        if (principalSystemId is not null)
        {
            _byUri[principalSystemId] = root;
        }

        var stack = new List<Frame>();
        Push(stack, root, new Level(), true);
        while (stack.Count > 0)
        {
            var frame = stack[stack.Count - 1];
            if (frame.Next == frame.Module.Items.Length)
            {
                stack.RemoveAt(stack.Count - 1);
                Pop(frame);
                continue;
            }

            var element = frame.Module.Items[frame.Next++];
            var isImport = XsltElementReader.IsXslt(element.Node, "import");
            if (isImport || XsltElementReader.IsXslt(element.Node, "include"))
            {
                Enter(element, isImport, frame, stack);
            }
            else
            {
                _placements.Add(new Placement(element, frame.Module.SystemId, frame.Level));
            }
        }

        var declarations = new XsltModuleDeclaration[_placements.Count];
        for (var i = 0; i < declarations.Length; i++)
        {
            var placement = _placements[i];
            declarations[i] = new XsltModuleDeclaration(placement.Element, placement.SystemId, placement.Level.Rank);
        }

        set = new XsltModuleSet(declarations);
        return !_failed;
    }

    private void Push(List<Frame> stack, Module module, Level level, bool opensLevel)
    {
        if (module.SystemId is not null)
        {
            _ = _active.Add(module.SystemId);
        }

        stack.Add(new Frame(module, level, opensLevel));
    }

    // The rank of a level is the number of levels that completed before it, plus one.
    private void Pop(Frame frame)
    {
        if (frame.Module.SystemId is not null)
        {
            _ = _active.Remove(frame.Module.SystemId);
        }

        if (frame.OpensLevel)
        {
            frame.Level.Rank = ++_rank;
        }
    }

    private void Enter(XPathNode element, bool isImport, Frame parent, List<Frame> stack)
    {
        var systemId = parent.Module.SystemId;
        var kind = isImport ? "xsl:import" : "xsl:include";
        if (!XsltElementReader.TryGetAttribute(element, "href", out var href))
        {
            Report(
                SchematronDiagnosticCodes.AttributeMissing,
                $"The {kind} element has no href attribute.",
                element,
                systemId,
                StructureConstraint
            );
            return;
        }

        var reference = new ModuleReference(
            isImport ? ModuleKind.XsltImport : ModuleKind.XsltInclude,
            href,
            systemId,
            XsltElementResolver.LocationOf(element, systemId)
        );
        if (!TryResolve(in reference, out var module))
        {
            return;
        }

        if (module.SystemId is not null && _active.Contains(module.SystemId))
        {
            Report(
                SchematronDiagnosticCodes.IncludeCycle,
                $"The module '{module.SystemId}' {(isImport ? "imports" : "includes")} itself, directly or through other modules.",
                element,
                systemId,
                isImport ? ImportCycleConstraint : IncludeCycleConstraint
            );
            return;
        }

        Push(stack, module, isImport ? new Level() : parent.Level, isImport);
    }

    // Finds the module by the resolved URI before asking the loader, so that a module that is reached again costs no load, and by the
    // system identifier after it, because a resolver may return one identifier for several spellings of a reference.
    private bool TryResolve(in ModuleReference reference, out Module module)
    {
        var key = UriReference.Resolve(reference.Href, reference.BaseUri).Uri;
        if (key is not null && _byUri.TryGetValue(key, out module!))
        {
            return true;
        }

        if (!_loader.TryLoad(in reference, _cancellationToken, out var loaded, out var problem))
        {
            _failed = true;
            _problems.Add(in problem);
            module = null!;
            return false;
        }

        if (!_byUri.TryGetValue(loaded.SystemId, out module!))
        {
            if (!TryRead(in loaded, in reference, out module))
            {
                return false;
            }

            _byUri.Add(loaded.SystemId, module);
        }

        if (key is not null)
        {
            _byUri[key] = module;
        }

        return true;
    }

    private bool TryRead(in LoadedModule loaded, in ModuleReference reference, out Module module)
    {
        var root = loaded.Root.Node.FirstChild;
        while (!root.IsEmpty && root.Kind != XmlNodeKind.Element)
        {
            root = root.NextSibling;
        }

        if (
            root.IsEmpty
            || !(XsltElementReader.IsXslt(root, "stylesheet") || XsltElementReader.IsXslt(root, "transform"))
        )
        {
            _failed = true;
            var problem = new SchematronProblem(
                SchematronDiagnosticCodes.ModuleInvalid,
                $"The module '{loaded.SystemId}' is not a stylesheet module: its root element is neither xsl:stylesheet nor xsl:transform.",
                reference.Location
            );
            _problems.Add(in problem);
            module = null!;
            return false;
        }

        var items = new List<XPathNode>();
        for (var child = root.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Element)
            {
                Collect(new XPathNode(child), loaded.SystemId, items);
            }
            else if (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue))
            {
                Report(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "The root element of a stylesheet module must not have character data as a child.",
                    new XPathNode(root),
                    loaded.SystemId,
                    TextConstraint
                );
            }
        }

        module = new Module(loaded.SystemId, Seal(items, loaded.SystemId));
        return true;
    }

    // An XSLT element is a declaration or a reference, an element of another namespace is user-defined data, and one without a namespace is an error.
    private void Collect(XPathNode element, string? systemId, List<XPathNode> items)
    {
        Argument.ThrowIfNull(element.Tree);

        var name = element.Node.Name;
        if (name.NamespaceUri.IsEmpty)
        {
            Report(
                SchematronDiagnosticCodes.ElementMisplaced,
                "A top-level element of a stylesheet module must be in a namespace.",
                element,
                systemId,
                NoNamespaceConstraint
            );
        }
        else if (
            string.Equals(
                element.Tree.Names.GetString(name.NamespaceUri),
                XsltInstructionRegistry.Namespace,
                StringComparison.Ordinal
            )
        )
        {
            items.Add(element);
        }
    }

    // Every xsl:import precedes all other top-level elements of its module; an xsl:include counts as another element.
    private XPathNode[] Seal(List<XPathNode> items, string? systemId)
    {
        var seenOther = false;
        foreach (var item in items)
        {
            if (!XsltElementReader.IsXslt(item.Node, "import"))
            {
                seenOther = true;
            }
            else if (seenOther)
            {
                Report(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "The xsl:import element must precede all other top-level elements of its module.",
                    item,
                    systemId,
                    ImportPlacementConstraint
                );
            }
        }

        return items.ToArray();
    }

    private void Report(DiagnosticCode code, string message, XPathNode element, string? systemId, string constraint)
    {
        _failed = true;
        var problem = new SchematronProblem(
            code,
            message,
            XsltElementResolver.LocationOf(element, systemId),
            constraint
        );
        _problems.Add(in problem);
    }

    private sealed class Module
    {
        internal Module(string? systemId, XPathNode[] items)
        {
            SystemId = systemId;
            Items = items;
        }

        internal string? SystemId { get; }

        internal XPathNode[] Items { get; }
    }

    private sealed class Level
    {
        internal int Rank { get; set; }
    }

    private sealed class Frame
    {
        internal Frame(Module module, Level level, bool opensLevel)
        {
            Module = module;
            Level = level;
            OpensLevel = opensLevel;
        }

        internal Module Module { get; }

        internal Level Level { get; }

        internal bool OpensLevel { get; }

        internal int Next { get; set; }
    }

    private readonly struct Placement
    {
        internal Placement(XPathNode element, string? systemId, Level level)
        {
            Element = element;
            SystemId = systemId;
            Level = level;
        }

        internal XPathNode Element { get; }

        internal string? SystemId { get; }

        internal Level Level { get; }
    }
}
