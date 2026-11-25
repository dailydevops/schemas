namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;

internal sealed partial class StreamingValidator
{
    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        if (_skip > 0)
        {
            return;
        }

        if (_attributeCount == _attributes.Length)
        {
            Array.Resize(ref _attributes, _attributeCount * 2);
        }

        if (_attributeLength + value.Length > _attributeChars.Length)
        {
            Array.Resize(ref _attributeChars, Math.Max(256, (_attributeLength + value.Length) * 2));
        }

        value.CopyTo(_attributeChars.AsSpan(_attributeLength));
        _attributes[_attributeCount++] = new AttributeEntry(name, _attributeLength, value.Length);
        _attributeLength += value.Length;
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        if (_skip > 0)
        {
            return;
        }

        var frame = _frames[_depth - 1];
        if (_hints is not null)
        {
            ApplyHints(frame, _hints);
        }

        BeginContent(frame);
        if (_onStart.Length > 0)
        {
            var info = new ElementStartInfo(
                frame.Ordinal,
                frame.Name,
                frame.Location,
                frame.Declaration,
                frame.Type,
                _depth
            );
            for (var i = 0; i < _onStart.Length; i++)
            {
                _onStart[i].StartElement(info);
            }
        }

        switch (frame.Content)
        {
            case FrameContent.Unassessed:
                AssessLaxAttributes(frame);
                break;
            case FrameContent.SimpleType:
                AssessSimpleTypeAttributes(frame);
                break;
            default:
                AssessAttributes(frame);
                break;
        }

        FireXsiAttributes(frame);

