using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class TowerUpgradeDraftDebugWindow : EditorWindow
{
    private const string WindowMenuPath =
        "Tools/Tower Nexus/Tower Upgrade Draft Debug";

    private readonly List<TowerInstance> runtimeTowers =
        new List<TowerInstance>();

    private TowerInstance selectedTower;
    [SerializeField]
    private TowerUpgradeDefinition selectedUpgrade;
    private Vector2 scrollPosition;

    [MenuItem(WindowMenuPath)]
    private static void OpenWindow()
    {
        TowerUpgradeDraftDebugWindow window =
            GetWindow<TowerUpgradeDraftDebugWindow>(
                "Tower Upgrade Draft Debug");
        window.minSize = new Vector2(440f, 500f);
        window.RefreshContent();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshContent();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField(
            "Tower Upgrade Draft Debug",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Editor-only Task003 helper. It can prepare a runtime Tower " +
            "level, inject a real Pending Upgrade Draft, or apply an Upgrade " +
            "directly through TowerUpgradeSystem.",
            MessageType.Info);

        DrawRuntimeStatus();

        if (GUILayout.Button("Refresh Runtime Towers"))
        {
            RefreshContent();
        }

        EditorGUILayout.Space();
        DrawTowerSelection();
        DrawUpgradeSelection();
        DrawSelectionStatus();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawRuntimeStatus()
    {
        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Play Mode",
            Application.isPlaying ? "Active" : "Inactive");

        CombatBalanceRunRecorder recorder =
            FindFirstObjectByType<CombatBalanceRunRecorder>(
                FindObjectsInactive.Include);
        EditorGUILayout.LabelField(
            "Combat Balance Recorder",
            recorder != null ? "Detected" : "Missing");
    }

    private void DrawTowerSelection()
    {
        EditorGUILayout.LabelField("Target Tower", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode and deploy a Tower before selecting a " +
                "runtime target.",
                MessageType.Warning);
            return;
        }

        if (runtimeTowers.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No initialized runtime TowerInstance was found.",
                MessageType.Warning);
            return;
        }

        string[] labels = new string[runtimeTowers.Count];
        int selectedIndex = 0;

        for (int i = 0; i < runtimeTowers.Count; i++)
        {
            TowerInstance tower = runtimeTowers[i];
            labels[i] = GetTowerLabel(tower);

            if (tower == selectedTower)
            {
                selectedIndex = i;
            }
        }

        int nextIndex = EditorGUILayout.Popup(
            "Runtime Tower",
            selectedIndex,
            labels);
        TowerInstance nextTower = runtimeTowers[nextIndex];

        if (nextTower != selectedTower)
        {
            selectedTower = nextTower;
        }
    }

    private void DrawUpgradeSelection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Upgrade Definition",
            EditorStyles.boldLabel);

        selectedUpgrade =
            (TowerUpgradeDefinition)EditorGUILayout.ObjectField(
                "Upgrade Definition",
                selectedUpgrade,
                typeof(TowerUpgradeDefinition),
                false);

        EditorGUILayout.HelpBox(
            "Assign the TowerUpgradeDefinition asset to test. TowerFamily, " +
            "Required Tower Level, duplicate, package, and Elemental rules " +
            "remain enforced by TowerUpgradeSystem.",
            MessageType.None);
    }

    private void DrawSelectionStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selection Status", EditorStyles.boldLabel);

        if (selectedTower == null || selectedUpgrade == null)
        {
            EditorGUILayout.HelpBox(
                "A runtime Tower and compatible Upgrade are required.",
                MessageType.None);
            return;
        }

        EditorGUILayout.LabelField(
            "Current Tower Level",
            selectedTower.CurrentLevel.ToString());
        EditorGUILayout.LabelField(
            "Required Tower Level",
            selectedUpgrade.RequiredTowerLevel.ToString());
        EditorGUILayout.LabelField(
            "Upgrade Layer",
            selectedUpgrade.UpgradeLayer.ToString());
        EditorGUILayout.LabelField(
            "Applied Upgrades",
            GetAppliedUpgradeSummary(selectedTower));

        TowerUpgradeSystem upgradeSystem =
            FindFirstObjectByType<TowerUpgradeSystem>();

        if (upgradeSystem == null)
        {
            EditorGUILayout.HelpBox(
                "TowerUpgradeSystem is missing.",
                MessageType.Error);
            return;
        }

        if (upgradeSystem.CanApplyUpgrade(
                selectedTower,
                selectedUpgrade,
                out string failureReason))
        {
            EditorGUILayout.HelpBox(
                "The selected Upgrade is currently eligible.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(failureReason, MessageType.Warning);
    }

    private void DrawActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        bool hasSelection =
            Application.isPlaying &&
            selectedTower != null &&
            selectedUpgrade != null;

        using (new EditorGUI.DisabledScope(!hasSelection))
        {
            if (GUILayout.Button("Prepare Required Level (Debug)"))
            {
                PrepareRequiredLevel();
            }

            if (GUILayout.Button("Grant Pending Upgrade Draft"))
            {
                GrantPendingUpgradeDraft();
            }

            if (GUILayout.Button("Apply Upgrade Directly"))
            {
                ApplyUpgradeDirectly();
            }
        }

        EditorGUILayout.HelpBox(
            "Prepare Required Level intentionally bypasses Player Progress, " +
            "Draft cost, and Stage level caps. Grant and Direct Apply still " +
            "use TowerUpgradeSystem eligibility. Restart Play Mode between " +
            "independent balance runs; this tool does not remove Upgrades.",
            MessageType.None);
    }

    private void RefreshContent()
    {
        TowerInstance previousTower = selectedTower;
        runtimeTowers.Clear();

        if (Application.isPlaying)
        {
            TowerInstance[] discoveredTowers =
                FindObjectsByType<TowerInstance>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int i = 0; i < discoveredTowers.Length; i++)
            {
                TowerInstance tower = discoveredTowers[i];

                if (tower != null && tower.TowerDefinition != null)
                {
                    runtimeTowers.Add(tower);
                }
            }

            runtimeTowers.Sort(CompareTowers);
        }

        selectedTower = runtimeTowers.Contains(previousTower)
            ? previousTower
            : runtimeTowers.Count > 0
                ? runtimeTowers[0]
                : null;

        Repaint();
    }

    private void PrepareRequiredLevel()
    {
        if (!TryGetSelection(out TowerInstance tower, out TowerUpgradeDefinition upgrade))
        {
            return;
        }

        int requiredLevel = upgrade.RequiredTowerLevel;

        if (tower.CurrentLevel >= requiredLevel)
        {
            Debug.Log(
                $"Tower upgrade debug window did not change '{tower.name}': " +
                $"current level {tower.CurrentLevel} already satisfies " +
                $"Required Tower Level {requiredLevel}.",
                tower);
            return;
        }

        for (int nextLevel = tower.CurrentLevel + 1;
             nextLevel <= requiredLevel;
             nextLevel++)
        {
            if (!tower.TrySetLevel(nextLevel))
            {
                Debug.LogWarning(
                    $"Tower upgrade debug window could not prepare " +
                    $"'{tower.name}' for '{GetUpgradeName(upgrade)}': " +
                    $"Tower Level {nextLevel} is not configured.",
                    tower);
                return;
            }
        }

        TowerBehaviour towerBehaviour = tower.GetComponent<TowerBehaviour>();

        if (towerBehaviour != null)
        {
            towerBehaviour.RefreshTowerVisual();
        }

        Debug.Log(
            $"Tower upgrade debug window prepared '{tower.name}' at " +
            $"Tower Level {tower.CurrentLevel} for " +
            $"'{GetUpgradeName(upgrade)}'. No Player Progress or Draft was " +
            "consumed.",
            tower);
        Repaint();
    }

    private void GrantPendingUpgradeDraft()
    {
        if (!TryGetEligibleSelection(
                out TowerInstance tower,
                out TowerUpgradeDefinition upgrade,
                out _))
        {
            return;
        }

        BattleHUDUI battleHud = FindFirstObjectByType<BattleHUDUI>();

        if (battleHud == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant a Pending Draft: " +
                "BattleHUDUI is missing.");
            return;
        }

        if (battleHud.IsDraftOpen)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant a Pending Draft " +
                "while the normal Draft window is open.",
                battleHud);
            return;
        }

        DraftResult draftResult =
            DraftResult.CreateTowerUpgradeDraft(upgrade);

        if (!battleHud.TryAddPendingDraft(
                draftResult,
                out PendingDraftUIItem committedItem,
                out string failureReason))
        {
            Debug.LogWarning(
                $"Tower upgrade debug window failed to grant " +
                $"'{GetUpgradeName(upgrade)}': {failureReason}",
                battleHud);
            return;
        }

        Debug.Log(
            $"Tower upgrade debug window granted Pending Draft " +
            $"'{GetUpgradeName(upgrade)}' after validating " +
            $"'{tower.name}'. The Draft remains target-independent and must " +
            "be dragged through the normal flow.",
            committedItem);
    }

    private void ApplyUpgradeDirectly()
    {
        if (!TryGetEligibleSelection(
                out TowerInstance tower,
                out TowerUpgradeDefinition upgrade,
                out TowerUpgradeSystem upgradeSystem))
        {
            return;
        }

        if (!upgradeSystem.TryApplyUpgrade(
                tower,
                upgrade,
                out string failureReason))
        {
            Debug.LogWarning(
                $"Tower upgrade debug window failed to apply " +
                $"'{GetUpgradeName(upgrade)}' to '{tower.name}': " +
                failureReason,
                tower);
            return;
        }

        TowerBehaviour towerBehaviour = tower.GetComponent<TowerBehaviour>();

        if (towerBehaviour != null && towerBehaviour.VisualController != null)
        {
            towerBehaviour.VisualController.PlayUpgradeAppliedFeedback();
        }

        Debug.Log(
            $"Tower upgrade debug window directly applied " +
            $"'{GetUpgradeName(upgrade)}' to '{tower.name}'.",
            tower);
        Repaint();
    }

    private bool TryGetSelection(
        out TowerInstance tower,
        out TowerUpgradeDefinition upgrade)
    {
        tower = selectedTower;
        upgrade = selectedUpgrade;

        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Tower upgrade debug window actions require Play Mode.");
            return false;
        }

        if (tower == null || upgrade == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window requires a runtime Tower and " +
                "compatible Upgrade selection.");
            return false;
        }

        return true;
    }

    private bool TryGetEligibleSelection(
        out TowerInstance tower,
        out TowerUpgradeDefinition upgrade,
        out TowerUpgradeSystem upgradeSystem)
    {
        upgradeSystem = null;

        if (!TryGetSelection(out tower, out upgrade))
        {
            return false;
        }

        upgradeSystem = FindFirstObjectByType<TowerUpgradeSystem>();

        if (upgradeSystem == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot validate eligibility: " +
                "TowerUpgradeSystem is missing.");
            return false;
        }

        if (upgradeSystem.CanApplyUpgrade(
                tower,
                upgrade,
                out string failureReason))
        {
            return true;
        }

        Debug.LogWarning(
            $"Tower upgrade debug window rejected " +
            $"'{GetUpgradeName(upgrade)}' for '{tower.name}': " +
            failureReason,
            tower);
        return false;
    }

    private static int CompareTowers(TowerInstance left, TowerInstance right)
    {
        return string.CompareOrdinal(
            GetTowerLabel(left),
            GetTowerLabel(right));
    }

    private static string GetTowerLabel(TowerInstance tower)
    {
        if (tower == null)
        {
            return "Missing Tower";
        }

        return tower.gameObject.name;
    }

    private static string GetUpgradeName(TowerUpgradeDefinition upgrade)
    {
        if (upgrade == null)
        {
            return "Missing Upgrade";
        }

        return string.IsNullOrEmpty(upgrade.DisplayName)
            ? upgrade.name
            : upgrade.DisplayName;
    }

    private static string GetAppliedUpgradeSummary(TowerInstance tower)
    {
        if (tower == null || tower.AppliedUpgrades == null ||
            tower.AppliedUpgrades.Count == 0)
        {
            return "None";
        }

        List<string> names = new List<string>();

        for (int i = 0; i < tower.AppliedUpgrades.Count; i++)
        {
            names.Add(GetUpgradeName(tower.AppliedUpgrades[i]));
        }

        return string.Join(", ", names);
    }
}
