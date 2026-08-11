using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatBalanceRunRecorderMenu
{
    private const string MenuPath =
        "Tools/Tower Nexus/Create Combat Balance Run Recorder";

    [MenuItem(MenuPath)]
    private static void CreateOrSelectRecorder()
    {
        CombatBalanceRunRecorder existingRecorder =
            Object.FindFirstObjectByType<CombatBalanceRunRecorder>(
                FindObjectsInactive.Include);

        if (existingRecorder != null)
        {
            Selection.activeGameObject = existingRecorder.gameObject;
            EditorGUIUtility.PingObject(existingRecorder.gameObject);
            Debug.Log(
                "Combat Balance Run Recorder already exists in the active " +
                "Scene. Selected the existing GameObject.",
                existingRecorder);
            return;
        }

        GameObject recorderObject =
            GameObject.Find("CombatBalanceRunRecorder");
        bool createdRecorderObject = recorderObject == null;

        if (createdRecorderObject)
        {
            recorderObject =
                new GameObject("CombatBalanceRunRecorder");
            recorderObject.hideFlags = HideFlags.DontSaveInBuild;
            Undo.RegisterCreatedObjectUndo(
                recorderObject,
                "Create Combat Balance Run Recorder");
        }

        CombatBalanceRunRecorder recorder =
            Undo.AddComponent<CombatBalanceRunRecorder>(recorderObject);

        if (recorder == null)
        {
            Debug.LogError(
                "Failed to attach CombatBalanceRunRecorder to the Scene " +
                "GameObject. Check Unity compilation errors before retrying.",
                recorderObject);
            return;
        }

        Selection.activeGameObject = recorderObject;
        EditorGUIUtility.PingObject(recorderObject);

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(recorderObject.scene);
        }

        Debug.Log(
            createdRecorderObject
                ? "Created a Scene GameObject with CombatBalanceRunRecorder. " +
                  "Runtime references resolve automatically when the " +
                  "component is enabled."
                : "Attached CombatBalanceRunRecorder to the existing " +
                  "CombatBalanceRunRecorder GameObject.",
            recorder);
    }
}
