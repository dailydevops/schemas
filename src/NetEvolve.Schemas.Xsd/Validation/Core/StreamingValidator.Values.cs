namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

internal sealed partial class StreamingValidator
{
    // The value of an element with simple content is its coalesced character data. An empty element takes the default or
    // fixed value of its declaration, which is assessed like a given value and reported to the listeners as defaulted.
    private void EndSimpleContent(ElementFrame frame)
    {
        if (frame.ContentFailed || frame.SimpleType is null)
        {
            return;
        }

        var constraint = frame.Declaration?.ValueConstraint ?? default;
        var defaulted = !frame.HasText && constraint.Kind != ElementValueConstraintKind.None;
        var value = defaulted ? constraint.Value.AsSpan() : _text.AsSpan(0, _textLength);
        var valid = AssessValue(frame.SimpleType, value, frame.Location, frame.QName, false, out var member);
        if (
            valid
            && !defaulted
            && constraint.Kind == ElementValueConstraintKind.Fixed
            && !IsEqual(value, constraint.Value)
        )
        {
            Invalid(
                frame,
                ValidationRules.Elt,
                "The value '"
                    + Shorten(value)
                    + "' of element "
                    + Display(frame.QName)
                    + " is not its fixed value '"
                    + constraint.Value
                    + "'.",
                "cvc-elt.5.2.2.2.2"
            );
            valid = false;
        }

        if (!valid)
        {
            frame.IsValid = false;
        }

        if (_onValue.Length > 0)
        {
            var info = new SimpleValueInfo(frame.Ordinal, value, frame.SimpleType, member, valid, defaulted);
            for (var i = 0; i < _onValue.Length; i++)
            {
                _onValue[i].SimpleValue(info);
            }
        }
    }

    // An element with mixed content and a value constraint: without children and text it takes the value (cvc-elt.5.1), a fixed
    // value is compared lexically with the character data of an element without children (cvc-elt.5.2.2).
    private void EndMixedValue(ElementFrame frame)
    {
        var constraint = frame.Declaration?.ValueConstraint ?? default;
        if (constraint.Kind == ElementValueConstraintKind.None || frame.ContentFailed)
        {
            return;
        }

        if (frame.HasChild)
        {
            if (constraint.Kind == ElementValueConstraintKind.Fixed)
            {
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The element " + Display(frame.QName) + " has a fixed value, so it must not have child elements.",
                    "cvc-elt.5.2.2.1"
                );
            }

            return;
        }

        if (frame.HasText)
        {
            var text = _text.AsSpan(0, _textLength);
            if (constraint.Kind == ElementValueConstraintKind.Fixed && !IsEqual(text, constraint.Value))
            {
                Invalid(
                    frame,
                    ValidationRules.Elt,
                    "The value '"
                        + Shorten(text)
                        + "' of element "
                        + Display(frame.QName)
                        + " is not its fixed value '"
                        + constraint.Value
                        + "'.",
                    "cvc-elt.5.2.2.2.1"
                );
            }

            return;
        }

        if (_onValue.Length > 0)
        {
            var info = new SimpleValueInfo(frame.Ordinal, constraint.Value.AsSpan(), null, -1, true, true);
            for (var i = 0; i < _onValue.Length; i++)
            {
                _onValue[i].SimpleValue(info);
            }
        }
    }

    // The datatype contract decides; a failure is reported by the rule of the failed facet and carries the datatype error.
    private bool AssessValue(
        IXsdComponent? type,
        ReadOnlySpan<char> value,
        DocumentLocation location,
        XPathQName name,
        bool isAttribute,
        out int memberIndex
    )
    {
        memberIndex = -1;
        if (type is null)
        {
            return true;
        }

        var result = _assessor.Assess(type, value, out memberIndex);
        if (result.IsValid)
        {
            return true;
        }

        Report(
            ValidationRules.ForFacet(result.FailedFacet),
            location,
            "The value '"
                + Shorten(value)
                + "' of "
                + (isAttribute ? "attribute " : "element ")
                + Display(name)
                + " is not valid: "
                + result.Error
                + ".",
            null
        );
        return false;
    }

    // Limitation: fixed values are compared lexically, not in the value space of the type.
    private static bool SatisfiesFixed(AttributeValueConstraint constraint, ReadOnlySpan<char> value) =>
        constraint.Kind != AttributeValueConstraintKind.Fixed || IsEqual(value, constraint.Value);

    private static bool IsEqual(ReadOnlySpan<char> value, string? expected) =>
        value.Equals((expected ?? string.Empty).AsSpan(), StringComparison.Ordinal);

    private static string Shorten(ReadOnlySpan<char> value) =>
        value.Length <= MaxReportedValue ? value.ToString() : value.Slice(0, MaxReportedValue).ToString() + "...";
}
