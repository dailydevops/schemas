namespace NetEvolve.Schemas.Tests.Unit.Building;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

public class BuilderContractTests
{
    private sealed record Call(
        string Hook,
        ValidationLimits Limits,
        IResourceResolver Resolver,
        CancellationToken Token
    );

    private sealed class RecordingBuilder : SchemaValidatorBuilder<RecordingBuilder, FakeValidator>
    {
        public List<Call> Calls { get; } = [];

        public Func<CancellationToken, ValueTask<FakeValidator>>? AsyncBehavior { get; set; }

        protected override FakeValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver)
        {
            Calls.Add(new("sync", limits, resolver, default));
            return new();
        }

        protected override ValueTask<FakeValidator> CreateValidatorAsync(
            ValidationLimits limits,
            IResourceResolver resolver,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(new("async", limits, resolver, cancellationToken));
            return AsyncBehavior is { } behavior ? behavior(cancellationToken) : new(new FakeValidator());
        }
    }

    private sealed class SyncOnlyBuilder : SchemaValidatorBuilder<SyncOnlyBuilder, FakeValidator>
    {
        public List<(ValidationLimits Limits, IResourceResolver Resolver)> Calls { get; } = [];

        protected override FakeValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver)
        {
            Calls.Add((limits, resolver));
            return new();
        }
    }

    private sealed class ThrowingBuilder(Exception exception) : SchemaValidatorBuilder<ThrowingBuilder, FakeValidator>
    {
        protected override FakeValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
            throw exception;

        protected override ValueTask<FakeValidator> CreateValidatorAsync(
            ValidationLimits limits,
            IResourceResolver resolver,
            CancellationToken cancellationToken
        ) => throw exception;
    }

    private static FakeValidator BuildSync<TBuilder>(SchemaValidatorBuilder<TBuilder, FakeValidator> builder)
        where TBuilder : SchemaValidatorBuilder<TBuilder, FakeValidator> => builder.Build();

    [Test]
    public async Task Limits_WithoutWithLimits_IsDefault() =>
        await Assert.That(new RecordingBuilder().Limits).IsSameReferenceAs(ValidationLimits.Default);

    [Test]
    public async Task Resolver_WithoutWithResolver_IsDenyAllInstance() =>
        await Assert.That(new RecordingBuilder().Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);

    [Test]
    public async Task Build_WithoutWithResolver_PassesDenyAllInstanceToCreateValidator()
    {
        var builder = new RecordingBuilder();

        _ = BuildSync(builder);

        await Assert.That(builder.Calls[0].Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task BuildAsync_WithoutWithResolver_PassesDenyAllInstanceToCreateValidatorAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new RecordingBuilder();

        _ = await builder.BuildAsync(cancellationToken);

        await Assert.That(builder.Calls[0].Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task WithLimits_ReturnsSameBuilderAndStoresLimits()
    {
        var builder = new RecordingBuilder();
        var limits = ValidationLimits.Default.WithMaxDepth(3);

        await Assert.That(builder.WithLimits(limits)).IsSameReferenceAs(builder);
        await Assert.That(builder.Limits).IsSameReferenceAs(limits);
    }

    [Test]
    public async Task WithResolver_ReturnsSameBuilderAndStoresResolver()
    {
        var builder = new RecordingBuilder();
        var resolver = new InMemoryResourceResolver();

        await Assert.That(builder.WithResolver(resolver)).IsSameReferenceAs(builder);
        await Assert.That(builder.Resolver).IsSameReferenceAs(resolver);
    }

    [Test]
    public async Task NullArguments_ThrowArgumentNullException()
    {
        var builder = new RecordingBuilder();

        await Assert.That(() => builder.WithLimits(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithResolver(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Build_PassesLimitsAndResolverToCreateValidator()
    {
        var limits = ValidationLimits.Default.WithMaxIssues(5);
        var resolver = new InMemoryResourceResolver();
        var builder = new RecordingBuilder().WithLimits(limits).WithResolver(resolver);

        _ = BuildSync(builder);

        await Assert.That(builder.Calls.Count).IsEqualTo(1);
        await Assert.That(builder.Calls[0].Hook).IsEqualTo("sync");
        await Assert.That(builder.Calls[0].Limits).IsSameReferenceAs(limits);
        await Assert.That(builder.Calls[0].Resolver).IsSameReferenceAs(resolver);
    }

    [Test]
    public async Task BuildAsync_PassesLimitsResolverAndTokenToCreateValidatorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var limits = ValidationLimits.Default.WithMaxIssues(5);
        var resolver = new InMemoryResourceResolver();
        var builder = new RecordingBuilder().WithLimits(limits).WithResolver(resolver);
        using var source = new CancellationTokenSource();

        var validator = await builder.BuildAsync(source.Token);

        await Assert.That(validator).IsNotNull();
        await Assert.That(builder.Calls.Count).IsEqualTo(1);
        await Assert.That(builder.Calls[0].Hook).IsEqualTo("async");
        await Assert.That(builder.Calls[0].Limits).IsSameReferenceAs(limits);
        await Assert.That(builder.Calls[0].Resolver).IsSameReferenceAs(resolver);
        await Assert.That(builder.Calls[0].Token).IsEqualTo(source.Token);
    }

    [Test]
    public async Task DefaultCreateValidatorAsync_CompletesSynchronouslyOverCreateValidator(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var limits = ValidationLimits.Default.WithMaxIssues(9);
        var resolver = new InMemoryResourceResolver();
        var builder = new SyncOnlyBuilder().WithLimits(limits).WithResolver(resolver);

        var pending = builder.BuildAsync(cancellationToken);

        await Assert.That(pending.IsCompletedSuccessfully).IsTrue();
        await Assert.That(await pending).IsNotNull();
        await Assert.That(builder.Calls.Count).IsEqualTo(1);
        await Assert.That(builder.Calls[0].Limits).IsSameReferenceAs(limits);
        await Assert.That(builder.Calls[0].Resolver).IsSameReferenceAs(resolver);
    }

    [Test]
    public async Task Build_AfterBuilderChange_FirstValidatorKeepsItsLimits()
    {
        var first = ValidationLimits.Default.WithMaxDepth(10);
        var second = ValidationLimits.Default.WithMaxDepth(20);
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());

        var validator1 = BuildSync(builder.WithLimits(first));
        var validator2 = BuildSync(builder.WithLimits(second));

        await Assert.That(validator2).IsNotSameReferenceAs(validator1);
        await Assert.That(builder.Calls[0].Limits).IsSameReferenceAs(first);
        await Assert.That(builder.Calls[0].Limits.MaxDepth).IsEqualTo(10);
        await Assert.That(builder.Calls[1].Limits).IsSameReferenceAs(second);
        await Assert.That(builder.Calls[1].Limits.MaxDepth).IsEqualTo(20);
    }

    [Test]
    public async Task Build_ExceptionFromCreateValidator_Propagates()
    {
        var builder = new ThrowingBuilder(new InvalidOperationException("boom")).WithResolver(
            new InMemoryResourceResolver()
        );

        await Assert.That(() => BuildSync(builder)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task BuildAsync_TokenAlreadyCanceled_ReturnsCanceledTaskWithoutCallingHook()
    {
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());
        using var source = new CancellationTokenSource();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        var task = builder.BuildAsync(source.Token).AsTask();

        await Assert.That(task.IsCanceled).IsTrue();
        await Assert.That(builder.Calls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BuildAsync_CallerCancelsDuringHook_ReturnsCanceledTask()
    {
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());
        using var source = new CancellationTokenSource();
        builder.AsyncBehavior = async token =>
        {
#if NET8_0_OR_GREATER
            await source.CancelAsync();
#else
            source.Cancel();
#endif
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            return new FakeValidator();
        };

        var task = builder.BuildAsync(source.Token).AsTask();

        await Task.WhenAny(task);

        await Assert.That(task.IsCanceled).IsTrue();
    }

    [Test]
    public async Task BuildAsync_ForeignOperationCanceledException_ReturnsFaultedTask(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());
        builder.AsyncBehavior = async _ =>
        {
            await Task.Yield();
            throw new OperationCanceledException("foreign");
        };

        var task = builder.BuildAsync(cancellationToken).AsTask();

        await Task.WhenAny(task);

        await Assert.That(task.IsFaulted).IsTrue();
        await Assert.That(task.IsCanceled).IsFalse();
        await Assert.That(task.Exception!.InnerException).IsTypeOf<OperationCanceledException>();
    }

    [Test]
    public async Task BuildAsync_ExceptionFromHook_ReturnsFaultedTaskInsteadOfThrowing(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = new ThrowingBuilder(new InvalidOperationException("boom")).WithResolver(
            new InMemoryResourceResolver()
        );

        var task = builder.BuildAsync(cancellationToken).AsTask();

        await Assert.That(task.IsFaulted).IsTrue();
        await Assert.That(task.Exception!.InnerException).IsTypeOf<InvalidOperationException>();
    }

    [Test]
    public async Task BuildAsync_HookCancelsCallerThenThrowsOperationCanceledSynchronously_ReturnsCanceledTask()
    {
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());
        using var source = new CancellationTokenSource();
        builder.AsyncBehavior = token =>
        {
#if NET8_0_OR_GREATER
            _ = source.CancelAsync();
#else
            source.Cancel();
#endif
            throw new OperationCanceledException(token);
        };

        var task = builder.BuildAsync(source.Token).AsTask();

        await Assert.That(task.IsCanceled).IsTrue();
    }

    [Test]
    public async Task BuildAsync_HookFaultsWithOperationCanceledAfterCallerCanceled_ReturnsCanceledTask()
    {
        var builder = new RecordingBuilder().WithResolver(new InMemoryResourceResolver());
        using var source = new CancellationTokenSource();
        var completion = new TaskCompletionSource<FakeValidator>();
        builder.AsyncBehavior = _ => new(completion.Task);

        var task = builder.BuildAsync(source.Token).AsTask();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif
        completion.SetException(new OperationCanceledException());
        await Task.WhenAny(task);

        await Assert.That(task.IsCanceled).IsTrue();
    }
}
