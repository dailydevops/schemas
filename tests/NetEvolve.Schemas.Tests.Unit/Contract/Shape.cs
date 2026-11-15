namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>Renders the public and protected surface of a type as sorted, comparable signature strings.</summary>
internal static class Shape
{
    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    internal static string[] Of(Type type)
    {
        var lines = new List<string>();
        if (type.IsEnum)
        {
            foreach (var name in Enum.GetNames(type))
            {
                var value = Convert.ToInt64(Enum.Parse(type, name), CultureInfo.InvariantCulture);
                lines.Add($"{name}={value.ToString(CultureInfo.InvariantCulture)}");
            }
        }
        else
        {
            foreach (var constructor in type.GetConstructors(All))
            {
                if (IsExposed(constructor.IsPublic, constructor.IsFamily || constructor.IsFamilyOrAssembly))
                {
                    lines.Add($"{Access(constructor.IsPublic)}.ctor({Parameters(constructor.GetParameters())})");
                }
            }

            foreach (var field in type.GetFields(All))
            {
                if (IsExposed(field.IsPublic, field.IsFamily || field.IsFamilyOrAssembly))
                {
                    lines.Add(
                        field.IsLiteral
                            ? $"const {Name(field.FieldType)} {field.Name}={Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}"
                            : $"{(field.IsStatic ? "static " : "")}{Name(field.FieldType)} {field.Name}"
                    );
                }
            }

            foreach (var property in type.GetProperties(All))
            {
                var getter = property.GetMethod ?? property.SetMethod!;
                if (IsExposed(getter.IsPublic, getter.IsFamily || getter.IsFamilyOrAssembly))
                {
                    var accessors = property.SetMethod is null ? "get" : "get; set";
                    lines.Add(
                        $"{Access(getter.IsPublic)}{(getter.IsStatic ? "static " : "")}{Name(property.PropertyType)} {property.Name} {{ {accessors} }}"
                    );
                }
            }

            foreach (var method in type.GetMethods(All))
            {
                if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsExposed(method.IsPublic, method.IsFamily || method.IsFamilyOrAssembly))
                {
                    lines.Add(
                        $"{Access(method.IsPublic)}{Modifier(method, type)}{Name(method.ReturnType)} {method.Name}({Parameters(method.GetParameters())})"
                    );
                }
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return [.. lines];
    }

    internal static string[] Sorted(params string[] expected)
    {
        Array.Sort(expected, StringComparer.Ordinal);
        return expected;
    }

    private static bool IsExposed(bool isPublic, bool isProtected) => isPublic || isProtected;

    private static string Access(bool isPublic) => isPublic ? "" : "protected ";

    private static string Modifier(MethodInfo method, Type type)
    {
        if (type.IsInterface)
        {
            return "";
        }

        if (method.IsStatic)
        {
            return "static ";
        }

        if (method.IsAbstract)
        {
            return "abstract ";
        }

        return method.IsVirtual && !method.IsFinal ? "virtual " : "";
    }

    private static string Parameters(ParameterInfo[] parameters)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            if (parameters[i].ParameterType.IsByRef)
            {
                builder.Append(parameters[i].IsIn ? "in " : "ref ");
            }

            builder.Append(Name(parameters[i].ParameterType));
        }

        return builder.ToString();
    }

    private static string Name(Type type)
    {
        if (type.IsByRef)
        {
            return Name(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            return Name(type.GetElementType()!) + "[]";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name.Substring(0, type.Name.IndexOf('`', StringComparison.Ordinal));
        var arguments = type.GetGenericArguments();
        var builder = new StringBuilder(name).Append('<');
        for (var i = 0; i < arguments.Length; i++)
        {
            builder.Append(i > 0 ? "," : "").Append(Name(arguments[i]));
        }

        return builder.Append('>').ToString();
    }
}
