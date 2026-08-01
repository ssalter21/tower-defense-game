using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Sim.Tests.Scan;

/// <summary>A decoded signature element: what it is called, and whether a float got in.</summary>
public sealed class SigType
{
    public SigType(string name, bool containsFloat)
    {
        Name = name;
        ContainsFloat = containsFloat;
    }

    /// <summary>Best-effort metadata name. For a generic instantiation this is the open type.</summary>
    public string Name { get; }

    /// <summary>True if <c>float</c> or <c>double</c> appears anywhere in this element.</summary>
    public bool ContainsFloat { get; }
}

/// <summary>
/// Decodes metadata signatures far enough to answer the only two questions the
/// scan asks of them: does a float appear anywhere inside, and what is the
/// declaring type called.
/// </summary>
/// <remarks>
/// Signature decoding is the only way to see floating point in metadata.
/// <c>float</c> and <c>double</c> are primitive element types encoded inline
/// as <c>ELEMENT_TYPE_R4</c> and <c>ELEMENT_TYPE_R8</c>; unlike
/// <c>Dictionary</c> or <c>Math</c> they never appear in the type-reference
/// table, so a scan that only walks references sees nothing at all. That is
/// precisely the hole the analyzer this project does not use would have left.
/// </remarks>
public sealed class SignatureProbe : ISignatureTypeProvider<SigType, object?>
{
    private static readonly SigType Float = new("System.Single", true);

    private static readonly SigType Double = new("System.Double", true);

    public SigType GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Single => Float,
        PrimitiveTypeCode.Double => Double,
        _ => new SigType("System." + typeCode, false),
    };

    public SigType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        new(MetadataNames.FullName(reader, handle), false);

    public SigType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        new(MetadataNames.FullName(reader, handle), false);

    public SigType GetTypeFromSpecification(
        MetadataReader reader,
        object? genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public SigType GetSZArrayType(SigType elementType) =>
        new(elementType.Name + "[]", elementType.ContainsFloat);

    public SigType GetArrayType(SigType elementType, ArrayShape shape) =>
        new(elementType.Name + "[,]", elementType.ContainsFloat);

    public SigType GetByReferenceType(SigType elementType) =>
        new(elementType.Name + "&", elementType.ContainsFloat);

    public SigType GetPointerType(SigType elementType) =>
        new(elementType.Name + "*", elementType.ContainsFloat);

    public SigType GetGenericInstantiation(SigType genericType, ImmutableArray<SigType> typeArguments)
    {
        bool containsFloat = genericType.ContainsFloat;
        foreach (SigType argument in typeArguments)
        {
            containsFloat |= argument.ContainsFloat;
        }

        // The open type is what the member-reference check wants to compare
        // against: a call to List<int>.Sort has a TypeSpec parent, and the ban
        // is written against List`1.
        return new SigType(genericType.Name, containsFloat);
    }

    public SigType GetGenericMethodParameter(object? genericContext, int index) => new("!!" + index, false);

    public SigType GetGenericTypeParameter(object? genericContext, int index) => new("!" + index, false);

    public SigType GetModifiedType(SigType modifier, SigType unmodifiedType, bool isRequired) => unmodifiedType;

    public SigType GetPinnedType(SigType elementType) => elementType;

    public SigType GetFunctionPointerType(MethodSignature<SigType> signature)
    {
        bool containsFloat = signature.ReturnType.ContainsFloat;
        foreach (SigType parameter in signature.ParameterTypes)
        {
            containsFloat |= parameter.ContainsFloat;
        }

        return new SigType("method*", containsFloat);
    }
}
