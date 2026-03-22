using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed partial class HexTacticsPrototype : MonoBehaviour
{
    [Header("Board")]
    [SerializeField, Range(2, 6)] private int boardRadius = 2;
    [SerializeField, Min(0.6f)] private float hexRadius = 1.15f;
    [SerializeField, Min(0.1f)] private float tileHeight = 0.28f;
    [SerializeField, Min(0.1f)] private float unitHoverHeight = 0.18f;

    [Header("Camera")]
    [SerializeField, Range(20f, 70f)] private float cameraFieldOfView = 42f;
    [SerializeField, Range(15f, 70f)] private float cameraPitch = 41f;
    [SerializeField, Range(-180f, 180f)] private float cameraYaw = 32f;
    [SerializeField, Range(1f, 1.5f)] private float cameraFitPadding = 1f;
    [SerializeField, Min(1f)] private float cameraMinDistance = 5.4f;
    [SerializeField, Min(0.01f)] private float cameraMoveSmoothTime = 0.24f;
    [SerializeField, Min(0.01f)] private float cameraZoomSmoothTime = 0.18f;
    [SerializeField, Range(1f, 20f)] private float cameraRotationSmoothness = 9f;
    [SerializeField, Range(0.7f, 0.98f)] private float attackCameraDistanceScale = 0.93f;
    [SerializeField, Range(0.55f, 1f)] private float attackCameraMinDistanceScale = 0.9f;
    [SerializeField, Range(0f, 1.2f)] private float attackCameraFocusLift = 0.14f;

    [Header("Battle")]
    [SerializeField, Min(0.05f)] private float moveDuration = 0.22f;
    [SerializeField, Min(0.05f)] private float attackDuration = 0.26f;
    [SerializeField, Min(0.02f)] private float damagedReactionDuration = 0.18f;
    [SerializeField, Min(0f)] private float sequentialAttackGap = 0.05f;
    [SerializeField, Min(0.05f)] private float cpuThinkDelay = 0.35f;
    [SerializeField, Min(1)] private int playerTeamCostLimit = 12;
    [SerializeField, Min(1)] private int cpuTeamCostLimit = 12;
    [SerializeField] private bool autoPopulateDefaultRoster = true;

    [Header("Presentation")]
    [SerializeField, Min(0.8f)] private float baseUnitVisualHeight = 1.62f;
    [SerializeField, Min(0.15f)] private float worldLabelPadding = 0.34f;
    [SerializeField, Range(0.8f, 1.4f)] private float selectionFootprintPadding = 1.05f;

    [Header("Hit Effects")]
    [SerializeField] private bool enableHitEffects = true;
    [SerializeField, Range(0.15f, 1.2f)] private float hitEffectHeightNormalized = 0.58f;
    [SerializeField, Min(0f)] private float hitEffectForwardOffset = 0.08f;

    [Header("Hit Feel")]
    [SerializeField, Range(0f, 0.3f)] private float hitShakeDistanceNormalized = 0.16f;
    [SerializeField, Min(0.02f)] private float hitShakeDuration = 0.12f;

    [Header("Roster")]
    [SerializeField] private List<HexTacticsCharacterConfig> characterRoster = new();

    private static readonly HexCoord[] NeighborDirections =
    {
        new(-1, 0),
        new(0, -1),
        new(1, -1),
        new(1, 0),
        new(0, 1),
        new(-1, 1)
    };

    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int DamagedHash = Animator.StringToHash("Damaged");
    private static readonly int ShiftHash = Animator.StringToHash("Shift");
    private static readonly int StandHash = Animator.StringToHash("Stand");
    private static readonly int SwimHash = Animator.StringToHash("Swim");
    private static readonly int FallHash = Animator.StringToHash("Fall");
    private static readonly int ActionHash = Animator.StringToHash("Action");
    private static readonly int StunnedHash = Animator.StringToHash("Stunned");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private const float SkillPopupHoldDuration = 0.24f;
    private const float SkillPopupMoveTolerance = 18f;

    private readonly Dictionary<HexCoord, HexCell> cells = new();
    private readonly Dictionary<Collider, HexCell> cellLookups = new();
    private readonly Dictionary<Collider, HexUnit> unitLookups = new();
    private readonly List<HexUnit> units = new();
    private readonly List<HexCell> moveCells = new();
    private readonly List<HexCell> attackCells = new();
    private readonly List<Material> runtimeMaterials = new();
    private readonly List<int> cpuTeamSelection = new();
    private readonly List<PlayerDeploymentEntry> playerDeploymentEntries = new();
    private readonly List<HexCoord> blueDeploySlots = new();
    private readonly List<HexCoord> redDeploySlots = new();
    private readonly HashSet<HexCoord> blueDeploySlotLookup = new();
    private readonly Dictionary<HexTacticsCharacterVisualArchetype, GameObject> unitVisualPrefabCache = new();

    private Transform boardRoot;
    private Transform unitsRoot;
    private Transform effectsRoot;
    private Mesh cellMesh;

    private Material tilePrimaryMaterial;
    private Material tileSecondaryMaterial;
    private Material tileMoveMaterial;
    private Material tileAttackMaterial;
    private Material tileSelectedMaterial;
    private Material platformMaterial;
    private Material blueBodyMaterial;
    private Material blueRingMaterial;
    private Material redBodyMaterial;
    private Material redRingMaterial;
    private Material selectedRingMaterial;
    private Material plannedRingMaterial;

    private HexUnit selectedUnit;
    private FlowState currentFlowState;
    private ResolutionKind lastResolutionKind;
    private Team? winningTeam;
    private int planningRoundNumber;
    private int resolvedTurnCount;
    private bool isAnimating;
    private bool isResolving;
    private string builderStatus = string.Empty;
    private string resolutionStatus = string.Empty;
    private int nextUnitId;
    private int nextPlayerDeploymentEntryId;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private FlowState lastCameraFlowState;
    private Vector3 cameraTargetPosition;
    private Quaternion cameraTargetRotation = Quaternion.identity;
    private Vector3 cameraPositionVelocity;
    private float cameraTargetFieldOfView;
    private float cameraTargetFarClipPlane;
    private float cameraFovVelocity;
    private bool hasCameraTarget;
    private bool cameraTransformInitialized;
    private CameraFocusOverride activeCameraFocusOverride;
    private HexTacticsHitEffectCatalog hitEffectCatalog;
    private GameObject rangedWaveEffectPrefab;
    private int nextHitEffectVariantIndex;
    private bool isPlanningPointerPressed;
    private bool planningPointerStartedOverUi;
    private bool planningPointerHoldTriggered;
    private Vector2 planningPointerPressPosition;
    private float planningPointerPressStartTime;
    private HexUnit planningPointerPressUnit;
    private bool isSkillPopupOpen;
    private Vector2 skillPopupScreenPosition;
    private int skillPopupHoveredSkillIndex = -1;

    private void Awake()
    {
        BuildPrototype();
    }

    private void OnValidate()
    {
        PopulateCharacterRosterIfNeeded();
    }

    private void Update()
    {
        if (currentFlowState != FlowState.Planning || isAnimating || isResolving)
        {
            return;
        }

        if (Mouse.current != null)
        {
            HandlePlanningPointerInput();
        }

        if (Keyboard.current == null || selectedUnit == null || selectedUnit.Team != Team.Blue)
        {
            return;
        }

        if (Keyboard.current.backspaceKey.wasPressedThisFrame || Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            SetUnitWaitCommand(selectedUnit);
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            CycleUnitSkill(selectedUnit);
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            UiSelectSelectedUnitSkill(0);
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            UiSelectSelectedUnitSkill(1);
            return;
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            UiSelectSelectedUnitSkill(2);
            return;
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            UiSelectSelectedUnitSkill(3);
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(-1, 0));
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(0, -1));
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(1, -1));
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(1, 0));
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(0, 1));
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            AssignDirectionalCommand(selectedUnit, new HexCoord(-1, 1));
        }
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight ||
            currentFlowState != lastCameraFlowState ||
            activeCameraFocusOverride != null ||
            !hasCameraTarget)
        {
            ConfigureCamera();
        }

        UpdateCameraTransform();
    }

    private void OnDestroy()
    {
        ReleaseGeneratedAssets();
    }

    private void BuildPrototype()
    {
        EnsureRoots();
        DestroyChildren(boardRoot);
        DestroyChildren(unitsRoot);
        DestroyChildren(effectsRoot);
        ReleaseGeneratedAssets();

        cells.Clear();
        cellLookups.Clear();
        unitLookups.Clear();
        units.Clear();
        moveCells.Clear();
        attackCells.Clear();
        cpuTeamSelection.Clear();
        playerDeploymentEntries.Clear();
        blueDeploySlots.Clear();
        redDeploySlots.Clear();
        blueDeploySlotLookup.Clear();
        selectedUnit = null;
        currentFlowState = FlowState.ModeSelect;
        lastResolutionKind = ResolutionKind.None;
        winningTeam = null;
        planningRoundNumber = 1;
        resolvedTurnCount = 0;
        isAnimating = false;
        isResolving = false;
        builderStatus = string.Empty;
        resolutionStatus = string.Empty;
        nextUnitId = 1;
        nextPlayerDeploymentEntryId = 1;
        cameraPositionVelocity = Vector3.zero;
        cameraFovVelocity = 0f;
        hasCameraTarget = false;
        cameraTransformInitialized = false;
        activeCameraFocusOverride = null;
        nextHitEffectVariantIndex = 0;

        EnsureCharacterRoster();
        BuildMaterials();
        cellMesh = BuildHexPrismMesh(hexRadius, tileHeight);

        BuildBoard();
        CacheDeploySlots();
        ConfigureCamera(immediate: true);
        RefreshVisuals();
    }

    private void EnsureRoots()
    {
        boardRoot = GetOrCreateChild("Board").transform;
        unitsRoot = GetOrCreateChild("Units").transform;
        effectsRoot = GetOrCreateChild("Effects").transform;
    }

    private GameObject GetOrCreateChild(string objectName)
    {
        var child = transform.Find(objectName);
        if (child != null)
        {
            return child.gameObject;
        }

        var obj = new GameObject(objectName);
        obj.transform.SetParent(transform, false);
        return obj;
    }

    private void EnsureCharacterRoster()
    {
        PopulateCharacterRosterIfNeeded();
    }

    [ContextMenu("Reload Character Roster From Addressables")]
    private void RebuildCharacterRosterFromAvailablePrefabs()
    {
        characterRoster = LoadCharacterRosterFromCatalog();
    }

    private void PopulateCharacterRosterIfNeeded()
    {
        if (!autoPopulateDefaultRoster || characterRoster.Count > 0)
        {
            return;
        }

        characterRoster = LoadCharacterRosterFromCatalog();
    }

    private List<HexTacticsCharacterConfig> LoadCharacterRosterFromCatalog()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            var editorRoster = LoadCharacterRosterFromAssetDatabase();
            if (editorRoster.Count > 0)
            {
                return editorRoster;
            }

            return BuildFallbackCharacterRoster();
        }
