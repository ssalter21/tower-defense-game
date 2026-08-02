using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Sim.Tests.Scan;

namespace Sim.Tests;

/// <summary>
/// Properties of the image in the repository, as opposed to properties of the
/// code that produced it.
/// </summary>
/// <remarks>
/// Everything here is read out of the file's metadata rather than by loading
/// the assembly. Loading would be shorter and would also be wrong: the runtime
/// resolves by identity, so <c>Assembly.LoadFrom</c> on the fresh build would
/// hand back the copy already loaded beside the tests and every assertion
/// about the fresh build would silently be an assertion about the committed
/// one. That is precisely the kind of check that passes for the wrong reason.
/// </remarks>
public class CommittedAssemblyTests
{
    /// <summary><c>DebuggableAttribute.DebuggingModes.DisableOptimizations</c>.</summary>
    private const int DisableOptimizations = 0x100;

    [Fact]
    public void The_assembly_and_its_symbols_are_both_in_the_repository()
    {
        // Both are committed, and the reason is mechanical rather than
        // aspirational: Sim.dll.meta carries the Auto Reference off setting,
        // and Unity deletes orphaned .meta files. An ignored assembly comes
        // back with auto-reference ON at every fresh clone, dissolving the
        // boundary the setting exists to protect. The symbols are committed
        // because the debugger can only step from view code into simulation
        // code if the .pdb is on disk beside the plug-in.
        Assert.True(File.Exists(RepoLayout.CommittedAssembly), RepoLayout.CommittedAssembly + " is missing.");
        Assert.True(File.Exists(RepoLayout.CommittedSymbols), RepoLayout.CommittedSymbols + " is missing.");
    }

    [Fact]
    public void The_plug_in_metadata_turns_auto_reference_off()
    {
        foreach (string artefact in new[] { RepoLayout.CommittedAssembly, RepoLayout.CommittedSymbols })
        {
            string meta = artefact + ".meta";
            Assert.True(
                File.Exists(meta),
                meta + " is missing, so a fresh clone would import this artefact with default settings.");
        }

        // Unity spells "Auto Reference off" as isExplicitlyReferenced: 1.
        // Measured with the editor in batch mode: with this at 0, Sim.dll
        // appears as a <Reference> in the generated Assembly-CSharp.csproj;
        // with it at 1, it does not.
        Assert.Contains(
            "isExplicitlyReferenced: 1",
            File.ReadAllText(RepoLayout.CommittedAssembly + ".meta"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_committed_configuration_is_debug()
    {
        // Load-bearing, not cosmetic. Debug.Assert and everything else marked
        // [Conditional] leaves no residue in a Release image, so the
        // conditional-diagnostics row of the IL scan is a clause that can only
        // fire against a Debug build. Committing Release would silently turn
        // that row into a check that cannot fail, and it would also mean every
        // debugging session left the working tree dirty.
        Assert.True(IsDebugBuild(RepoLayout.CommittedAssembly), "The committed Sim.dll is not a Debug build.");
        Assert.True(IsDebugBuild(RepoLayout.FreshlyBuiltAssembly), "A fresh build of sim/ is not a Debug build.");
    }

    [Fact]
    public void Nothing_in_the_assembly_can_reach_the_engine()
    {
        // The strong form of the sim/view boundary: UnityEngine is not
        // "unreferenced here by agreement", it is unresolvable, because the
        // assembly was compiled against netstandard2.1 reference assemblies by
        // a toolchain that has never heard of it. This asserts the resulting
        // fact about the artefact.
        using FileStream stream = File.OpenRead(RepoLayout.CommittedAssembly);
        using var peReader = new PEReader(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        string[] referenced = reader.AssemblyReferences
            .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
            .ToArray();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("netstandard", referenced);
    }

    [Fact]
    public void The_assembly_targets_the_profile_the_engine_and_the_command_line_both_accept()
    {
        Assert.Equal(".NETStandard,Version=v2.1", TargetFramework(RepoLayout.CommittedAssembly));
        Assert.Equal(".NETStandard,Version=v2.1", TargetFramework(RepoLayout.FreshlyBuiltAssembly));
    }

    private static bool IsDebugBuild(string assemblyPath)
    {
        byte[] blob = AssemblyAttribute(assemblyPath, "System.Diagnostics.DebuggableAttribute")
            ?? throw new InvalidOperationException(assemblyPath + " carries no DebuggableAttribute at all.");

        // Blob layout: a 2-byte prolog, then the DebuggingModes argument.
        int modes = BitConverter.ToInt32(blob, 2);
        return (modes & DisableOptimizations) != 0;
    }

    private static string TargetFramework(string assemblyPath)
    {
        byte[] blob = AssemblyAttribute(assemblyPath, "System.Runtime.Versioning.TargetFrameworkAttribute")
            ?? throw new InvalidOperationException(assemblyPath + " carries no TargetFrameworkAttribute.");

        // Blob layout: a 2-byte prolog, then a compressed length and UTF-8 bytes.
        int index = 2;
        int length = ReadCompressedInteger(blob, ref index);
        return Encoding.UTF8.GetString(blob, index, length);
    }

    /// <summary>ECMA-335 II.23.2 compressed unsigned integer.</summary>
    private static int ReadCompressedInteger(byte[] blob, ref int index)
    {
        byte first = blob[index++];

        if ((first & 0x80) == 0)
        {
            return first;
        }

        if ((first & 0xC0) == 0x80)
        {
            return ((first & 0x3F) << 8) | blob[index++];
        }

        int value = ((first & 0x1F) << 24) | (blob[index] << 16) | (blob[index + 1] << 8) | blob[index + 2];
        index += 3;
        return value;
    }

    private static byte[]? AssemblyAttribute(string assemblyPath, string attributeTypeName)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);

            if (AttributeTypeName(reader, attribute) == attributeTypeName)
            {
                return reader.GetBlobBytes(attribute.Value);
            }
        }

        return null;
    }

    private static string AttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference reference = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                return reference.Parent.Kind == HandleKind.TypeReference
                    ? MetadataNames.FullName(reader, (TypeReferenceHandle)reference.Parent)
                    : "<unknown>";

            case HandleKind.MethodDefinition:
                MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                return MetadataNames.FullName(reader, method.GetDeclaringType());

            default:
                return "<unknown>";
        }
    }
}
