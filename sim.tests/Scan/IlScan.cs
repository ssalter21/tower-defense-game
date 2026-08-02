using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Sim.Tests.Scan;

/// <summary>
/// Reads a compiled assembly and reports every banned construct in it.
/// </summary>
/// <remarks>
/// <para>
/// The subject is the artefact, not the source. A rule enforced on source is
/// enforced on the text someone last read; a rule enforced on the image is
/// enforced on what actually ships, and the two stop agreeing the first time a
/// rebuild is forgotten. The build gate therefore runs this over the committed
/// assembly <b>and</b> over a fresh build of the same sources, so neither a
/// stale image nor a dirty source tree can be the one that goes green.
/// </para>
/// <para>
/// A test that cannot fail is not a test, so every clause below has a
/// deliberate violation waiting for it in sim.poison and the poison suite
/// watches each one fire.
/// </para>
/// </remarks>
public static class IlScan
{
    /// <summary>Opcodes that can only appear if floating point is being computed.</summary>
    private static readonly ImmutableHashSet<short> FloatOpCodes = ImmutableHashSet.Create(
        OpCodes.Ldc_R4.Value,
        OpCodes.Ldc_R8.Value,
        OpCodes.Ldind_R4.Value,
        OpCodes.Ldind_R8.Value,
        OpCodes.Stind_R4.Value,
        OpCodes.Stind_R8.Value,
        OpCodes.Conv_R4.Value,
        OpCodes.Conv_R8.Value,
        OpCodes.Conv_R_Un.Value,
        OpCodes.Ldelem_R4.Value,
        OpCodes.Ldelem_R8.Value,
        OpCodes.Stelem_R4.Value,
        OpCodes.Stelem_R8.Value);

