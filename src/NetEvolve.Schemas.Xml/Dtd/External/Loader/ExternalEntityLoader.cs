namespace NetEvolve.Schemas.Xml.Dtd.External.Loader;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Limits;

/// <summary>
/// The single access path of the external subset and the external parsed entities (design #122, sections 4 and 8): it
/// resolves a system identifier against the base URI of the declaring entity, requests it through the options (deny by
/// default), rejects recursion, charges the limits, and hands back a frame that decodes the entity with its own encoding.
/// </summary>
/// <remarks>
/// The order of the checks is: recursion (<c>XML0020</c>, by kind and name or by resolved URI on the open frames), depth
/// (<c>XML1110</c>), the number of resolver calls (<c>XML1117</c>), then the options, which deny a kind whose flag is off
/// without calling the resolver (<c>XML2002</c>). The decoded characters of every frame are charged to
/// <c>MaxEntityExpansionCharacters</c> (<c>XML1109</c>) when the frame is read. The loader belongs to one document and is
/// not thread-safe. The loader owns the budgets of the document: the external subset and the external general entities share
/// them, and the internal entity stack charges the characters of its replacement texts to them (XML-62). Limitations:
/// <c>MaxResourceSize</c> is not applied: the bytes of a frame are
/// bounded by the expansion budget, because a frame is read in chunks and never buffered completely (#122, D9 rule 5).
/// An <c>xml:base</c> attribute does not take part in the resolution (#122, section 12). A request that the options
/// refuse still consumes one <c>MaxExternalEntityRequests</c> charge, because the charge happens before the options are
/// asked. Limitations: the recursion check compares the resolved URI of the request only, not the final base URI the
/// resolver reports, so a redirect cycle is bounded by the depth and request budgets alone. A fragment identifier in a
/// system identifier (XML 1.0 section 4.2.2) is neither rejected nor stripped (tracked in #689).
/// </remarks>
internal sealed class ExternalEntityLoader
{
    private readonly DtdValidationOptions _options;
    private readonly XmlReaderLimits _limits;
    private readonly List<ExternalEntityFrame> _active = [];
    private XmlBudget _depth;
    private XmlBudget _requests;
    private XmlBudget _expansion;

    /// <summary>Initializes a new instance of the <see cref="ExternalEntityLoader"/> class.</summary>
    /// <param name="options">The options: the resolver, the flags, and the limits of DTD validation.</param>
    /// <param name="limits">The reader limits: the depth, the expansion budget, and the length of the text declaration.</param>
    internal ExternalEntityLoader(DtdValidationOptions options, XmlReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(limits);

        _options = options;
        _limits = limits;
        _depth = XmlBudget.EntityDepth(limits);
        _expansion = XmlBudget.ExpansionCharacters(limits);
        _requests = new XmlBudget(options.DtdLimits.MaxExternalEntityRequests, DtdLimitCodes.XML1117);
    }

    /// <summary>Requests an external entity or the external subset.</summary>
    /// <param name="kind">The kind; it must be an external subset, an external entity, or an external parameter entity.</param>
    /// <param name="name">The entity name, or the document type name for the external subset; with the kind it identifies the frame for the recursion check.</param>
    /// <param name="publicId">The public identifier, or <see langword="null"/>.</param>
    /// <param name="systemId">The system identifier as written; it is resolved against <paramref name="declaringBaseUri"/>.</param>
    /// <param name="declaringBaseUri">The base URI of the entity that contains the declaration (section 4.2.2), or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The token that cancels the request and the reads of the frame.</param>
    /// <param name="frame">The open frame when the method returns <see langword="true"/>; the caller disposes it.</param>
    /// <param name="failure">The reason when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity was found and its head was read.</returns>
    internal bool TryLoad(
        ResourceKind kind,
        string name,
        string? publicId,
        string systemId,
        string? declaringBaseUri,
        CancellationToken cancellationToken,
        out ExternalEntityFrame? frame,
        out ExternalEntityFailure failure
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(systemId);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)ResourceKind.ExternalParameterEntity);

        frame = null;
        failure = default;

        // A reference that cannot be resolved still goes to the options, so every refusal ends in the same denial.
        var resolvedUri = UriReference.Resolve(systemId, declaringBaseUri).Uri;
        if (IsActive(kind, name, resolvedUri))
        {
            failure = Fail(XmlErrorCodes.XML0020);
            return false;
        }

        if (!_depth.TryCharge(1))
        {
            failure = Fail(_depth.Code);
            return false;
        }

        if (!_requests.TryCharge(1))
        {
            _depth.Release(1);
            failure = Fail(_requests.Code);
            return false;
        }

        var opened = false;
        try
        {
            var request = new ResourceRequest(kind, systemId, declaringBaseUri, resolvedUri, publicId);
            opened = TryOpen(in request, name, cancellationToken, out frame, out failure);

            return opened;
        }
        finally
        {
            if (!opened)
            {
                _depth.Release(1);
            }
        }
    }

    /// <summary>Charges decoded characters of a frame to the expansion budget.</summary>
    /// <param name="count">The number of characters.</param>
    /// <returns><see langword="false"/> when the budget is exceeded; <c>XML1109</c> is then the error to report.</returns>
    internal bool TryChargeExpansion(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        return _expansion.TryCharge(count);
    }

    /// <summary>Closes a frame: it no longer takes part in the recursion check and gives its depth back; repeated calls do nothing.</summary>
    /// <param name="frame">The frame.</param>
    internal void Leave(ExternalEntityFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_active.Remove(frame))
        {
            _depth.Release(1);
        }
    }

    // Requests the resource through the options and opens the frame; the frame joins the open frames on success.
    private bool TryOpen(
        in ResourceRequest request,
        string name,
        CancellationToken cancellationToken,
        out ExternalEntityFrame? frame,
        out ExternalEntityFailure failure
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        frame = null;
        failure = default;
        var result = _options.Resolve(in request, cancellationToken);
        if (result.Status != ResourceStatus.Found)
        {
            var message = result.Message ?? XmlErrorCodes.XML2002.Message;
            result.Dispose();
            failure = new ExternalEntityFailure(XmlErrorCodes.XML2002, message);
            return false;
        }

        bool created;
        ExternalEntityFrame? opened;
        XmlErrorCode? error;
        try
        {
            created = ExternalEntityFrame.TryCreate(
                this,
                request.Kind,
                name,
                request.ResolvedUri,
                result,
                _limits,
                cancellationToken,
                out opened,
                out error
            );
        }
        catch
        {
            // The token was canceled between the resolver and the frame, or a guard failed: nobody else owns the stream.
            result.Dispose();
            throw;
        }

        if (!created)
        {
            failure = Fail(error!);
            return false;
        }

        _active.Add(opened!);
        frame = opened;
        return true;
    }

    private bool IsActive(ResourceKind kind, string name, string? resolvedUri)
    {
        for (var i = 0; i < _active.Count; i++)
        {
            var open = _active[i];
            if (
                (open.Kind == kind && string.Equals(open.Name, name, StringComparison.Ordinal))
                || (resolvedUri is not null && string.Equals(open.ResolvedUri, resolvedUri, StringComparison.Ordinal))
            )
            {
                return true;
            }
        }

        return false;
    }

    private static ExternalEntityFailure Fail(XmlErrorCode code) => new(code, code.Message);
}
