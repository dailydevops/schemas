namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The state of one resolution. Phase one discovers the documents in a worklist, one entry per reference and without recursion,
/// so the nesting of includes costs no stack however deep a chain is (decision OQ-5, condition 2); phase two rebuilds the models
/// from the last discovered document to the first, so every document is rebuilt after the documents it references.
/// </summary>
internal sealed partial class ResolveRun : IProblemSink
{
    private readonly SchematronSchema _schema;
    private readonly IModuleLoader _loader;
    private readonly IProblemSink _problems;
    private readonly SchematronEdition _edition;
    private readonly bool _xsltBinding;
    private readonly List<InclusionModule> _modules = [];
    private readonly Dictionary<SchematronNode, InclusionModule> _children = [];
    private bool _failed;
    private bool _limitReported;

    internal ResolveRun(SchematronSchema schema, IModuleLoader loader, IProblemSink problems)
    {
        _schema = schema;
        _loader = loader;
        _problems = problems;
        _edition = schema.Edition;
        _xsltBinding = CoreParseRun.IsXsltBinding(schema.QueryBinding);
    }

    internal SchematronSchema? Run()
    {
        _modules.Add(new InclusionModule(_schema, _schema.Location.Source, -1));
        for (var i = 0; i < _modules.Count; i++)
        {
            Scan(i);
        }

        if (_failed)
        {
            return null;
        }

        for (var i = _modules.Count - 1; i >= 0; i--)
        {
            _modules[i].Resolved = Rebuild(_modules[i]);
        }

        return _failed ? null : (SchematronSchema?)_modules[0].Resolved;
    }

    /// <summary>Counts the problem and passes it on; the limit is reported once, because every reference behind it fails the same way.</summary>
    public void Add(in SchematronProblem problem)
    {
        _failed = true;
        if (problem.Code == CoreDiagnosticCodes.ExternalResourceLimitExceeded)
        {
            if (_limitReported)
            {
                return;
            }

            _limitReported = true;
        }

        _problems.Add(in problem);
    }

    private void Report(DiagnosticCode code, string message, DocumentLocation location)
    {
        var problem = new SchematronProblem(code, message, location);
        Add(in problem);
    }

    // Phase one.

    private void Scan(int index)
    {
        switch (_modules[index].Model)
        {
            case SchematronContainer container:
                foreach (var include in container.Includes)
                {
                    Load(index, include, include.Href, ModuleKind.SchematronInclude, FragmentTarget.Container);
                }

                foreach (var rules in container.Rules)
                {
                    ScanRules(index, rules.Rules);
                }

                foreach (var pattern in container.Patterns)
                {
                    ScanPattern(index, pattern);
                }

                break;
            case SchematronPatternBase pattern:
                ScanPattern(index, pattern);
                break;
            case SchematronRule rule:
                ScanRule(index, rule);
                break;
            default:
                break;
        }
    }

    private void ScanPattern(int index, SchematronPatternBase pattern)
    {
        foreach (var include in pattern.Includes)
        {
            Load(index, include, include.Href, ModuleKind.SchematronInclude, FragmentTarget.Pattern);
        }

        ScanRules(index, pattern.Rules);
    }

    private void ScanRules(int index, IReadOnlyList<SchematronRule> rules)
    {
        foreach (var rule in rules)
        {
            ScanRule(index, rule);
        }
    }

    private void ScanRule(int index, SchematronRule rule)
    {
        foreach (var item in rule.Items)
        {
            if (item is SchematronInclude include)
            {
                Load(index, include, include.Href, ModuleKind.SchematronInclude, FragmentTarget.Rule);
            }
            else if (item is SchematronExtends { Href: { } href } extends)
            {
                Load(index, extends, href, ModuleKind.SchematronExtends, FragmentTarget.Library);
            }
        }
    }

    private void Load(int parent, SchematronNode site, string href, ModuleKind kind, FragmentTarget target)
    {
        var at = site.Location;
        if (href.AsSpan().IndexOf('#') >= 0)
        {
            Report(
                SchematronDiagnosticCodes.Unsupported,
                "The reference '" + href + "' has a fragment identifier, which is not supported yet.",
                at
            );
            return;
        }

        var reference = new ModuleReference(kind, href, _modules[parent].SystemId, at);
        if (!_loader.TryLoad(in reference, default, out var loaded, out var problem))
        {
            Add(in problem);
            return;
        }

        if (IsOnPath(parent, loaded.SystemId))
        {
            Report(
                kind == ModuleKind.SchematronInclude
                    ? SchematronDiagnosticCodes.IncludeCycle
                    : SchematronDiagnosticCodes.AbstractTargetInvalid,
                "The reference '" + href + "' closes a cycle through the module '" + loaded.SystemId + "'.",
                at
            );
            return;
        }

        Accept(parent, site, loaded, target);
    }

    private void Accept(int parent, SchematronNode site, LoadedModule loaded, FragmentTarget target)
    {
        var at = site.Location;
        var status = SchematronCoreParser.TryParseFragment(
            loaded.Root.Node,
            loaded.SystemId,
            _edition,
            _xsltBinding,
            target,
            this,
            out var fragment,
            out var rootName
        );
        switch (status)
        {
            case FragmentStatus.Parsed:
                var module = new InclusionModule(fragment!, loaded.SystemId, parent);
                _modules.Add(module);
                _children.Add(site, module);
                break;
            case FragmentStatus.Unsupported:
                Report(
                    SchematronDiagnosticCodes.Unsupported,
                    "The root element '"
                        + rootName
                        + "' of the module '"
                        + loaded.SystemId
                        + "' is allowed here by the grammar, but an included document with this root is not supported yet.",
                    at
                );
                break;
            case FragmentStatus.WrongRoot:
                Report(
                    SchematronDiagnosticCodes.ModuleInvalid,
                    "The root element '"
                        + rootName
                        + "' of the module '"
                        + loaded.SystemId
                        + "' is not allowed at the place of the reference.",
                    at
                );
                break;
            default:
                break;
        }
    }

    private bool IsOnPath(int module, string systemId)
    {
        for (var i = module; i >= 0; i = _modules[i].Parent)
        {
            if (string.Equals(_modules[i].SystemId, systemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
