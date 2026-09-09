#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
internal static class CombatReportExporter
{
    private static readonly HashSet<string> pendingPaths = new HashSet<string>();
    internal static void Enqueue(string path, string json, string terminal)
    {
        string requestedPath = path;
        int suffix = 1;
        while (pendingPaths.Contains(path) || File.Exists(path))
            path = Path.Combine(Path.GetDirectoryName(requestedPath), Path.GetFileNameWithoutExtension(requestedPath) + "_" + suffix++ + ".json");
        pendingPaths.Add(path);
        // Only detached strings survive here. Queued output survives recorder disable/destruction.
        EditorApplication.delayCall += () =>
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json, new UTF8Encoding(false));
                Debug.Log("Combat balance " + terminal + " report: " + path);
            }
            catch (Exception error) { Debug.LogWarning("Combat report export failed: " + error.Message); }
            finally { pendingPaths.Remove(path); }
        };
    }
}
#endif
