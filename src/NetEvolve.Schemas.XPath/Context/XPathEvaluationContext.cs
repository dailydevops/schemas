namespace NetEvolve.Schemas.XPath.Context;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The dynamic context of an evaluation (XPath 2.0, 2.1.2). Not thread-safe and reusable; use one per thread.</summary>
public sealed class XPathEvaluationContext : IDisposable
{
    private bool _disposed;

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

    internal XdmItem? ContextItem { get; private set; }

    internal int Position { get; private set; }

    internal int Size { get; private set; }

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

        ContextItem = item;
        Position = position;
        Size = size;
    }

    /// <summary>Clears the focus, the variables, and the host slots; the resource resolver stays.</summary>
    /// <exception cref="ObjectDisposedException">The context is disposed.</exception>
    public void Reset()
    {
        ThrowIfDisposed();

        ContextItem = null;
        Position = 0;
        Size = 0;
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