#endif

        var roster = HexTacticsAddressables.LoadCharacterConfigs();
        if (roster.Count > 0)
        {
            return roster;
        }

        return BuildFallbackCharacterRoster();
    }

    private List<HexTacticsCharacterConfig> BuildFallbackCharacterRoster()
    {
        var roster = new List<HexTacticsCharacterConfig>();
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.Fawn, "幼鹿斥候", "幼鹿，低 cost 高机动侦察", 7, 2, 2, 2, 0, 6);
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.Doe, "林地游骑", "母鹿，游击压制与快速补位", 9, 3, 3, 2, 1, 5);
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.Stag, "角冠先锋", "牡鹿，稳健推进的均衡前锋", 12, 4, 4, 1, 0, 3);
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.WhiteTiger, "霜牙猎手", "白虎，擅长追击的精英猎手", 10, 4, 4, 2, 1, 5);
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.Tiger, "猛虎斗士", "孟加拉虎，高爆发近战输出", 11, 5, 5, 1, 0, 4);
        TryAddFallbackCharacter(roster, HexTacticsCharacterVisualArchetype.Elk, "巨角卫士", "驼鹿，高血量低机动防线", 16, 3, 5, 1, 0, 2);
        return roster;
    }

    private void TryAddFallbackCharacter(
        List<HexTacticsCharacterConfig> roster,
        HexTacticsCharacterVisualArchetype archetype,
        string displayName,
        string description,
        int maxHealth,
        int attackPower,
        int cost,
        int moveRange,
        int attackRange,
        int speed)
    {
        if (LoadUnitVisualPrefabFromArchetype(archetype) == null)
        {
            return;
        }

        var config = ScriptableObject.CreateInstance<HexTacticsCharacterConfig>();
        config.hideFlags = HideFlags.DontSave;
        config.ConfigureRuntime(displayName, description, maxHealth, attackPower, cost, moveRange, attackRange, speed, archetype);
        roster.Add(config);
    }

