namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

/// <summary>
/// The immutable data the streaming validator runs on: the schema set for the global declarations, one dense schema name id per
/// element name that a content model mentions, and one <see cref="ContentProgram"/> per complex type with element content.
/// Everything is computed when the model is built, so the model is safe to share between runs and threads (design #146 DP-4).
/// </summary>
/// <remarks>
/// Limitations: an element reference matches the declaration and every non-abstract member of its substitution group; a
/// <c>all</c> group is compiled with the XSD 1.0 matcher in 1.0 mode (members with <c>maxOccurs</c> above 1 and wildcards are
/// left out) and with the XSD 1.1 matcher in 1.1 mode. A circular model group
/// definition is a schema error that the resolver reports; here the repeated group is left out so that building terminates.
/// </remarks>
internal sealed class ValidationModel
{
    private readonly Dictionary<XPathQName, int> _ids;
    private readonly XPathQName[] _names;
    private readonly Dictionary<ComplexTypeDefinition, ContentProgram> _programs;

    private ValidationModel(
        XsdSchemaSet schema,
        Dictionary<XPathQName, int> ids,
        XPathQName[] names,
        Dictionary<ComplexTypeDefinition, ContentProgram> programs
    )
    {
        Schema = schema;
        _ids = ids;
        _names = names;
        _programs = programs;
    }

    /// <summary>Gets the schema set that holds the global declarations.</summary>
    public XsdSchemaSet Schema { get; }

    /// <summary>Gets the number of programs, which is the size of the pool arrays of a session.</summary>
    public int ProgramCount => _programs.Count;

    /// <summary>Builds the model.</summary>
    /// <param name="schema">The frozen schema set.</param>
    /// <param name="components">The registered components; every complex type reachable from them gets its program.</param>
    /// <param name="reporter">The reporter for content models that exceed an internal ceiling.</param>
    /// <param name="substitutions">The substitution groups: a position of a content model that holds the head of a group also matches the instance members of the group; <see langword="null"/> for none.</param>
    /// <returns>The model.</returns>
    internal static ValidationModel Build(
        XsdSchemaSet schema,
        IReadOnlyList<IXsdComponent> components,
        XsdIssueReporter reporter,
        SubstitutionGroups? substitutions = null
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(reporter);

        var builder = new Builder(reporter)
        {
            Substitutions = substitutions,
            Schema = schema,
            AllGroups11 = schema.Version == XsdVersion.V11,
        };
        for (var i = 0; i < components.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(components[i]);

            builder.Push(components[i]);
        }

        builder.Run();
        return new ValidationModel(schema, builder.Ids, builder.Names.ToArray(), builder.Programs);
    }

    /// <summary>
    /// Builds the model of an overlay: this model plus the components of a schema that was compiled on its own. The ids and the programs
    /// of this model are kept, the new names and programs follow them, so a run that continues on the result still matches the content
    /// models that it already started; this model is not changed (design #146 DP-22).
    /// </summary>
    /// <param name="overlay">The schema set of the overlay, chained to the set of this model.</param>
    /// <param name="components">The registered components of the overlay.</param>
    /// <param name="reporter">The reporter for content models that exceed an internal ceiling.</param>
    /// <returns>The model.</returns>
    internal ValidationModel Extend(
        XsdSchemaSet overlay,
        IReadOnlyList<IXsdComponent> components,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(reporter);

        var builder = new Builder(reporter, _ids, _names, _programs)
        {
            Schema = overlay,
            AllGroups11 = overlay.Version == XsdVersion.V11,
        };
        for (var i = 0; i < components.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(components[i]);

            builder.Push(components[i]);
        }

        builder.Run();
        return new ValidationModel(overlay, builder.Ids, builder.Names.ToArray(), builder.Programs);
    }

    /// <summary>Gets the program of a complex type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The program, or <see langword="null"/> when the type has no element-only or mixed content.</returns>
    public ContentProgram? GetProgram(ComplexTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _programs.TryGetValue(type, out var program) ? program : null;
    }

    /// <summary>Gets the dense id of an element name that a content model mentions.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="id">The id.</param>
    /// <returns><see langword="true"/> when a content model mentions the name.</returns>
    public bool TryGetNameId(XPathQName name, out int id) => _ids.TryGetValue(name, out id);

