namespace NetEvolve.Schemas.Formats;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;

/// <summary>Shared fluent builder base. Builders are not thread-safe; the validators they build are.</summary>
/// <typeparam name="TBuilder">The concrete builder type.</typeparam>
/// <typeparam name="TValidator">The validator type returned by <see cref="Build"/>.</typeparam>
public abstract class SchemaValidatorBuilder<TBuilder, TValidator>
    where TBuilder : SchemaValidatorBuilder<TBuilder, TValidator>
    where TValidator : ISchemaValidator
{
    private ValidationLimits? _limits;
    private IResourceResolver? _resolver;

    /// <summary>Initializes a new instance of the <see cref="SchemaValidatorBuilder{TBuilder, TValidator}"/> class.</summary>
    protected SchemaValidatorBuilder() { }

    /// <summary>Gets the limits, <see cref="ValidationLimits.Default"/> unless set.</summary>
    public ValidationLimits Limits => _limits ?? ValidationLimits.Default;

    /// <summary>Gets the resolver, never <see langword="null"/>, <see cref="DenyAllResourceResolver.Instance"/> unless set.</summary>
    public IResourceResolver Resolver => _resolver ?? DenyAllResourceResolver.Instance;

    /// <summary>Sets the limits.</summary>
    /// <param name="limits">The limits.</param>
    /// <returns>This builder.</returns>
    public TBuilder WithLimits(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        _limits = limits;
        return (TBuilder)this;
    }

    /// <summary>Sets the resolver.</summary>
    /// <param name="resolver">The resolver.</param>
    /// <returns>This builder.</returns>
    public TBuilder WithResolver(IResourceResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _resolver = resolver;
        return (TBuilder)this;
    }

    /// <summary>Builds an immutable validator.</summary>
    /// <returns>The validator.</returns>
    public TValidator Build() => CreateValidator(Limits, Resolver);

    /// <summary>Builds an immutable validator, reading schema sources asynchronously.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validator.</returns>
    public ValueTask<TValidator> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(Task.FromCanceled<TValidator>(cancellationToken));
        }

        ValueTask<TValidator> pending;
        try
        {
            pending = CreateValidatorAsync(Limits, Resolver, cancellationToken);
        }
        catch (Exception ex)
        {
            return new(
                ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                    ? Task.FromCanceled<TValidator>(cancellationToken)
                    : Task.FromException<TValidator>(ex)
            );
        }

        if (pending.IsCompletedSuccessfully)
        {
            return pending;
        }

        // Only the caller's own cancellation may end in a canceled task (an OperationCanceledException from elsewhere
        // stays faulted), so the outcome is mapped by a continuation instead of being awaited.
        return new(
            pending
                .AsTask()
                .ContinueWith(
                    static (task, state) =>
                    {
                        var callerToken = (CancellationToken)state!;
                        if (task.IsCanceled)
                        {
                            return callerToken.IsCancellationRequested
                                ? task
                                : Task.FromException<TValidator>(new OperationCanceledException());
                        }

                        return
                            task.IsFaulted
                            && callerToken.IsCancellationRequested
                            && task.Exception!.InnerExceptions[0] is OperationCanceledException
                            ? Task.FromCanceled<TValidator>(callerToken)
                            : task;
                    },
                    cancellationToken,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                )
                .Unwrap()
        );
    }

    /// <summary>Creates the validator from the snapshotted limits and resolver.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    /// <returns>The validator.</returns>
    protected abstract TValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver);

    /// <summary>Creates the validator asynchronously. The default completes synchronously over <see cref="CreateValidator"/>.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validator.</returns>
    protected virtual ValueTask<TValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    ) => new(CreateValidator(limits, resolver));
}