#if UNITY_EDITOR
    private static List<HexTacticsCharacterConfig> LoadCharacterRosterFromAssetDatabase()
    {
        var roster = new List<HexTacticsCharacterConfig>();
        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config != null)
            {
                roster.Add(config);
            }
        }

        roster.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
        return roster;
    }
#endif

    private void BuildMaterials()
    {
        tilePrimaryMaterial = CreateLitMaterial(new Color(0.42f, 0.49f, 0.34f), new Color(0.05f, 0.08f, 0.04f));
        tileSecondaryMaterial = CreateLitMaterial(new Color(0.50f, 0.43f, 0.30f), new Color(0.06f, 0.05f, 0.03f));
        tileMoveMaterial = CreateLitMaterial(new Color(0.44f, 0.88f, 0.72f), new Color(0.10f, 0.25f, 0.18f));
        tileAttackMaterial = CreateLitMaterial(new Color(0.95f, 0.55f, 0.36f), new Color(0.26f, 0.09f, 0.04f));
        tileSelectedMaterial = CreateLitMaterial(new Color(1.00f, 0.85f, 0.42f), new Color(0.25f, 0.18f, 0.04f));
        platformMaterial = CreateLitMaterial(new Color(0.22f, 0.27f, 0.20f), new Color(0.01f, 0.03f, 0.02f));
        blueBodyMaterial = CreateLitMaterial(new Color(0.18f, 0.45f, 0.82f), new Color(0.03f, 0.06f, 0.12f));
        blueRingMaterial = CreateLitMaterial(new Color(0.36f, 0.66f, 0.98f), new Color(0.07f, 0.11f, 0.18f));
        redBodyMaterial = CreateLitMaterial(new Color(0.82f, 0.30f, 0.26f), new Color(0.11f, 0.04f, 0.03f));
        redRingMaterial = CreateLitMaterial(new Color(0.95f, 0.58f, 0.36f), new Color(0.16f, 0.06f, 0.02f));
        selectedRingMaterial = CreateLitMaterial(new Color(1.00f, 0.91f, 0.50f), new Color(0.32f, 0.24f, 0.05f));
        plannedRingMaterial = CreateLitMaterial(new Color(0.60f, 0.88f, 0.70f), new Color(0.08f, 0.20f, 0.10f));
    }

}
