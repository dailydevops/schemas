#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #137 AP-5)

namespace NetEvolve.Schemas;

using System;
using System.Buffers;
using System.IO;
using NetEvolve.Schemas.Formats;

/// <summary>
/// The Schematron entry point. C# 14 callers write <c>SchemaValidator.ForSchematron(...)</c>; compilers older than C# 14
/// call <see cref="SchematronSchemaValidatorExtensions.ForSchematron(Stream, string?)"/> on this class directly.
/// </summary>
/// <remarks>
/// Limitation: there is no <see cref="ReadOnlyMemory{T}"/> overload, because with one a <c>byte[]</c> or <c>char[]</c> argument
/// is ambiguous for compilers older than C# 14 (CS0121); pass <c>memory.Span</c>. No overload reads, parses, or compiles
/// anything; every load problem surfaces at <c>Build</c>.
/// </remarks>
public static class SchematronSchemaValidatorExtensions
{
    extension(SchemaValidator)
    {
        /// <summary>Starts a builder for a schema in a stream. The stream is read from its current position at <c>Build</c> and never disposed.</summary>
        /// <param name="schema">The stream.</param>
        /// <param name="systemId">The base URI of the schema for <c>include</c>, or <see langword="null"/>.</param>
        /// <returns>A new builder.</returns>
        public static SchematronValidatorBuilder ForSchematron(Stream schema, string? systemId = null) =>
            new(SchemaSourceHolder.FromStream(schema, systemId));

        /// <summary>Starts a builder for a schema in encoded bytes. The bytes are copied once.</summary>
        /// <param name="schema">The bytes.</param>
        /// <param name="systemId">The base URI of the schema for <c>include</c>, or <see langword="null"/>.</param>
        /// <returns>A new builder.</returns>
        public static SchematronValidatorBuilder ForSchematron(ReadOnlySpan<byte> schema, string? systemId = null) =>
            new(SchemaSourceHolder.FromBytes(schema, systemId));

        /// <summary>Starts a builder for a schema in text. The characters are copied once.</summary>
        /// <param name="schema">The characters.</param>
        /// <param name="systemId">The base URI of the schema for <c>include</c>, or <see langword="null"/>.</param>
        /// <returns>A new builder.</returns>
        public static SchematronValidatorBuilder ForSchematron(ReadOnlySpan<char> schema, string? systemId = null) =>
            new(SchemaSourceHolder.FromChars(schema, systemId));

        /// <summary>Starts a builder for a schema in a byte sequence. The segments are copied once.</summary>
        /// <param name="schema">The bytes.</param>
        /// <param name="systemId">The base URI of the schema for <c>include</c>, or <see langword="null"/>.</param>
        /// <returns>A new builder.</returns>
        public static SchematronValidatorBuilder ForSchematron(
            ReadOnlySequence<byte> schema,
            string? systemId = null
        ) => new(SchemaSourceHolder.FromBytes(in schema, systemId));

        /// <summary>Starts a builder for a schema in a character sequence. The segments are copied once.</summary>
        /// <param name="schema">The characters.</param>
        /// <param name="systemId">The base URI of the schema for <c>include</c>, or <see langword="null"/>.</param>
        /// <returns>A new builder.</returns>
        public static SchematronValidatorBuilder ForSchematron(
            ReadOnlySequence<char> schema,
            string? systemId = null
        ) => new(SchemaSourceHolder.FromChars(in schema, systemId));
    }
}
