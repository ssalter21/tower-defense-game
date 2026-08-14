using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace View.Editor
{
    /// <summary>
    /// Generates the one <see cref="PanelSettings"/> asset in this project: the
    /// base every runtime panel is cloned from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why an asset exists at all, in a project whose chrome is built in code,
    /// is <see cref="RuntimePanel.Base"/>'s to explain, and the measurement
    /// behind it is in
    /// <c>docs/research/a-player-build-measures-no-text-without-a-panelsettings-asset.md</c>.
    /// What is written here is a bare <see cref="PanelSettings"/> plus the
    /// theme, which the editor stamps the text engine's ICU data onto on its
    /// way to disk.
    /// </para>
    /// <para>
    /// The write is read back and the ICU reference asserted, because an asset
    /// that exists, loads and carries nothing is exactly the file this one
    /// replaces and looks identical from the outside.
    /// <c>Tests.EditMode.GeneratedProjectFilesTests</c> then holds every other
    /// value on it to a fresh instance's default, since the clone carries those
    /// too.
    /// </para>
    /// <para>
    /// Runs from a shell — <c>tools/build-panel-settings.ps1</c>, which is
    /// <c>-batchmode -executeMethod</c> and needs no editor session, no bridge
    /// and nobody at a keyboard.
    /// </para>
    /// </remarks>
    public static class PanelSettingsAsset
    {
        /// <summary>Where the asset lands.</summary>
        public const string AssetPath =
            "Assets/Resources/" + RuntimePanel.SettingsResourcePath + ".asset";

        /// <summary>
        /// The field the editor puts the text engine's ICU data in. Named here
        /// because it is what the asset is for and nothing public reports it.
        /// </summary>
        private const string ICUDataField = "m_ICUDataAsset";

        [MenuItem("Tools/Rewrite the panel settings")]
        public static void Run()
        {
            EnsureFolder(Path.GetDirectoryName(AssetPath).Replace('\\', '/'));

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = RuntimePanel.Theme();

            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

            var written = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetPath);

            if (written == null)
            {
                throw new InvalidOperationException(AssetPath + " was written and does not read back.");
            }

            SerializedProperty reference = new SerializedObject(written).FindProperty(ICUDataField);

            if (reference == null)
            {
                throw new InvalidOperationException(
                    "PanelSettings has no " + ICUDataField + " field in this editor version — "
                    + "the mechanism this asset exists for has moved.");
            }

            if (reference.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    AssetPath + " serialized with no ICU data on it. A build carrying it would still "
                    + "measure every string as nothing.");
            }

            Debug.Log(
                "PanelSettingsAsset: wrote " + AssetPath
                + " carrying " + reference.objectReferenceValue.name);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            AssetDatabase.CreateFolder(
                Path.GetDirectoryName(folder).Replace('\\', '/'),
                Path.GetFileName(folder));
        }
    }
}
