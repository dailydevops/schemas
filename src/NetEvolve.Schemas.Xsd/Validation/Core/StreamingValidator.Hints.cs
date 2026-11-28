namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Validation.Entities;
using NetEvolve.Schemas.Xsd.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.SchemaLocation;
using NetEvolve.Schemas.Xsd.Validation.Xsi;

internal sealed partial class StreamingValidator
{
    private const string SchemaLocationAttribute = "schemaLocation";
    private const string NoNamespaceSchemaLocationAttribute = "noNamespaceSchemaLocation";

    // Reads the schema location hints of an element once its attributes are complete (Structures 2.6.3, 4.3.2): a hint extends the
    // model of this run, and an element that the model could not declare before is declared against the extended one. The element
    // is recorded as a use of its namespaces only afterwards, so a hint for the namespace of its own name is not late.
    private void ApplyHints(ElementFrame frame, SchemaLocationHints hints)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(hints);

        var model = _model;
        var index = FindXsiAttribute(SchemaLocationAttribute);
        if (index >= 0)
        {
            model = hints.ApplySchemaLocation(model, ValueOf(_attributes[index]), frame.Location);
        }

        index = FindXsiAttribute(NoNamespaceSchemaLocationAttribute);
        if (index >= 0)
        {
            model = hints.ApplyNoNamespaceSchemaLocation(model, ValueOf(_attributes[index]), frame.Location);
        }

        if (!ReferenceEquals(model, _model))
        {
            UseModel(model);
            AddHintListeners(hints);
            if (frame.Declaration is null && TryGetGlobalElement(frame.QName, out var declaration))
            {
                frame.UndeclaredRoot = false;
                Declare(frame, declaration);
            }
        }

        hints.NoteUse(frame.QName.NamespaceName);
        for (var i = 0; i < _attributeCount; i++)
        {
            var uri = _attributes[i].Name.NamespaceUri;
            if (uri.Value != 0)
            {
                var name = Names.GetString(uri);
                if (!string.Equals(name, XsiNamespace, StringComparison.Ordinal))
                {
                    hints.NoteUse(name);
                }
            }
        }
    }

    // The extended model keeps the ids and programs of the one before, so the frames that are open stay valid; only what depends on
    // the model is made again.
    private void UseModel(ValidationModel model)
    {
        _model = model;
        _binding = new NameBinding(model, Names);
        _xsi = new XsiTypeResolver(model.Schema, _scope, _derivation);
        Array.Resize(ref _countingPool, model.ProgramCount);
        if (_allPool is not null)
        {
            Array.Resize(ref _allPool, model.ProgramCount);
        }

        if (_all11Pool is not null)
        {
            Array.Resize(ref _all11Pool, model.ProgramCount);
        }
    }

    // The listeners of a run are chosen from the configured schema, so a schema that a hint loaded gets the ones it needs from here
    // on: one that starts late misses only what came before the hint, which no type of its schema could have been used by. The
    // entity listener is told the document type declaration again, and the id listener reads the notations of the extended schema.
    private void AddHintListeners(SchemaLocationHints hints)
    {
        var added = new List<ValidationListener>(3);
        IdListener? ids = null;
        var hasEntities = false;
        var hasConstraints = false;
        for (var i = 0; i < _listeners.Length; i++)
        {
            ids ??= _listeners[i] as IdListener;
            hasEntities |= _listeners[i] is EntityListener;
            hasConstraints |= _listeners[i] is IdentityConstraintListener;
        }

        if (ids is not null)
        {
            ids.UseSchema(_model.Schema);
        }
        else if (hints.NeedsIds)
        {
            added.Add(new IdListener(_assessor, _model.Schema));
        }

        if (!hasEntities && hints.NeedsEntities)
        {
            var entities = new EntityListener(_assessor, _readerLimits);
            added.Add(entities);
            Begin(entities);
            if (_doctype is { } doctype)
            {
                entities.ReceiveDoctype(
                    new XmlDoctypeInfo(
                        doctype.Name.AsSpan(),
                        doctype.PublicId.AsSpan(),
                        doctype.SystemId.AsSpan(),
                        doctype.InternalSubset.AsSpan()
                    )
                );
            }
        }

        if (!hasConstraints && hints.HasConstraints)
        {
            added.Add(new IdentityConstraintListener(_assessor, _model.Schema.Version, hints.ConstraintsOf));
        }

        if (added.Count == 0)
        {
            return;
        }

        var all = new List<ValidationListener>(_listeners.Length + added.Count);
        all.AddRange(_listeners);

        // The annotation listener reads the validity that the others settle, so it stays last.
        var at = all.FindIndex(static l => l is Annotations.AnnotationListener);
        all.InsertRange(at < 0 ? all.Count : at, added);
        for (var i = 0; i < added.Count; i++)
        {
            if (added[i] is not EntityListener)
            {
                Begin(added[i]);
            }
        }

        Distribute(all);
    }

    private void Begin(ValidationListener listener) =>
        listener.BeginRun(new ValidationRunContext(_limits, Names, _reporter));

    private void Distribute(List<ValidationListener> all)
    {
        _listeners = all.ToArray();
        _onStart = Interested(all, ListenerInterest.StartElement);
        _onAttribute = Interested(all, ListenerInterest.Attribute);
        _onXsiAttribute = Interested(all, ListenerInterest.XsiAttribute);
        _onValue = Interested(all, ListenerInterest.SimpleValue);
        _onEnd = Interested(all, ListenerInterest.EndElement);
    }

    private sealed class DoctypeCopy(string name, string publicId, string systemId, string internalSubset)
    {
        internal string Name { get; } = name;

        internal string PublicId { get; } = publicId;

        internal string SystemId { get; } = systemId;

        internal string InternalSubset { get; } = internalSubset;
    }
}