    /// <summary>Every banned construct in the assembly at <paramref name="assemblyPath"/>.</summary>
    public static IReadOnlyList<BanFinding> Scan(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Nothing to scan: {assemblyPath} does not exist. If this is the committed assembly, the "
                + "plug-in is missing from the repository; if it is a fresh build, the build did not run.",
                assemblyPath);
        }

        var findings = new List<BanFinding>();
        var probe = new SignatureProbe();

        using FileStream stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        // Pass one walks every method body: it is where the two float clauses
        // that only exist in compiled form are found, and it is also what lets
        // pass two say WHICH of this assembly's methods reached for a banned
        // type. A reference-table entry on its own says only that the assembly
        // touched something, and "somewhere in Sim.dll" is not a report anyone
        // can act on.
        var usage = new UsageMap();
        ScanMethodBodies(peReader, reader, probe, findings, usage);

        ScanDeclaredSignatures(reader, probe, findings);
        ScanReferences(reader, probe, findings, usage);

        return findings;
    }

    /// <summary>
    /// Rows 2 to 7: every type and member the assembly reaches for. A banned
    /// type appears in the type-reference table the moment anything touches
    /// it, and a banned member on an otherwise allowed type appears in the
    /// member-reference table.
    /// </summary>
    private static void ScanReferences(
        MetadataReader reader,
        SignatureProbe probe,
        List<BanFinding> findings,
        UsageMap usage)
    {
        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            string name = MetadataNames.FullName(reader, handle);

            if (BanTable.BannedTypes.TryGetValue(name, out BanRow row))
            {
                findings.Add(new BanFinding(
                    row,
                    BanTable.ClauseBannedType,
                    usage.SiteFor(name),
                    $"references the banned type {name}"));
                continue;
            }

            foreach (KeyValuePair<string, BanRow> prefix in BanTable.BannedNamespacePrefixes)
            {
                if (name.StartsWith(prefix.Key, StringComparison.Ordinal))
                {
                    findings.Add(new BanFinding(
                        prefix.Value,
                        BanTable.ClauseBannedType,
                        usage.SiteFor(name),
                        $"references {name}, under the banned namespace {prefix.Key}"));
                    break;
                }
            }
        }

        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            MemberReference reference = reader.GetMemberReference(handle);
            string key = MetadataNames.DeclaringTypeName(reader, reference, probe)
                + "::"
                + reader.GetString(reference.Name);

            if (BanTable.BannedMembers.TryGetValue(key, out BanRow row))
            {
                findings.Add(new BanFinding(
                    row,
                    BanTable.ClauseBannedMember,
                    usage.SiteFor(key),
                    $"calls the banned member {key}"));
            }
        }
    }

    /// <summary>
    /// Row 1, first clause: floats in the signatures this assembly declares --
    /// fields, methods, properties -- and in the members it references.
    /// </summary>
    private static void ScanDeclaredSignatures(MetadataReader reader, SignatureProbe probe, List<BanFinding> findings)
    {
        foreach (FieldDefinitionHandle handle in reader.FieldDefinitions)
        {
            FieldDefinition field = reader.GetFieldDefinition(handle);
            SigType type = field.DecodeSignature(probe, null);

            if (type.ContainsFloat)
            {
                findings.Add(new BanFinding(
                    BanRow.Floats,
                    BanTable.ClauseFloatSignature,
                    MetadataNames.FullName(reader, field.GetDeclaringType()) + "::" + reader.GetString(field.Name),
                    $"field is declared {type.Name}"));
            }
        }

        foreach (MethodDefinitionHandle handle in reader.MethodDefinitions)
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);

            if (ContainsFloat(method.DecodeSignature(probe, null), out string where))
            {
                findings.Add(new BanFinding(
                    BanRow.Floats,
                    BanTable.ClauseFloatSignature,
                    MetadataNames.MemberSite(reader, handle),
                    $"signature has {where}"));
            }
        }

        foreach (PropertyDefinitionHandle handle in reader.PropertyDefinitions)
        {
            PropertyDefinition property = reader.GetPropertyDefinition(handle);

            if (ContainsFloat(property.DecodeSignature(probe, null), out string where))
            {
                findings.Add(new BanFinding(
                    BanRow.Floats,
                    BanTable.ClauseFloatSignature,
                    reader.GetString(property.Name),
                    $"property signature has {where}"));
            }
        }

        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            MemberReference reference = reader.GetMemberReference(handle);

            bool containsFloat = reference.GetKind() == MemberReferenceKind.Field
                ? reference.DecodeFieldSignature(probe, null).ContainsFloat
                : ContainsFloat(reference.DecodeMethodSignature(probe, null), out _);

            if (containsFloat)
            {
                findings.Add(new BanFinding(
                    BanRow.Floats,
                    BanTable.ClauseFloatSignature,
                    MetadataNames.DeclaringTypeName(reader, reference, probe) + "::" + reader.GetString(reference.Name),
                    "the referenced member's own signature is floating point"));
            }
        }
    }

    /// <summary>
    /// Row 1, clauses two and three: floats in local slots and floats in the
    /// instruction stream. These are the two no source-level tool can see, and
    /// between them they are what makes scanning the artefact worth doing.
    /// Also builds the usage map that gives every other row a real site.
    /// </summary>
    private static void ScanMethodBodies(
        PEReader peReader,
        MetadataReader reader,
        SignatureProbe probe,
        List<BanFinding> findings,
        UsageMap usage)
    {
        foreach (MethodDefinitionHandle handle in reader.MethodDefinitions)
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            MethodBodyBlock body = peReader.GetMethodBody(method.RelativeVirtualAddress);
            string site = MetadataNames.MemberSite(reader, handle);

            if (!body.LocalSignature.IsNil)
            {
                ImmutableArray<SigType> locals = reader
                    .GetStandaloneSignature(body.LocalSignature)
                    .DecodeLocalSignature(probe, null);

                for (int slot = 0; slot < locals.Length; slot++)
                {
                    if (locals[slot].ContainsFloat)
                    {
                        findings.Add(new BanFinding(
                            BanRow.Floats,
                            BanTable.ClauseFloatLocal,
                            site,
                            $"local slot {slot} is {locals[slot].Name}"));
                    }
                }
            }

            byte[] il = body.GetILBytes() ?? Array.Empty<byte>();

            foreach ((int offset, OpCode opCode) in IlWalker.Walk(il))
            {
                if (FloatOpCodes.Contains(opCode.Value))
                {
                    findings.Add(new BanFinding(
                        BanRow.Floats,
                        BanTable.ClauseFloatInstruction,
                        site,
                        $"IL offset {offset} is {opCode.Name}"));
                }

                if (TakesMetadataToken(opCode.OperandType))
                {
                    int operandStart = offset + opCode.Size;
                    usage.Record(reader, probe, BitConverter.ToInt32(il, operandStart), site);
                }
            }
        }
    }

    private static bool TakesMetadataToken(OperandType operandType) =>
        operandType is OperandType.InlineMethod
            or OperandType.InlineField
            or OperandType.InlineType
            or OperandType.InlineTok;

    private static bool ContainsFloat(MethodSignature<SigType> signature, out string where)
    {
        if (signature.ReturnType.ContainsFloat)
        {
            where = "return type " + signature.ReturnType.Name;
            return true;
        }

        for (int i = 0; i < signature.ParameterTypes.Length; i++)
        {
            if (signature.ParameterTypes[i].ContainsFloat)
            {
                where = $"parameter {i} of type {signature.ParameterTypes[i].Name}";
                return true;
            }
        }

        where = string.Empty;
        return false;
    }

    /// <summary>
    /// Which of the assembly's own methods reached for which external name.
    /// Built from instruction operands, because that is the only place the
    /// answer exists: the reference tables record that something is used, not
    /// by whom.
    /// </summary>
    private sealed class UsageMap
    {
        private readonly Dictionary<string, SortedSet<string>> _sites = new(StringComparer.Ordinal);

        public void Record(MetadataReader reader, SignatureProbe probe, int token, string site)
        {
            EntityHandle handle = MetadataTokens.EntityHandle(token);

            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                    Add(MetadataNames.FullName(reader, (TypeReferenceHandle)handle), site);
                    break;

                case HandleKind.TypeSpecification:
                    Add(
                        reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(probe, null).Name,
                        site);
                    break;

                case HandleKind.MemberReference:
                    RecordMemberReference(reader, probe, (MemberReferenceHandle)handle, site);
                    break;

                case HandleKind.MethodSpecification:
                    // A call to a generic method -- Array.Sort<T> is the one
                    // that matters here -- goes through a MethodSpec, and the
                    // member it instantiates is one hop further in.
                    EntityHandle instantiated = reader
                        .GetMethodSpecification((MethodSpecificationHandle)handle)
                        .Method;

                    if (instantiated.Kind == HandleKind.MemberReference)
                    {
                        RecordMemberReference(reader, probe, (MemberReferenceHandle)instantiated, site);
                    }

                    break;
            }
        }

        public string SiteFor(string name) =>
            _sites.TryGetValue(name, out SortedSet<string>? sites)
                ? string.Join(", ", sites)
                : "(no instruction reaches it: an attribute, a base type or a signature)";

        private void RecordMemberReference(
            MetadataReader reader,
            SignatureProbe probe,
            MemberReferenceHandle handle,
            string site)
        {
            MemberReference reference = reader.GetMemberReference(handle);
            string declaringType = MetadataNames.DeclaringTypeName(reader, reference, probe);

            Add(declaringType, site);
            Add(declaringType + "::" + reader.GetString(reference.Name), site);
        }

        private void Add(string name, string site)
        {
            if (!_sites.TryGetValue(name, out SortedSet<string>? sites))
            {
                sites = new SortedSet<string>(StringComparer.Ordinal);
                _sites[name] = sites;
            }

            sites.Add(site);
        }
    }
}