    /// <summary>Gets the name of an id.</summary>
    /// <param name="id">An id that <see cref="TryGetNameId"/> returned.</param>
    /// <returns>The expanded name.</returns>
    public XPathQName NameOf(int id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, _names.Length);

        return _names[id];
    }

    private sealed class Builder
    {
        private readonly XsdIssueReporter _reporter;
        private readonly Stack<IXsdComponent> _work = new();
        private readonly HashSet<IXsdComponent> _seen = [];
        private readonly HashSet<ModelGroup> _active = [];
        private IDeclaredNames? _siblings;
        private Particle? _content;

        internal Builder(XsdIssueReporter reporter)
        {
            ArgumentNullException.ThrowIfNull(reporter);

            _reporter = reporter;
            Ids = [];
            Names = [];
            Programs = [];
        }

        // Starts from the ids and programs of a model, which keeps them: the new ones are numbered after them.
        internal Builder(
            XsdIssueReporter reporter,
            Dictionary<XPathQName, int> ids,
            XPathQName[] names,
            Dictionary<ComplexTypeDefinition, ContentProgram> programs
        )
        {
            ArgumentNullException.ThrowIfNull(reporter);
            ArgumentNullException.ThrowIfNull(ids);
            ArgumentNullException.ThrowIfNull(names);
            ArgumentNullException.ThrowIfNull(programs);

            _reporter = reporter;
            Ids = new Dictionary<XPathQName, int>(ids);
            Names = new List<XPathQName>(names);
            Programs = new Dictionary<ComplexTypeDefinition, ContentProgram>(programs);
        }

        internal SubstitutionGroups? Substitutions { get; set; }

        internal XsdSchemaSet? Schema { get; set; }

        internal bool AllGroups11 { get; set; }

        internal Dictionary<XPathQName, int> Ids { get; }

        internal List<XPathQName> Names { get; }

        internal Dictionary<ComplexTypeDefinition, ContentProgram> Programs { get; }

        internal void Push(IXsdComponent? component)
        {
            if (component is not null && _seen.Add(component))
            {
                _work.Push(component);
            }
        }

        internal void Run()
        {
            while (_work.Count > 0)
            {
                switch (_work.Pop())
                {
                    case ElementDeclaration element:
                        Push(element.TypeDefinition ?? ComplexTypeDefinition.AnyType);
                        break;
                    case ComplexTypeDefinition type:
                        Compile(type);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Compile(ComplexTypeDefinition type)
        {
            var content = type.ContentParticle;
            if (Programs.ContainsKey(type))
            {
                return;
            }

            if (content is null || type.ContentKind is not (ComplexContentKind.ElementOnly or ComplexContentKind.Mixed))
            {
                return;
            }

            var terms = new List<IXsdComponent>();
            var poolIndex = Programs.Count;
            _content = content;
            _siblings = null;
            if (
                content.MaxOccurs > 0
                && Unwrap(content.Term) is ModelGroup { Compositor: ModelGroupCompositor.All } all
            )
            {
                Programs.Add(
                    type,
                    AllGroups11 ? CompileAll11(content, terms, poolIndex) : CompileAll(content, all, terms, poolIndex)
                );
                return;
            }

            var root = Convert(content, terms) ?? AutomatonParticle.Sequence(1, 1);
            var automaton = CountingAutomatonCompiler.Compile(root, _reporter, content.Location);
            Programs.Add(type, new ContentProgram(automaton, null, terms, poolIndex));
        }

        private ContentProgram CompileAll11(Particle content, List<IXsdComponent> terms, int poolIndex)
        {
            var group = AllGroup11Compiler.Compile(content, IdOf, terms, IdsOf, Bind);
            if (group is null)
            {
                _reporter.Report(
                    XsdRuleCodes.EngineRules[0],
                    content.Location,
                    "The all group nests deeper than " + AllGroup11Compiler.MaxNesting + " levels."
                );
            }

            for (var i = 0; i < terms.Count; i++)
            {
                if (terms[i] is ElementDeclaration element)
                {
                    Push(element);
                }
            }

            return new ContentProgram(null, null, terms, poolIndex, group);
        }

        private ContentProgram CompileAll(Particle content, ModelGroup group, List<IXsdComponent> terms, int poolIndex)
        {
            var nameIds = new List<int[]>();
            var optional = new List<bool>();
            foreach (var member in group.Particles)
            {
                if (member.MaxOccurs == 0 || Unwrap(member.Term) is not ElementDeclaration element)
                {
                    continue;
                }

                Push(element);
                terms.Add(element);
                nameIds.Add(IdsOf(element));
                optional.Add(member.MinOccurs == 0);
            }

            return new ContentProgram(
                null,
                new AllGroup(content.MinOccurs == 0, nameIds.ToArray(), optional.ToArray()),
                terms,
                poolIndex
            );
        }

        // Leaves are numbered in document order and a particle that cannot occur is left out, which is exactly the numbering of
        // the positions of the automaton compiler; terms.Count at a leaf is therefore its position.
        private AutomatonParticle? Convert(Particle particle, List<IXsdComponent> terms)
        {
            if (particle.MaxOccurs == 0 || particle.MinOccurs > particle.MaxOccurs)
            {
                return null;
            }

            switch (Unwrap(particle.Term))
            {
                case ElementDeclaration element:
                    Push(element);
                    terms.Add(element);
                    return AutomatonParticle.Element(particle.MinOccurs, particle.MaxOccurs, IdsOf(element));
                case IWildcard unbound:
                    var wildcard = Bind(unbound);
                    terms.Add(wildcard);
                    return AutomatonParticle.Wildcard(particle.MinOccurs, particle.MaxOccurs, wildcard);
                case ModelGroup group when _active.Add(group):
                    var children = new List<AutomatonParticle>();
                    foreach (var child in group.Particles)
                    {
                        var converted = Convert(child, terms);
                        if (converted is not null)
                        {
                            children.Add(converted);
                        }
                    }

                    _ = _active.Remove(group);
                    return group.Compositor == ModelGroupCompositor.Choice
                        ? AutomatonParticle.Choice(particle.MinOccurs, particle.MaxOccurs, children.ToArray())
                        : AutomatonParticle.Sequence(particle.MinOccurs, particle.MaxOccurs, children.ToArray());
                default:
                    return null;
            }
        }

        // ##defined and ##definedSibling need the declarations of the schema and the elements of the content model (Structures 3.10.4.1),
        // which the wildcard does not know when it is read; a wildcard of XSD 1.1 that uses them is bound here, per complex type.
        private IWildcard Bind(IWildcard wildcard)
        {
            if (
                wildcard
                is not NegatedWildcard { Constraint: { DisallowsDefined: true } or { DisallowsSibling: true } } negated
            )
            {
                return wildcard;
            }

            _siblings ??= DeclaredNames.SiblingsOf(_content, Substitutions);
            return negated.WithContext(new SchemaElementNames(Schema!), _siblings);
        }

        private static IXsdComponent? Unwrap(IXsdComponent? term) =>
            term is ModelGroupDefinition definition ? definition.ModelGroup : term;

        // The name of the declaration first, then the names of the other instance members of the substitution group it heads.
        private int[] IdsOf(ElementDeclaration element)
        {
            if (Substitutions is null || !element.IsGlobal)
            {
                return [IdOf(element.Name)];
            }

            var ids = new List<int> { IdOf(element.Name) };
            var instances = Substitutions.GetInstanceElements(element);
            for (var i = 0; i < instances.Count; i++)
            {
                if (!ReferenceEquals(instances[i], element))
                {
                    ids.Add(IdOf(instances[i].Name));
                }
            }

            return ids.ToArray();
        }

        private int IdOf(XPathQName name)
        {
            if (!Ids.TryGetValue(name, out var id))
            {
                id = Names.Count;
                Ids.Add(name, id);
                Names.Add(name);
            }

            return id;
        }
    }

    private sealed class SchemaElementNames(XsdSchemaSet schema) : IDeclaredNames
    {
        public bool Contains(XPathQName name) => schema.TryGet(XsdComponentKind.ElementDeclaration, name, out _);
    }
}
