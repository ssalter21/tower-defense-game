using System.Reflection.Metadata;

namespace Sim.Tests.Scan;

/// <summary>Metadata handles to the names the ban table is written in.</summary>
internal static class MetadataNames
{
    /// <summary>
    /// A type reference's full name. Nested types are joined with <c>+</c>,
    /// matching the way the runtime spells them, and generic types keep their
    /// arity suffix, so <c>Dictionary&lt;K,V&gt;</c> is
    /// <c>System.Collections.Generic.Dictionary`2</c>.
    /// </summary>
    public static string FullName(MetadataReader reader, TypeReferenceHandle handle)
    {
        TypeReference reference = reader.GetTypeReference(handle);
        string name = reader.GetString(reference.Name);

        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return FullName(reader, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        }

        string ns = reference.Namespace.IsNil ? string.Empty : reader.GetString(reference.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    /// <summary>A type definition's full name, spelled the same way.</summary>
    public static string FullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition definition = reader.GetTypeDefinition(handle);
        string name = reader.GetString(definition.Name);

        TypeDefinitionHandle declaring = definition.GetDeclaringType();
        if (!declaring.IsNil)
        {
            return FullName(reader, declaring) + "+" + name;
        }

        string ns = definition.Namespace.IsNil ? string.Empty : reader.GetString(definition.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    /// <summary>The type a member reference hangs off, however that parent is encoded.</summary>
    public static string DeclaringTypeName(MetadataReader reader, MemberReference reference, SignatureProbe probe)
    {
        EntityHandle parent = reference.Parent;
        return parent.Kind switch
        {
            HandleKind.TypeReference => FullName(reader, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => FullName(reader, (TypeDefinitionHandle)parent),
            HandleKind.TypeSpecification =>
                reader.GetTypeSpecification((TypeSpecificationHandle)parent).DecodeSignature(probe, null).Name,
            HandleKind.MethodDefinition =>
                FullName(reader, reader.GetMethodDefinition((MethodDefinitionHandle)parent).GetDeclaringType()),
            _ => "<unknown>",
        };
    }

    /// <summary>The name a method definition is reported under in a finding.</summary>
    public static string MemberSite(MetadataReader reader, MethodDefinitionHandle handle)
    {
        MethodDefinition method = reader.GetMethodDefinition(handle);
        return FullName(reader, method.GetDeclaringType()) + "::" + reader.GetString(method.Name);
    }
}
