namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

// Type alternatives (Structures 1.1, 3.12.4) and the inheritable attributes (3.2.2): the type table of an element declaration chooses
// the type from the attributes of the element and the inheritable attributes of its ancestors, before xsi:type is applied.
internal sealed partial class StreamingValidator
{
    // Opens the inheritance frame of the element. Which of its attributes are inheritable is known only after its type is chosen, so
    // they are added by PublishInheritable; the type table of this element sees what its ancestors passed down.
    private void EnterInherited(ElementFrame frame)
    {
        if (_taAttributes.Length < _attributeCount)
        {
            _taAttributes = new TypeAlternativeAttribute[Math.Max(_attributeCount, _taAttributes.Length * 2)];
        }

        for (var i = 0; i < _attributeCount; i++)
        {
            var entry = _attributes[i];
            _ = IsXsiControl(entry.Name, out var qname);
            _taAttributes[i] = new TypeAlternativeAttribute(qname, entry.Start, entry.Length, false);
        }

        _inherited!.Enter(_taAttributes.AsSpan(0, _attributeCount), _attributeChars.AsSpan(0, _attributeLength));
        frame.Inherits = true;
    }

    private IXsdComponent SelectAlternative(ElementFrame frame, TypeTable table, IXsdComponent declared)
    {
        _alternativeEvaluator ??= new TypeAlternativeEvaluator();
        var selection = _alternativeEvaluator.Select(
            table,
            declared,
            frame.QName,
            _taAttributes.AsSpan(0, _attributeCount),
            _attributeChars.AsSpan(0, _attributeLength),
            _inherited!
        );
        if (!selection.Validate(frame.QName, frame.Location, _reporter))
        {
            frame.IsValid = false;
        }

        return selection.Type;
    }

    // The attributes whose declaration is inheritable are visible to the type tables of the descendants.
    private void PublishInheritable(ElementFrame frame)
    {
        for (var i = 0; i < _attributeCount; i++)
        {
            var attribute = _taAttributes[i];
            if (!IsXsiControl(_attributes[i].Name, out _) && IsInheritable(frame, attribute.Name))
            {
                _taAttributes[i] = new TypeAlternativeAttribute(
                    attribute.Name,
                    attribute.Start,
                    attribute.Length,
                    true
                );
            }
        }

        _inherited!.AddInheritable(
            _taAttributes.AsSpan(0, _attributeCount),
            _attributeChars.AsSpan(0, _attributeLength)
        );
    }

    private bool IsInheritable(ElementFrame frame, XPathQName name)
    {
        if (frame.Content == FrameContent.SimpleType)
        {
            return false;
        }

        if (frame.Complex is { } complex)
        {
            var index = FindUse(complex.AttributeUses, name);
            if (index >= 0)
            {
                return complex.AttributeUses[index].Declaration.IsInheritable;
            }

            // An attribute that a wildcard assesses has the global declaration of its name.
            if (
                complex.AttributeWildcard is not { } wildcard
                || !wildcard.Allows(name)
                || wildcard.ProcessContents == XsdProcessContents.Skip
            )
            {
                return false;
            }
        }

        return _model.Schema.TryGet(XsdComponentKind.AttributeDeclaration, name, out var component)
            && component is AttributeDeclaration { IsInheritable: true };
    }
}
