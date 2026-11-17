namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Plays the documented event order of one document, <c>&lt;a b="1"&gt;7&lt;/a&gt;</c>, to the given listeners in the
/// given order and calls a hook only when the interest of the listener names it.
/// </summary>
internal static class FakeListenerDriver
{
    public static void Run(IReadOnlyList<ValidationListener> listeners)
    {
        var limits = ValidationLimits.Default;
        var names = new SpanInterningNameTable(1000);
        var name = names.Intern("", "a", "");
        var context = new ValidationRunContext(
            limits,
            names,
            new XsdIssueReporter(new ValidationIssueCollector(limits))
        );
        var location = new DocumentLocation("file:///a.xml", 1, 1);

        foreach (var listener in listeners)
        {
            listener.BeginRun(context);
        }

        foreach (var listener in listeners)
        {
            if (listener.Interest.HasFlag(ListenerInterest.StartElement))
            {
                listener.StartElement(new ElementStartInfo(1, name, location, null, null, 1));
            }
        }

        foreach (var listener in listeners)
        {
            if (listener.Interest.HasFlag(ListenerInterest.Attribute))
            {
                listener.Attribute(
                    new AttributeInfo(1, name, location, null, "1".AsSpan(), null, isValid: true, isDefaulted: false)
                );
            }
        }

        foreach (var listener in listeners)
        {
            if (listener.Interest.HasFlag(ListenerInterest.SimpleValue))
            {
                listener.SimpleValue(new SimpleValueInfo(1, "7".AsSpan(), null, -1, isValid: true, isDefaulted: false));
            }
        }

        foreach (var listener in listeners)
        {
            if (listener.Interest.HasFlag(ListenerInterest.EndElement))
            {
                listener.EndElement(new ElementEndInfo(1, isValid: true, isNilled: false, null));
            }
        }

        foreach (var listener in listeners)
        {
            listener.EndRun();
        }
    }
}
