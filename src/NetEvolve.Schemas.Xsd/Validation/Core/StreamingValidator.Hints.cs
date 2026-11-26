namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Arguments;
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
        Array.Resize(ref _allPool, model.ProgramCount);
        if (_all11Pool is not null)
        {
            Array.Resize(ref _all11Pool, model.ProgramCount);
        }
    }
}
