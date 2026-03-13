#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HexTacticsUiPrefabBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string UiFolder = "Assets/Resources/UI";
    private const string ScreensFolder = UiFolder + "/Screens";
    private const string ElementsFolder = UiFolder + "/Elements";

    private const string CanvasPrefabPath = UiFolder + "/HexTacticsCanvasRoot.prefab";
    private const string ModeSelectScreenPrefabPath = ScreensFolder + "/HexTacticsModeSelectScreen.prefab";
    private const string TeamBuilderScreenPrefabPath = ScreensFolder + "/HexTacticsTeamBuilderScreen.prefab";
    private const string PlanningScreenPrefabPath = ScreensFolder + "/HexTacticsPlanningScreen.prefab";
    private const string ResolvingScreenPrefabPath = ScreensFolder + "/HexTacticsResolvingScreen.prefab";
    private const string VictoryOverlayPrefabPath = ScreensFolder + "/HexTacticsVictoryOverlay.prefab";
    private const string RosterRowPrefabPath = ElementsFolder + "/HexTacticsRosterRow.prefab";
    private const string SelectedRosterRowPrefabPath = ElementsFolder + "/HexTacticsSelectedRosterRow.prefab";
    private const string CommandRowPrefabPath = ElementsFolder + "/HexTacticsCommandRow.prefab";
    private const string WorldLabelPrefabPath = ElementsFolder + "/HexTacticsWorldLabel.prefab";

    private static readonly string[] RequiredPrefabPaths =
    {
        CanvasPrefabPath,
        ModeSelectScreenPrefabPath,
        TeamBuilderScreenPrefabPath,
        PlanningScreenPrefabPath,
        ResolvingScreenPrefabPath,
        VictoryOverlayPrefabPath,
        RosterRowPrefabPath,
        SelectedRosterRowPrefabPath,
        CommandRowPrefabPath,
        WorldLabelPrefabPath
    };

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExistsOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        for (var i = 0; i < RequiredPrefabPaths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RequiredPrefabPaths[i]) == null)
            {
                GeneratePrefab();
                return;
            }
        }
    }

    [MenuItem("Tools/Hex Tactics/Regenerate UI Prefabs")]
    public static void GeneratePrefab()
    {
        EnsureFolders();

        var rosterRow = BuildGeneratedPrefab<HexTacticsRosterRowView>(RosterRowPrefabPath, "HexTacticsRosterRow");
        var selectedRosterRow = BuildGeneratedPrefab<HexTacticsSelectedRosterRowView>(SelectedRosterRowPrefabPath, "HexTacticsSelectedRosterRow");
        var commandRow = BuildGeneratedPrefab<HexTacticsCommandRowView>(CommandRowPrefabPath, "HexTacticsCommandRow");
        var worldLabel = BuildGeneratedPrefab<HexTacticsWorldLabelView>(WorldLabelPrefabPath, "HexTacticsWorldLabel");

        var modeSelectScreen = BuildGeneratedPrefab<HexTacticsModeSelectScreenView>(ModeSelectScreenPrefabPath, "HexTacticsModeSelectScreen");
        var teamBuilderScreen = BuildGeneratedPrefab<HexTacticsTeamBuilderScreenView>(
            TeamBuilderScreenPrefabPath,
            "HexTacticsTeamBuilderScreen",
            view => view.ConfigureItemPrefabs(rosterRow, selectedRosterRow));
        var planningScreen = BuildGeneratedPrefab<HexTacticsPlanningScreenView>(
            PlanningScreenPrefabPath,
            "HexTacticsPlanningScreen",
            view => view.ConfigureItemPrefab(commandRow));
        var resolvingScreen = BuildGeneratedPrefab<HexTacticsResolvingScreenView>(ResolvingScreenPrefabPath, "HexTacticsResolvingScreen");
        var victoryOverlay = BuildGeneratedPrefab<HexTacticsVictoryOverlayView>(VictoryOverlayPrefabPath, "HexTacticsVictoryOverlay");

        BuildGeneratedPrefab<HexTacticsCanvasView>(
            CanvasPrefabPath,
            "HexTacticsCanvasRoot",
            view => view.ConfigurePrefabs(modeSelectScreen, teamBuilderScreen, planningScreen, resolvingScreen, victoryOverlay, worldLabel),
            buildHierarchy: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void GeneratePrefabFromBatchmode()
    {
        GeneratePrefab();
    }

    private static T BuildGeneratedPrefab<T>(string prefabPath, string name, System.Action<T> configure = null, bool buildHierarchy = true)
        where T : HexTacticsUiGeneratedView
    {
        var root = new GameObject(name, typeof(RectTransform));

        try
        {
            var view = root.AddComponent<T>();
            configure?.Invoke(view);
            if (buildHierarchy)
            {
                view.EnsureBuilt();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return AssetDatabase.LoadAssetAtPath<T>(prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(UiFolder))
        {
            AssetDatabase.CreateFolder(ResourcesFolder, "UI");
        }

        if (!AssetDatabase.IsValidFolder(ScreensFolder))
        {
            AssetDatabase.CreateFolder(UiFolder, "Screens");
        }

        if (!AssetDatabase.IsValidFolder(ElementsFolder))
        {
            AssetDatabase.CreateFolder(UiFolder, "Elements");
        }
    }
}
#endif
