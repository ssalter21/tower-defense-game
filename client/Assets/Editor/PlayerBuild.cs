using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Cuts the double-clickable build: the thing somebody who has never
    /// cloned this repository can unzip and run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the artefact the slice ends in.</b> Every other way of
    /// looking at the match so far needs the project — an editor session, a
    /// licence, a test runner, or a batchmode capture. The sit-down in
    /// <c>docs/sit-down.md</c> is run against a build instead, because a
    /// walking skeleton that only walks inside its own editor has not crossed
    /// the boundary it exists to cross, and row 12 of that checklist is
    /// precisely the failure this catches: a missing assembly or a runtime
    /// prompt on a machine that never had the project.
    /// </para>
    /// <para>
    /// <b>A failed build has to end the process badly.</b> Unity's own
    /// <c>BuildPipeline.BuildPlayer</c> reports failure by handing back a
    /// report, not by throwing — and <c>-batchmode -quit</c> exits zero for a
    /// build that produced nothing at all. An unattended caller would file that
    /// under success and go looking for an executable nobody wrote, so the
    /// report is read here and anything short of
    /// <see cref="BuildResult.Succeeded"/> is thrown with the counts in it.
    /// </para>
    /// <para>
    /// <b>Nothing here chooses what goes in the build.</b> The scene list is
    /// whatever <c>MatchSceneBuilder</c> put in the project's build settings —
    /// one scene, at index zero — rather than a second list assembled here that
    /// could name a scene the project does not ship. The content comes along on
    /// its own: <c>Assets/StreamingAssets/</c> is copied into the player
    /// verbatim by the engine, which is the entire reason the match record and
    /// the content files live there.
    /// </para>
    /// <para>
    /// Runs from a shell — <c>tools/build-player.ps1</c> — with no editor
    /// session, no bridge and nobody at a keyboard.
    /// </para>
    /// </remarks>
    public static class PlayerBuild
    {
        /// <summary>Where the player is written.</summary>
        public const string OutDirArgument = "-playerBuildOut";

        /// <summary>
        /// The folder builds land in, relative to the Unity project. Ignored by
        /// <c>client/.gitignore</c>: a player is a hundred megabytes of engine
        /// output that can be made again from the commit, which is the opposite
        /// of the committed plug-in beside it.
        /// </summary>
        public const string DefaultOutDir = "Builds/Windows";

        /// <summary>
        /// The executable's name, and therefore what a human double-clicks.
        /// </summary>
        public const string ExecutableName = "TowerDefense.exe";

        [MenuItem("Tools/Build the player")]
        public static void BuildDefault() => Run();

        public static void Run()
        {
            string outDir = BatchArguments.Value(OutDirArgument)
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultOutDir));

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidDataException(
                    "The project's build settings name no enabled scene, so this build would open on "
                    + "nothing. The scene is generated: run tools/build-match-scene.ps1 and commit what "
                    + "it writes.");
            }

            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,

                // No development build, no profiler, no script debugging. What
                // is being looked at is the thing that ships; a development
                // player draws a different set of guarantees about stripping,
                // and stripping is exactly what row 2 of the checklist is
                // about.
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "The player build "
                    + summary.result.ToString().ToLowerInvariant()
                    + " with "
                    + summary.totalErrors.ToString(CultureInfo.InvariantCulture)
                    + " errors and "
                    + summary.totalWarnings.ToString(CultureInfo.InvariantCulture)
                    + " warnings. "
                    + FirstError(report)
                    + " Nothing double-clickable was produced.");
            }

            Debug.Log(
                "PlayerBuild: wrote "
                + summary.outputPath
                + " ("
                + (summary.totalSize / (1024 * 1024)).ToString(CultureInfo.InvariantCulture)
                + " MB, "
                + summary.totalWarnings.ToString(CultureInfo.InvariantCulture)
                + " warnings) from "
                + scenes.Length.ToString(CultureInfo.InvariantCulture)
                + " scene(s): "
                + string.Join(", ", scenes));
        }

        /// <summary>
        /// The first error the build reported, so the exception carries a
        /// reason rather than only a count. A build log is thousands of lines
        /// and the one line that matters is not reliably near the end of it.
        /// </summary>
        private static string FirstError(BuildReport report)
        {
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        return "First error, during '" + step.name + "': " + message.content;
                    }
                }
            }

            return "The report carried no error message.";
        }

    }
}
