using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class TowerUpgradeDraftDebugWindow : EditorWindow
{
    private const string WindowMenuPath =
        "Tools/Tower Nexus/Tower Upgrade Draft Debug";
    private static ulong debugDraftAttemptIdentity;

    private readonly List<TowerInstance> runtimeTowers =
        new List<TowerInstance>();

    [SerializeField]
    private TowerDefinition selectedTowerDefinition;
    private TowerInstance selectedTower;
    [SerializeField]
    private List<TowerUpgradeDefinition> selectedUpgrades =
        new List<TowerUpgradeDefinition>();
    private SerializedObject serializedWindow;
    private SerializedProperty selectedTowerDefinitionProperty;
    private SerializedProperty selectedUpgradesProperty;
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
        EnsureSerializedProperties();
        RefreshContent();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField(
            "Tower Upgrade Draft Debug",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Editor-only Task003/Task004 helper. Configure one Upgrade list " +
            "to prepare a runtime Tower, grant real Pending Tower / Upgrade " +
            "Drafts, or apply the Upgrade list directly through " +
            "TowerUpgradeSystem.",
            MessageType.Info);

        DrawRuntimeStatus();

        if (GUILayout.Button("Refresh Runtime Towers"))
        {
            RefreshContent();
        }

        EditorGUILayout.Space();
        DrawPendingTowerDraftGrant();
        EditorGUILayout.Space();
        DrawTowerSelection();
        DrawUpgradeSelection();
        DrawSelectionStatus();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPendingTowerDraftGrant()
    {
        EditorGUILayout.LabelField(
            "Pending Tower Draft",
            EditorStyles.boldLabel);

        EnsureSerializedProperties();
        serializedWindow.Update();
        EditorGUILayout.PropertyField(
            selectedTowerDefinitionProperty,
            new GUIContent("Tower Definition"));
        serializedWindow.ApplyModifiedProperties();

        bool canGrantTowerDraft =
            Application.isPlaying && selectedTowerDefinition != null;

        using (new EditorGUI.DisabledScope(!canGrantTowerDraft))
        {
            if (GUILayout.Button("Grant Tower As Pending Draft"))
            {
                GrantPendingTowerDraft();
            }
        }

        EditorGUILayout.HelpBox(
            "Drag one TowerDefinition here during Play Mode, then grant it " +
            "to the Pending area. The item uses the normal Tower Draft " +
            "drag, preview, placement, and consumption flow.",
            MessageType.None);
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
            "Upgrade Definitions",
            EditorStyles.boldLabel);

        EnsureSerializedProperties();
        serializedWindow.Update();
        EditorGUILayout.PropertyField(
            selectedUpgradesProperty,
            new GUIContent("Upgrade Definitions"),
            includeChildren: true);
        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.HelpBox(
            "Assign the complete Basic / Behaviour / Elemental build to test. " +
            "TowerFamily, Required Tower Level, duplicate, package, and " +
            "Elemental rules remain enforced.",
            MessageType.None);
    }

    private void DrawSelectionStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selection Status", EditorStyles.boldLabel);

        if (selectedTower == null)
        {
            EditorGUILayout.HelpBox(
                "A runtime Tower and Upgrade list are required.",
                MessageType.None);
            return;
        }

        if (!TryGetConfiguredUpgrades(
                selectedTower,
                out List<TowerUpgradeDefinition> upgrades,
                out string configurationFailureReason))
        {
            EditorGUILayout.HelpBox(
                configurationFailureReason,
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField(
            "Current Tower Level",
            selectedTower.CurrentLevel.ToString());
        EditorGUILayout.LabelField(
            "Maximum Required Level",
            GetMaximumRequiredLevel(upgrades).ToString());
        EditorGUILayout.LabelField(
            "Applied Upgrades",
            GetAppliedUpgradeSummary(selectedTower));

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];
            EditorGUILayout.LabelField(
                $"#{i + 1} {GetUpgradeName(upgrade)}",
                $"{upgrade.UpgradeLayer} / L{upgrade.RequiredTowerLevel}");
        }

        TowerUpgradeSystem upgradeSystem =
            FindFirstObjectByType<TowerUpgradeSystem>();

        if (upgradeSystem == null)
        {
            EditorGUILayout.HelpBox(
                "TowerUpgradeSystem is missing.",
                MessageType.Error);
            return;
        }

        int eligibleCount = 0;
        int appliedCount = 0;
        List<string> blockedReasons = new List<string>();

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];

            if (selectedTower.HasUpgrade(upgrade))
            {
                appliedCount++;
                continue;
            }

            if (upgradeSystem.CanApplyUpgrade(
                    selectedTower,
                    upgrade,
                    out string failureReason))
            {
                eligibleCount++;
                continue;
            }

            blockedReasons.Add(
                $"{GetUpgradeName(upgrade)}: {failureReason}");
        }

        if (blockedReasons.Count == 0)
        {
            EditorGUILayout.HelpBox(
                $"List status: {appliedCount} applied, {eligibleCount} " +
                "currently eligible.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(
            "Prepare the required level or resolve these eligibility issues:\n" +
            string.Join("\n", blockedReasons),
            MessageType.Warning);
    }

    private void DrawActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        bool hasSelection =
            Application.isPlaying &&
            selectedTower != null &&
            selectedUpgrades != null &&
            selectedUpgrades.Count > 0;

        using (new EditorGUI.DisabledScope(!hasSelection))
        {
            if (GUILayout.Button("Prepare List Required Level (Debug)"))
            {
                PrepareRequiredLevels();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Grant List As Pending Drafts"))
            {
                GrantPendingUpgradeDrafts();
            }

            if (GUILayout.Button("Apply List Directly"))
            {
                ApplyUpgradesDirectly();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.HelpBox(
            "Prepare bypasses Player Progress, Draft cost, and Stage level " +
            "caps. Grant and Direct Apply are alternative paths: Grant " +
            "creates Pending Draft items for normal dragging; Direct Apply " +
            "builds the Tower immediately. Both preserve eligibility rules. " +
            "Restart Play Mode between independent balance runs; this tool " +
            "does not remove Upgrades.",
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

    private void PrepareRequiredLevels()
    {
        if (!TryGetBatchSelection(
                out TowerInstance tower,
                out List<TowerUpgradeDefinition> upgrades))
        {
            return;
        }

        int requiredLevel = GetMaximumRequiredLevel(upgrades);

        if (tower.CurrentLevel >= requiredLevel)
        {
            Debug.Log(
                $"Tower upgrade debug window did not change '{tower.name}': " +
                $"current level {tower.CurrentLevel} already satisfies the " +
                $"list's maximum Required Tower Level {requiredLevel}.",
                tower);
            return;
        }

        Debug.LogWarning(
            $"Tower upgrade debug window cannot force '{tower.name}' from " +
            $"Level {tower.CurrentLevel} to Level {requiredLevel}. Tower Level " +
            "changes now require the prepared Level-Up transaction and exact " +
            "held Draft consumption. Use a Fixed Draft calibration sequence " +
            "to reach the required Level before applying this Upgrade list.",
            tower);
        Repaint();
    }

    private void GrantPendingUpgradeDrafts()
    {
        if (!TryGetEligibleBatch(
                out TowerInstance tower,
                out List<TowerUpgradeDefinition> upgrades,
                out _))
        {
            return;
        }

        BattleHUDUI battleHud = FindFirstObjectByType<BattleHUDUI>();

        if (battleHud == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant Pending Drafts: " +
                "BattleHUDUI is missing.");
            return;
        }

        if (battleHud.IsDraftOpen)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant Pending Drafts " +
                "while the normal Draft window is open.",
                battleHud);
            return;
        }

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];

            if (tower.HasUpgrade(upgrade))
            {
                continue;
            }

            if (TryGetPendingDraftConflict(
                    battleHud,
                    upgrade,
                    out string pendingFailureReason))
            {
                Debug.LogWarning(
                    "Tower upgrade debug window cannot grant the configured " +
                    "list: " + pendingFailureReason,
                    battleHud);
                return;
            }
        }

        var results = new List<DraftResult>();
        var tokens = new List<DraftAttemptToken>();
        foreach (var upgrade in upgrades)
        {
            if (tower.HasUpgrade(upgrade)) continue;
            results.Add(DraftResult.CreateTowerUpgradeDraft(upgrade));
            tokens.Add(CreateDebugDraftAttemptToken());
        }
        if (results.Count == 0) return;
        string failureReason = "Draft owner is unavailable.";
        if (battleHud.DraftOwner == null ||
            !battleHud.DraftOwner.TryGrantDebugPendingBatch(results, tokens, out failureReason))
        {
            Debug.LogWarning($"Pending batch preparation failed: {failureReason}. No rewards granted.", battleHud);
            return;
        }

        Debug.Log(
            $"Tower upgrade debug window granted {results.Count} " +
            $"Pending Upgrade Drafts after validating '{tower.name}'. The " +
            "Drafts remain target-independent and must be dragged through " +
            "the normal flow.",
            battleHud);
        Repaint();
    }

    private void GrantPendingTowerDraft()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Tower upgrade debug window actions require Play Mode.");
            return;
        }

        TowerDefinition towerDefinition = selectedTowerDefinition;

        if (towerDefinition == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window requires a Tower Definition " +
                "to grant a Pending Tower Draft.");
            return;
        }

        if (!towerDefinition.IsValid())
        {
            Debug.LogWarning(
                $"Tower upgrade debug window cannot grant " +
                $"'{GetTowerDefinitionName(towerDefinition)}': the Tower " +
                "Definition is invalid.",
                towerDefinition);
            return;
        }

        BattleHUDUI battleHud = FindFirstObjectByType<BattleHUDUI>();

        if (battleHud == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant a Pending Tower " +
                "Draft: BattleHUDUI is missing.");
            return;
        }

        if (battleHud.IsDraftOpen)
        {
            Debug.LogWarning(
                "Tower upgrade debug window cannot grant a Pending Tower " +
                "Draft while the normal Draft window is open.",
                battleHud);
            return;
        }

        DraftResult draftResult =
            DraftResult.CreateTowerDraft(towerDefinition);

        string failureReason = "Draft owner is unavailable.";
        if (battleHud.DraftOwner == null || !battleHud.DraftOwner.TryGrantDebugPendingBatch(
                new[] { draftResult }, new[] { CreateDebugDraftAttemptToken() },
                out failureReason))
        {
            Debug.LogWarning(
                $"Tower upgrade debug window failed to grant " +
                $"'{GetTowerDefinitionName(towerDefinition)}': " +
                failureReason,
                battleHud);
            return;
        }

        Debug.Log(
            $"Tower upgrade debug window granted " +
            $"'{GetTowerDefinitionName(towerDefinition)}' as a Pending " +
            "Tower Draft. It must be dragged through the normal placement " +
            "flow.",
            battleHud);
        Repaint();
    }

    private void ApplyUpgradesDirectly()
    {
        if (!TryGetEligibleBatch(
                out TowerInstance tower,
                out List<TowerUpgradeDefinition> upgrades,
                out TowerUpgradeSystem upgradeSystem))
        {
            return;
        }

        string targetName = tower.name;
        int appliedCount = 0;

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];

            if (tower == null || upgradeSystem == null) return;

            if (tower.HasUpgrade(upgrade))
            {
                continue;
            }

            TowerSubmissionResult result = upgradeSystem.ApplyDebugUpgrade(tower, upgrade);
            string failureReason = result.FailureReason;
            if (result.Outcome == TowerSubmissionOutcome.Committed)
            {
                appliedCount++;
                continue;
            }

            Debug.LogWarning(
                $"Tower upgrade debug request ended as {result.Outcome}: " +
                $"'{GetUpgradeName(upgrade)}' on '{targetName}', after " +
                $"committing {appliedCount} earlier list entries: " +
                failureReason,
                tower);
            return;
        }

        Debug.Log(
            $"Tower upgrade debug window directly applied {appliedCount} " +
            $"configured Upgrades to '{targetName}'. Entries already applied " +
            "were skipped.",
            tower);
        Repaint();
    }

    private bool TryGetBatchSelection(
        out TowerInstance tower,
        out List<TowerUpgradeDefinition> upgrades)
    {
        tower = selectedTower;
        upgrades = null;

        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Tower upgrade debug window actions require Play Mode.");
            return false;
        }

        if (tower == null)
        {
            Debug.LogWarning(
                "Tower upgrade debug window requires a runtime Tower.");
            return false;
        }

        if (TryGetConfiguredUpgrades(
                tower,
                out upgrades,
                out string failureReason))
        {
            return true;
        }

        Debug.LogWarning(
            "Tower upgrade debug window rejected the configured Upgrade " +
            "list: " + failureReason,
            tower);
        return false;
    }

    private bool TryGetEligibleBatch(
        out TowerInstance tower,
        out List<TowerUpgradeDefinition> upgrades,
        out TowerUpgradeSystem upgradeSystem)
    {
        upgradeSystem = null;

        if (!TryGetBatchSelection(out tower, out upgrades))
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

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];

            if (tower.HasUpgrade(upgrade))
            {
                continue;
            }

            if (upgradeSystem.CanApplyUpgrade(
                    tower,
                    upgrade,
                    out string failureReason))
            {
                continue;
            }

            Debug.LogWarning(
                $"Tower upgrade debug window rejected " +
                $"'{GetUpgradeName(upgrade)}' for '{tower.name}': " +
                failureReason,
                tower);
            return false;
        }

        return true;
    }

    private void EnsureSerializedProperties()
    {
        if (serializedWindow != null &&
            selectedTowerDefinitionProperty != null &&
            selectedUpgradesProperty != null)
        {
            return;
        }

        serializedWindow = new SerializedObject(this);
        selectedTowerDefinitionProperty =
            serializedWindow.FindProperty(nameof(selectedTowerDefinition));
        selectedUpgradesProperty =
            serializedWindow.FindProperty(nameof(selectedUpgrades));
    }

    private bool TryGetConfiguredUpgrades(
        TowerInstance tower,
        out List<TowerUpgradeDefinition> upgrades,
        out string failureReason)
    {
        upgrades = new List<TowerUpgradeDefinition>();
        failureReason = string.Empty;

        if (tower == null || tower.TowerDefinition == null)
        {
            failureReason = "A valid runtime Tower is required.";
            return false;
        }

        if (selectedUpgrades == null || selectedUpgrades.Count == 0)
        {
            failureReason = "Configure at least one Upgrade Definition.";
            return false;
        }

        HashSet<TowerUpgradeDefinition> uniqueUpgrades =
            new HashSet<TowerUpgradeDefinition>();
        HashSet<TowerBehaviourPackageType> behaviourPackages =
            new HashSet<TowerBehaviourPackageType>();
        int elementalCount = 0;

        for (int i = 0; i < selectedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = selectedUpgrades[i];

            if (upgrade == null)
            {
                failureReason =
                    $"Upgrade list entry #{i + 1} is missing.";
                return false;
            }

            if (!uniqueUpgrades.Add(upgrade))
            {
                failureReason =
                    $"'{GetUpgradeName(upgrade)}' appears more than once.";
                return false;
            }

            if (upgrade.TowerFamily != tower.TowerDefinition.TowerFamily)
            {
                failureReason =
                    $"'{GetUpgradeName(upgrade)}' belongs to " +
                    $"{upgrade.TowerFamily}, but the selected Tower belongs " +
                    $"to {tower.TowerDefinition.TowerFamily}.";
                return false;
            }

            if (!upgrade.IsValid())
            {
                failureReason =
                    $"'{GetUpgradeName(upgrade)}' is not a valid Upgrade " +
                    "Definition.";
                return false;
            }

            if (upgrade.UpgradeLayer == TowerUpgradeLayer.Elemental)
            {
                elementalCount++;

                if (elementalCount > 1)
                {
                    failureReason =
                        "A configured build may contain at most one " +
                        "Elemental Upgrade.";
                    return false;
                }
            }

            if (upgrade.UpgradeLayer == TowerUpgradeLayer.Behaviour &&
                upgrade.BehaviourPackageType !=
                    TowerBehaviourPackageType.None &&
                !behaviourPackages.Add(upgrade.BehaviourPackageType))
            {
                failureReason =
                    $"Behaviour package '{upgrade.BehaviourPackageType}' " +
                    "appears more than once in the configured build.";
                return false;
            }

            upgrades.Add(upgrade);
        }

        return true;
    }

    private static int GetMaximumRequiredLevel(
        IReadOnlyList<TowerUpgradeDefinition> upgrades)
    {
        int requiredLevel = 1;

        for (int i = 0; i < upgrades.Count; i++)
        {
            requiredLevel = Mathf.Max(
                requiredLevel,
                upgrades[i].RequiredTowerLevel);
        }

        return requiredLevel;
    }

    private static bool TryGetPendingDraftConflict(
        BattleHUDUI battleHud,
        TowerUpgradeDefinition candidate,
        out string failureReason)
    {
        failureReason = string.Empty;

        if (battleHud == null || battleHud.DraftOwner == null || candidate == null)
        {
            return false;
        }

        IReadOnlyList<PendingDraftEntry> pendingItems =
            battleHud.DraftOwner.PendingDrafts;

        for (int i = 0; i < pendingItems.Count; i++)
        {
            PendingDraftEntry pendingItem = pendingItems[i];
            TowerUpgradeDefinition pendingUpgrade = pendingItem != null
                ? pendingItem.TowerUpgradeDefinition
                : null;

            if (pendingUpgrade == null)
            {
                continue;
            }

            if (pendingUpgrade == candidate)
            {
                failureReason =
                    $"'{GetUpgradeName(candidate)}' is already pending.";
                return true;
            }

            if (pendingUpgrade.UpgradeLayer ==
                    TowerUpgradeLayer.Elemental &&
                candidate.UpgradeLayer == TowerUpgradeLayer.Elemental &&
                pendingUpgrade.TowerFamily == candidate.TowerFamily)
            {
                failureReason =
                    $"TowerFamily '{candidate.TowerFamily}' already has " +
                    $"pending Elemental Upgrade " +
                    $"'{GetUpgradeName(pendingUpgrade)}'.";
                return true;
            }
        }

        return false;
    }

    private static int CompareTowers(TowerInstance left, TowerInstance right)
    {
        return string.CompareOrdinal(
            GetTowerLabel(left),
            GetTowerLabel(right));
    }

    private static DraftAttemptToken CreateDebugDraftAttemptToken()
    {
        debugDraftAttemptIdentity++;
        return new DraftAttemptToken(
            ulong.MaxValue,
            debugDraftAttemptIdentity);
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

    private static string GetTowerDefinitionName(
        TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            return "Missing Tower";
        }

        return string.IsNullOrEmpty(towerDefinition.DisplayName)
            ? towerDefinition.name
            : towerDefinition.DisplayName;
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
