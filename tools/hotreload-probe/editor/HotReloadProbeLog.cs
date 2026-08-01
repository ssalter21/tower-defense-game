// Throwaway -- part of tools/hotreload-probe/. Delete with `probe.ps1 -Remove`.
//
// Prints the plug-in's build stamp on every domain reload. Rebuild the probe
// DLL with `probe.ps1 -Bump`, alt-tab to Unity, and watch the Console: the
// gap between the rebuild and a new stamp appearing IS the answer issue #5
// wants -- whether the edit/rebuild/see-it loop is 3 seconds or 40.
//
// The stamp is read by reflection rather than by a direct type reference, on
// purpose. A direct reference would turn "Unity did not load the plug-in"
// into a red compile error, which is a confusing way to learn something.
// This way that outcome is itself a legible result, printed in the Console.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HotReloadProbeLog
{
    private const string TypeName = "HotReloadProbe.Probe, HotReloadProbe";

    static HotReloadProbeLog()
    {
        Report("domain reload");
    }

    [MenuItem("Tools/Hot-reload probe/Print stamp now")]
    private static void PrintStamp()
    {
        Report("manual check");
    }

    private static void Report(string trigger)
    {
        var now = DateTime.Now.ToString("HH:mm:ss.fff");
        var stamp = ReadStamp(out var detail);

        if (stamp == null)
        {
            Debug.LogWarning(
                $"[hot-reload probe] {now} ({trigger}) -- plug-in NOT loaded. {detail}\n" +
                "That is a real result, not a failure of the probe: it means Unity has not " +
                "picked up client/Packages/com.ssalter.hotreloadprobe/Runtime/HotReloadProbe.dll. " +
                "Try Assets > Refresh (Ctrl+R); if that does not do it, note that a full Editor " +
                "restart was required and record it on issue #10.");
            return;
        }

        Debug.Log($"[hot-reload probe] {now} ({trigger}) -- plug-in stamp: {stamp}");
    }

    private static string ReadStamp(out string detail)
    {
        detail = null;

        var type = Type.GetType(TypeName, throwOnError: false);
        if (type == null)
        {
            // Fall back to a scan, in case the assembly loaded under a name
            // the qualified lookup does not resolve.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name != "HotReloadProbe") continue;
                type = asm.GetType("HotReloadProbe.Probe");
                break;
            }
        }

        if (type == null)
        {
            detail = "Assembly 'HotReloadProbe' is not in the current AppDomain.";
            return null;
        }

        var field = type.GetField("Stamp", BindingFlags.Public | BindingFlags.Static);
        if (field == null)
        {
            detail = "Found HotReloadProbe.Probe but it has no public static Stamp field.";
            return null;
        }

        return field.GetValue(null) as string;
    }
}
