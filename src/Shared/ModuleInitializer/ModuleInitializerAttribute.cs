namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill of the attribute that marks a method as the initializer of the module; the BCL has it from net5.0 on. Only the
/// project that registers function groups compiles this file (not the shared polyfill folder, which every project compiles): two
/// assemblies that both define the internal type conflict where one sees the internals of the other (CS0436). Assemblies above
/// that project use its internal type through <c>InternalsVisibleTo</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class ModuleInitializerAttribute : Attribute { }