        // The attributes of a nilled element are assessed against its type; from here on only its absent content counts.
        if (frame.IsNilled)
        {
            frame.Content = FrameContent.Nilled;
        }
    }

    // The xsi attributes are attributes of the element too (an @* identity field selects them), but they are not assessed as such.
    private void FireXsiAttributes(ElementFrame frame)
    {
        if (_onXsiAttribute.Length == 0)
        {
            return;
        }

        for (var i = 0; i < _attributeCount; i++)
        {
            var entry = _attributes[i];
            if (!IsXsiControl(entry.Name, out _))
            {
                continue;
            }

            var info = new AttributeInfo(
                frame.Ordinal,
                entry.Name,
                frame.Location,
                null,
                ValueOf(entry),
                null,
                true,
                false
            );
            for (var l = 0; l < _onXsiAttribute.Length; l++)
            {
                _onXsiAttribute[l].Attribute(info);
            }
        }
    }

    // An element that is not assessed (no declaration) assesses an attribute only when a global declaration names it.
    private void AssessLaxAttributes(ElementFrame frame)
    {
        for (var i = 0; i < _attributeCount; i++)
        {
            var entry = _attributes[i];
            if (IsXsiControl(entry.Name, out var qname))
            {
                continue;
            }

            AssessByDeclaration(frame, entry, qname, false);
        }
    }

    // A simple type has no attributes, except those of the schema instance namespace.
    private void AssessSimpleTypeAttributes(ElementFrame frame)
    {
        for (var i = 0; i < _attributeCount; i++)
        {
            var entry = _attributes[i];
            if (IsXsiControl(entry.Name, out var qname))
            {
                continue;
            }

            Invalid(
                frame,
                ValidationRules.Type,
                "The type of element "
                    + Display(frame.QName)
                    + " is a simple type, so it must not have the attribute "
                    + Display(qname)
                    + ".",
                "cvc-type.3.1.1"
            );
            FireAttribute(frame, entry, null, null, false, false);
        }
    }

    private void AssessAttributes(ElementFrame frame)
    {
        var complex = frame.Complex!;
        var uses = complex.AttributeUses;
        if (_seen.Length < uses.Count)
        {
            _seen = new bool[Math.Max(uses.Count, _seen.Length * 2)];
        }

        Array.Clear(_seen, 0, uses.Count);
        for (var i = 0; i < _attributeCount; i++)
        {
            var entry = _attributes[i];
            if (IsXsiControl(entry.Name, out var qname))
            {
                continue;
            }

            var index = FindUse(uses, qname);
            if (index >= 0)
            {
                _seen[index] = true;
                AssessDeclared(frame, entry, qname, uses[index].Declaration, uses[index].ValueConstraint);
            }
            else
            {
                AssessByWildcard(frame, entry, qname, complex.AttributeWildcard);
            }
        }

        for (var u = 0; u < uses.Count; u++)
        {
            if (_seen[u])
            {
                continue;
            }

            if (uses[u].Required)
            {
                Invalid(
                    frame,
                    ValidationRules.ComplexType,
                    "The required attribute "
                        + Display(uses[u].Name)
                        + " is missing on element "
                        + Display(frame.QName)
                        + ".",
                    "cvc-complex-type.4"
                );
            }
            else if (uses[u].ValueConstraint.Kind != AttributeValueConstraintKind.None)
            {
                ApplyDefault(frame, uses[u]);
            }
        }
    }

    private void AssessByWildcard(ElementFrame frame, AttributeEntry entry, XPathQName qname, IWildcard? wildcard)
    {
        if (wildcard is null)
        {
            Invalid(
                frame,
                ValidationRules.ComplexType,
                "The attribute " + Display(qname) + " is not allowed on element " + Display(frame.QName) + ".",
                "cvc-complex-type.3.2.1"
            );
            FireAttribute(frame, entry, null, null, false, false);
        }
        else if (!wildcard.Allows(qname))
        {
            Invalid(
                frame,
                ValidationRules.WildcardNamespace,
                "The attribute "
                    + Display(qname)
                    + " of element "
                    + Display(frame.QName)
                    + " is not in a namespace that the attribute wildcard allows.",
                "cvc-wildcard-namespace"
            );
            FireAttribute(frame, entry, null, null, false, false);
        }
        else if (wildcard.ProcessContents == XsdProcessContents.Skip)
        {
            FireAttribute(frame, entry, null, null, true, false);
        }
        else
        {
            AssessByDeclaration(frame, entry, qname, wildcard.ProcessContents == XsdProcessContents.Strict);
        }
    }

    // The global attribute declaration of the name assesses the value; without one, strict is an error and lax is not assessed.
    private void AssessByDeclaration(ElementFrame frame, AttributeEntry entry, XPathQName qname, bool strict)
    {
        if (
            !_model.Schema.TryGet(XsdComponentKind.AttributeDeclaration, qname, out var component)
            || component is not AttributeDeclaration declaration
        )
        {
            if (strict)
            {
                Invalid(
                    frame,
                    ValidationRules.Wildcard,
                    "The attribute wildcard of element "
                        + Display(frame.QName)
                        + " is strict, but no declaration was found for the attribute "
                        + Display(qname)
                        + ".",
                    "cvc-wildcard.2"
                );
            }

            FireAttribute(frame, entry, null, null, !strict, false);
            return;
        }

        AssessDeclared(frame, entry, qname, declaration, declaration.ValueConstraint);
    }

    // The value must be valid for the type of the declaration and equal to a fixed value.
    private void AssessDeclared(
        ElementFrame frame,
        AttributeEntry entry,
        XPathQName qname,
        AttributeDeclaration declaration,
        AttributeValueConstraint constraint
    )
    {
        var value = ValueOf(entry);
        var valid = AssessValue(declaration.TypeDefinition, value, frame.Location, qname, true, out _);
        if (valid && !SatisfiesFixed(constraint, value))
        {
            Report(
                ValidationRules.Au,
                frame.Location,
                "The value '" + Shorten(value) + "' of attribute " + Display(qname) + " is not its fixed value.",
                null
            );
            valid = false;
        }

        if (!valid)
        {
            frame.IsValid = false;
        }

        FireAttribute(frame, entry, declaration, declaration.TypeDefinition, valid, false);
    }

    private void ApplyDefault(ElementFrame frame, AttributeUse use)
    {
        if (_onAttribute.Length == 0)
        {
            return;
        }

        var declaration = use.Declaration;
        var name = Names.Intern(
            ReadOnlySpan<char>.Empty,
            declaration.Name.LocalName.AsSpan(),
            declaration.Name.NamespaceName.AsSpan()
        );
        var value = (use.ValueConstraint.Value ?? string.Empty).AsSpan();
        var info = new AttributeInfo(
            frame.Ordinal,
            name,
            frame.Location,
            declaration,
            value,
            declaration.TypeDefinition,
            true,
            true
        );
        for (var i = 0; i < _onAttribute.Length; i++)
        {
            _onAttribute[i].Attribute(info);
        }
    }

    private void FireAttribute(
        ElementFrame frame,
        AttributeEntry entry,
        IXsdComponent? declaration,
        IXsdComponent? type,
        bool valid,
        bool defaulted
    )
    {
        if (_onAttribute.Length == 0)
        {
            return;
        }

        var info = new AttributeInfo(
            frame.Ordinal,
            entry.Name,
            frame.Location,
            declaration,
            ValueOf(entry),
            type,
            valid,
            defaulted
        );
        for (var i = 0; i < _onAttribute.Length; i++)
        {
            _onAttribute[i].Attribute(info);
        }
    }

    private ReadOnlySpan<char> ValueOf(AttributeEntry entry) => _attributeChars.AsSpan(entry.Start, entry.Length);

    // The attributes of the schema instance namespace that control assessment itself are not assessed as attributes: xsi:type and
    // xsi:nil are decided in BeginContent, the schema location hints are read by SchemaLocationHints.
    private bool IsXsiControl(XmlName name, out XPathQName qname)
    {
        qname = new XPathQName(Names.GetString(name.NamespaceUri), Names.GetString(name.Local));
        return name.NamespaceUri.Value != 0
            && string.Equals(qname.NamespaceName, XsiNamespace, StringComparison.Ordinal)
            && qname.LocalName is "type" or "nil" or "schemaLocation" or "noNamespaceSchemaLocation";
    }

    private static int FindUse(IReadOnlyList<AttributeUse> uses, XPathQName name)
    {
        for (var i = 0; i < uses.Count; i++)
        {
            if (uses[i].Name.Equals(name))
            {
                return i;
            }
        }

        return -1;
    }
}
