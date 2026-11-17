namespace NetEvolve.Schemas.XPath.Context;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The dynamic context of an evaluation (XPath 2.0, 2.1.2). Not thread-safe and reusable; use one per thread.</summary>
public sealed class XPathEvaluationContext : IDisposable
{
    private const int MaxTimezoneMinutes = 14 * 60;

    private bool _disposed;
    private int _implicitTimezoneMinutes;
    private DateTimeOffset? _currentDateTime;

    /// <summary>Initializes a new instance of the <see cref="XPathEvaluationContext"/> class.</summary>
    public XPathEvaluationContext()
    {
        Variables = new XPathVariables();
        Host = new XPathHostSlots();
    }

    /// <summary>Gets the variables.</summary>
    public XPathVariables Variables { get; }

    /// <summary>Gets the host slots: current item, current group, regex groups, decimal formats, and keys.</summary>
    public XPathHostSlots Host { get; }

    /// <summary>Gets or sets the resolver for external resources; <see langword="null"/> denies them (FODC0002).</summary>
    public IXPathResourceResolver? ResourceResolver { get; set; }

    internal XPathFocus Focus { get; private set; }

    internal XdmItem? ContextItem => Focus.Item;

    internal int Position => Focus.Position;

    internal int Size => Focus.Size;

    /// <summary>Gets or sets the implicit timezone in minutes from UTC, -840 to 840; UTC by default for identical results on every host.</summary>
    internal int ImplicitTimezoneMinutes
    {
        get => _implicitTimezoneMinutes;
        set
        {
            Argument.ThrowIfLessThan(value, -MaxTimezoneMinutes);
            Argument.ThrowIfGreaterThan(value, MaxTimezoneMinutes);

            _implicitTimezoneMinutes = value;
            _currentDateTime = null;
        }
    }

    /// <summary>Gets or sets the clock read once per evaluation for <see cref="CurrentDateTime"/>.</summary>
    internal Func<DateTimeOffset> Clock { get; set; } = static () => DateTimeOffset.UtcNow;

    /// <summary>Gets the current dateTime in the implicit timezone; the same value until the next <see cref="Reset"/>.</summary>
    internal DateTimeOffset CurrentDateTime =>
        _currentDateTime ??= Clock().ToOffset(TimeSpan.FromMinutes(_implicitTimezoneMinutes));

    /// <summary>Makes a child focus current, for a step or predicate; copies nothing but the three focus fields.</summary>
    /// <param name="item">The context item.</param>
    /// <param name="position">The one-based position.</param>
    /// <param name="size">The size.</param>
    /// <returns>The previous focus, to hand to <see cref="RestoreFocus"/>.</returns>
    internal XPathFocus EnterFocus(in XdmItem item, int position, int size)
    {
        var previous = Focus;
        Focus = new XPathFocus(item, position, size);
        return previous;
    }

    /// <summary>Restores the focus returned by <see cref="EnterFocus"/>.</summary>
    /// <param name="previous">The previous focus.</param>
    internal void RestoreFocus(in XPathFocus previous) => Focus = previous;

    /// <summary>Sets the focus: the context item, its position, and the size of the sequence it came from.</summary>
    /// <param name="item">The context item.</param>
    /// <param name="position">The one-based position.</param>
    /// <param name="size">The size, at least <paramref name="position"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is below one or <paramref name="size"/> is below it.</exception>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    public void SetContextItem(in XdmItem item, int position = 1, int size = 1)
    {
        ThrowIfDisposed();
        Argument.ThrowIfLessThan(position, 1);
        Argument.ThrowIfLessThan(size, position);

        Focus = new XPathFocus(item, position, size);
    }

    /// <summary>Clears the focus, the variables, and the host slots; the resource resolver stays.</summary>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    public void Reset()
    {
        ThrowIfDisposed();

        Focus = default;
        _currentDateTime = null;
        Variables.Clear();
        Host.Clear();
    }

    /// <summary>Releases the context; the results of its evaluations become invalid.</summary>
    public void Dispose() => _disposed = true;

    internal IXPathResourceResolver RequireResourceResolver() =>
        ResourceResolver ?? throw XPathErrors.Exception(XPathErrorCodes.FODC0002);

    // Decision (#449): ThrowIfDisposed has no NetEvolve.Arguments.Argument equivalent and ObjectDisposedException.ThrowIf does not exist on netstandard; it stays the single explicit throw.
#pragma warning disable CA1513
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(XPathEvaluationContext));
        }
    }
#pragma warning restore CA1513
}
