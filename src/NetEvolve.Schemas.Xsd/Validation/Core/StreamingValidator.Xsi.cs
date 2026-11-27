namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Validation.Xsi;

internal sealed partial class StreamingValidator
{
    private const string TypeAttribute = "type";
    private const string NilAttribute = "nil";

    // Decides the type and the nil state of the element from its xsi attributes (Structures 3.3.4, Element Locally Valid
    // (Element), clauses 3 and 4), then sets up its content. Runs at the end of the attributes, before any child or text event.
    private void BeginContent(ElementFrame frame)
    {
        var declaration = frame.Declaration;
        var declared = declaration is null ? null : declaration.TypeDefinition ?? ComplexTypeDefinition.AnyType;
        if (declaration?.TypeTable is { } table)
        {
            // The type table selects the type that xsi:type must be derived from (Structures 1.1, 3.3.4).
            declared = SelectAlternative(frame, table, declared!);
        }

        var type = declared;
        var typeIndex = FindXsiAttribute(TypeAttribute);
        if (typeIndex >= 0)
        {
            var value = ValueOf(_attributes[typeIndex]);
            var result = _xsi.Resolve(value, declared, declaration?.Block ?? XsdDerivationMethod.None);
            if (result.Status == XsiTypeStatus.Resolved)
            {
                type = result.Type;
            }
            else
            {
                ReportXsiType(frame, value, result, declared);
            }
        }

        if (declaration is not null)
        {
            ApplyNil(frame, declaration);
        }

        if (type is null)
        {
            if (frame.UndeclaredStrict)
            {
                // The parent is the frame below this one; its wildcard matched the element.
                var parent = _frames[_depth - 2];
                parent.IsValid = false;
                frame.IsValid = false;
                frame.ContentFailed = true;
                Report(
                    ValidationRules.Wildcard,
                    frame.Location,
                    "The wildcard of element "
                        + Display(parent.QName)
                        + " is strict, but no declaration was found for the element "
                        + Display(frame.QName)
                        + ".",
                    "cvc-wildcard.2"
                );
                return;
            }

            if (frame.UndeclaredRoot)
            {
                frame.IsValid = false;
                Report(
                    ValidationRules.AssessElt,
                    frame.Location,
                    "No declaration was found for the root element " + Display(frame.QName) + ".",
                    "cvc-assess-elt.1"
                );
            }

            return;
        }

        if (type is ComplexTypeDefinition { IsAbstract: true } abstractType)
        {
            Invalid(
                frame,
                ValidationRules.Type,
                "The type " + Display(abstractType.Name) + " of element " + Display(frame.QName) + " is abstract.",
                "cvc-type.2"
            );
        }

        OpenType(frame, type);
    }

    private void ReportXsiType(
        ElementFrame frame,
        ReadOnlySpan<char> value,
        XsiTypeResult result,
        IXsdComponent? declared
    )
    {
        var element = Display(frame.QName);
        switch (result.Status)
        {
            case XsiTypeStatus.InvalidQName:
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The value '"
                        + Shorten(value)
                        + "' of attribute xsi:type of element "
                        + element
                        + " is not a QName.",
                    "cvc-elt.4.1"
                );
                break;
            case XsiTypeStatus.UnboundPrefix:
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The value '"
                        + Shorten(value)
                        + "' of attribute xsi:type of element "
                        + element
                        + " has a prefix that is not bound to a namespace.",
                    "cvc-elt.4.1"
                );
                break;
            case XsiTypeStatus.UnknownType:
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The type "
                        + Display(result.Name)
                        + " of attribute xsi:type of element "
                        + element
                        + " is unknown.",
                    "cvc-elt.4.2"
                );
                break;
            default:
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The type "
                        + Display(result.Name)
                        + " of attribute xsi:type of element "
                        + element
                        + " is not validly derived from the type "
                        + Display(declared!.Name)
                        + " of its declaration ("
                        + result.ViolatedRule!.Id
                        + ").",
                    "cvc-elt.4.3"
                );
                break;
        }
    }

    private void ReportNilledText(ElementFrame frame)
    {
        // The content of a nilled element is reported once, like the content of an element of empty content type.
        if (!frame.ContentFailed)
        {
            frame.ContentFailed = true;
            Invalid(
                frame,
                _textStart,
                ValidationRules.Elt,
                "The element " + Display(frame.QName) + " has xsi:nil true, so it must not have character data.",
                "cvc-elt.3.2.1"
            );
        }
    }

    // xsi:nil is a property of a declaration: without one the attribute is not checked and the element is not nilled.
    private void ApplyNil(ElementFrame frame, ElementDeclaration declaration)
    {
        var index = FindXsiAttribute(NilAttribute);
        if (index < 0)
        {
            return;
        }

        var value = ValueOf(_attributes[index]);
        if (!declaration.IsNillable)
        {
            Invalid(
                frame,
                ValidationRules.Elt,
                "The element " + Display(frame.QName) + " is not nillable, so it must not have the attribute xsi:nil.",
                "cvc-elt.3.1"
            );
        }
        else if (!_nil.TryParse(value, out var nilled))
        {
            Report(
                ValidationRules.ForFacet(FacetKind.None),
                frame.Location,
                "The value '"
                    + Shorten(value)
                    + "' of attribute xsi:nil of element "
                    + Display(frame.QName)
                    + " is not a boolean.",
                null
            );
            frame.IsValid = false;
        }
        else if (nilled)
        {
            frame.IsNilled = true;
            if (declaration.ValueConstraint.Kind == ElementValueConstraintKind.Fixed)
            {
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The element " + Display(frame.QName) + " has a fixed value, so it must not have xsi:nil true.",
                    "cvc-elt.3.2.2"
                );
            }
        }
    }

    private int FindXsiAttribute(string local)
    {
        for (var i = 0; i < _attributeCount; i++)
        {
            var name = _attributes[i].Name;
            if (
                name.NamespaceUri.Value != 0
                && string.Equals(Names.GetString(name.NamespaceUri), XsiNamespace, StringComparison.Ordinal)
                && string.Equals(Names.GetString(name.Local), local, StringComparison.Ordinal)
            )
            {
                return i;
            }
        }

        return -1;
    }
}
