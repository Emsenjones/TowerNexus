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

internal static class CombatDamageAuthoringValidator
{
    [MenuItem("Tools/Tower Nexus/Validate Combat Damage Authoring")]
    private static void ValidateAllCombatDamageAuthoring()
    {
        int checkedAssetCount = 0;
        int invalidAssetCount = 0;

        ValidateScriptableObjects<EffectDefinition>(
            definition => definition.IsValid(),
            ref checkedAssetCount,
            ref invalidAssetCount);
        ValidateScriptableObjects<BuffDefinition>(
            definition => definition.IsValid(),
            ref checkedAssetCount,
            ref invalidAssetCount);
        ValidateScriptableObjects<TowerUpgradeDefinition>(
            definition => definition.IsValid(),
            ref checkedAssetCount,
            ref invalidAssetCount);
        ValidateScriptableObjects<TowerDefinition>(
            definition => definition.IsValid(),
            ref checkedAssetCount,
            ref invalidAssetCount);
        ValidateSpecializedEffectOwnerPrefabs(
            ref checkedAssetCount,
            ref invalidAssetCount);

        if (invalidAssetCount > 0)
        {
            Debug.LogError(
                $"Combat damage authoring validation failed: " +
                $"{invalidAssetCount} of {checkedAssetCount} checked assets are invalid.");
            return;
        }

        Debug.Log(
            $"Combat damage authoring validation passed: " +
            $"{checkedAssetCount} assets checked.");
    }

    private static void ValidateScriptableObjects<T>(
        System.Func<T, bool> validate,
        ref int checkedAssetCount,
        ref int invalidAssetCount)
        where T : ScriptableObject
    {
        string[] assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            checkedAssetCount++;

            if (asset == null || !validate(asset))
            {
                invalidAssetCount++;
            }
        }
    }

    private static void ValidateSpecializedEffectOwnerPrefabs(
        ref int checkedAssetCount,
        ref int invalidAssetCount)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                continue;
            }

            MagicArcaneFieldBehaviour[] arcaneFields =
                prefab.GetComponentsInChildren<MagicArcaneFieldBehaviour>(true);

            for (int fieldIndex = 0;
                 fieldIndex < arcaneFields.Length;
                 fieldIndex++)
            {
                checkedAssetCount++;

                if (!arcaneFields[fieldIndex].IsAuthoredConfigurationValid())
                {
                    invalidAssetCount++;
                    Debug.LogWarning(
                        $"Combat damage authoring is invalid on prefab '{assetPath}': " +
                        "Magic Arcane Field requires a TowerScaled damage Effect.",
                        prefab);
                }
            }

            WindVortexBehaviour[] windVortices =
                prefab.GetComponentsInChildren<WindVortexBehaviour>(true);

            for (int vortexIndex = 0;
                 vortexIndex < windVortices.Length;
                 vortexIndex++)
            {
                checkedAssetCount++;

                if (!windVortices[vortexIndex].IsValid())
                {
                    invalidAssetCount++;
                    Debug.LogWarning(
                        $"Combat damage authoring is invalid on prefab '{assetPath}': " +
                        "Wind Vortex requires a FixedBuff damage Effect.",
                        prefab);
                }
            }
        }
    }
}
