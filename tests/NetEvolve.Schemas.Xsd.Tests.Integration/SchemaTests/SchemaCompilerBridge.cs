namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>
/// Calls the schema compilation pipeline of the Xsd package. The pipeline is internal and the integration tests have no access to
/// the internals of the package (design #146 DP-1 gives that to the unit tests only), so the bridge binds to it by reflection and
/// fails at construction when a member is renamed, which <c>SchemaCompilerBridgeTests</c> pins.
/// </summary>
internal sealed class SchemaCompilerBridge
{
    private const string Namespace = "NetEvolve.Schemas.Xsd.Compilation";

    private readonly object _compiler;
    private readonly MethodInfo _compile;
    private readonly MethodInfo _fromBytes;
    private readonly Type _listType;
    private readonly PropertyInfo _result;

    public SchemaCompilerBridge(XsdVersion version, IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        var assembly = typeof(XsdValidatorBuilder).Assembly;
        var compilerType = Find(assembly, "SchemaCompiler");
        var inputType = Find(assembly, "SchemaInput");
        var compilationType = Find(assembly, "SchemaCompilation");
        _listType = typeof(List<>).MakeGenericType(inputType);
        _compile = compilerType.GetMethod(
            "Compile",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IReadOnlyList<>).MakeGenericType(inputType), typeof(CancellationToken)],
            null
        ) ?? throw new MissingMethodException(compilerType.FullName, "Compile");
        _fromBytes = inputType.GetMethod("FromBytes", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(inputType.FullName, "FromBytes");
        _result = compilationType.GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(compilationType.FullName, "Result");
        _compiler = Activator.CreateInstance(
            compilerType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [version, resolver, limits],
            null
        ) ?? throw new InvalidOperationException("The schema compiler could not be created.");
    }

    /// <summary>Compiles the documents as one schema.</summary>
    /// <param name="documents">The bytes of every root document with its base URI.</param>
    /// <param name="cancellationToken">A token to cancel the compilation.</param>
    /// <returns>The result of the compilation: valid when the documents are a valid schema.</returns>
    public ValidationResult Compile(
        IReadOnlyList<(ReadOnlyMemory<byte> Content, string BaseUri)> documents,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(documents);

        var inputs = (IList)Activator.CreateInstance(_listType)!;
        foreach (var (content, baseUri) in documents)
        {
            _ = inputs.Add(_fromBytes.Invoke(null, [content, baseUri]));
        }

        try
        {
            var compilation = _compile.Invoke(_compiler, [inputs, cancellationToken])!;
            return (ValidationResult)_result.GetValue(compilation)!;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw;
        }
    }

    private static Type Find(Assembly assembly, string name) =>
        assembly.GetType(Namespace + "." + name) ?? throw new TypeLoadException(Namespace + "." + name);
}
