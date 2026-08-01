using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Spikes.Playables.Editor
{
    /// <summary>
    /// Answers the asset-import half of the Playables ticket without any mouse work:
    /// given FBX files dropped under Assets/Art/, report what AnimationClips Unity
    /// actually produced from them and whether those clips carry root motion.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -quit -projectPath client \
    ///             -executeMethod Spikes.Playables.Editor.FbxClipReport.Run
    /// </summary>
    public static class FbxClipReport
    {
        private const string SearchRoot = "Assets/Art";

        public static void Run()
        {
            var report = new StringBuilder();
            report.AppendLine("# FBX clip report");
            report.AppendLine();

            if (!AssetDatabase.IsValidFolder(SearchRoot))
            {
                report.AppendLine($"No `{SearchRoot}` folder — nothing imported yet.");
                Emit(report);
                return;
            }

            var models = AssetDatabase.FindAssets("t:Model", new[] { SearchRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .ToArray();

            report.AppendLine($"Found {models.Length} model asset(s) under `{SearchRoot}`.");
            report.AppendLine();

            foreach (var path in models)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                report.AppendLine($"## {path}");
                report.AppendLine();
                report.AppendLine($"- size on disk: {new FileInfo(path).Length:n0} bytes");

                if (importer != null)
                {
                    report.AppendLine($"- animationType: **{importer.animationType}**");
                    report.AppendLine($"- importAnimation: {importer.importAnimation}");
                    report.AppendLine($"- avatarSetup: {importer.avatarSetup}");
                }
                else
                {
                    report.AppendLine("- (no ModelImporter — not a model?)");
                }

                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                    .OrderBy(c => c.name)
                    .ToArray();

                report.AppendLine($"- AnimationClips produced: **{clips.Length}**");
                report.AppendLine();

                if (clips.Length > 0)
                {
                    report.AppendLine("| clip | length | fps | rootCurves | motionCurves | genericRoot | humanMotion | looping |");
                    report.AppendLine("|---|---|---|---|---|---|---|---|");
                    foreach (var c in clips)
                    {
                        report.AppendLine(
                            $"| `{c.name}` | {c.length:0.###}s | {c.frameRate:0} | {Mark(c.hasRootCurves)} | " +
                            $"{Mark(c.hasMotionCurves)} | {Mark(c.hasGenericRootTransform)} | " +
                            $"{Mark(c.humanMotion)} | {Mark(c.isLooping)} |");
                    }
                    report.AppendLine();
                }
            }

            Emit(report);
        }

        private static string Mark(bool b) => b ? "**yes**" : "no";

        private static void Emit(StringBuilder report)
        {
            var text = report.ToString();
            var outPath = Path.Combine(Directory.GetCurrentDirectory(), "fbx-clip-report.md");
            File.WriteAllText(outPath, text);
            Debug.Log("FbxClipReport written to " + outPath + "\n\n" + text);
        }
    }
}
