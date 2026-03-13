using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed partial class HexTacticsPrototype : MonoBehaviour
{
    [Header("Board")]
    [SerializeField, Range(2, 6)] private int boardRadius = 4;
    [SerializeField, Min(0.6f)] private float hexRadius = 1.15f;
    [SerializeField, Min(0.1f)] private float tileHeight = 0.28f;
    [SerializeField, Min(0.1f)] private float unitHoverHeight = 0.18f;

    [Header("Camera")]
    [SerializeField, Range(20f, 70f)] private float cameraFieldOfView = 42f;
    [SerializeField, Range(15f, 70f)] private float cameraPitch = 41f;
    [SerializeField, Range(-180f, 180f)] private float cameraYaw = 32f;
    [SerializeField, Range(1f, 1.5f)] private float cameraFitPadding = 1.08f;
    [SerializeField, Min(1f)] private float cameraMinDistance = 7f;

    [Header("Battle")]
    [SerializeField, Min(0.05f)] private float moveDuration = 0.22f;
    [SerializeField, Min(0.05f)] private float attackDuration = 0.26f;
    [SerializeField, Min(0.05f)] private float cpuThinkDelay = 0.35f;
    [SerializeField, Min(1)] private int playerTeamCostLimit = 12;
    [SerializeField, Min(1)] private int cpuTeamCostLimit = 12;
    [SerializeField] private bool autoPopulateDefaultRoster = true;

    [Header("Presentation")]
    [SerializeField, Min(0.8f)] private float baseUnitVisualHeight = 1.52f;
    [SerializeField, Min(0.15f)] private float worldLabelPadding = 0.34f;
    [SerializeField, Range(0.8f, 1.4f)] private float selectionFootprintPadding = 1.05f;

    [Header("Roster")]
    [SerializeField] private List<CharacterDefinition> characterRoster = new();

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

    private readonly Dictionary<HexCoord, HexCell> cells = new();
    private readonly Dictionary<Collider, HexCell> cellLookups = new();
    private readonly Dictionary<Collider, HexUnit> unitLookups = new();
    private readonly List<HexUnit> units = new();
    private readonly List<HexCell> moveCells = new();
    private readonly List<HexCell> attackCells = new();
    private readonly List<Material> runtimeMaterials = new();
    private readonly List<int> playerTeamSelection = new();
    private readonly List<int> cpuTeamSelection = new();
    private readonly Dictionary<UnitVisualArchetype, GameObject> unitVisualPrefabCache = new();

    private Transform boardRoot;
    private Transform unitsRoot;
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

    private GUIStyle centeredLabelStyle;
    private GUIStyle worldLabelStyle;
    private GUIStyle titleLabelStyle;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        BuildPrototype();
    }

    private void Update()
    {
        if (currentFlowState != FlowState.Planning || isAnimating || isResolving)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        ConfigureCamera();
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
        ReleaseGeneratedAssets();

        cells.Clear();
        cellLookups.Clear();
        unitLookups.Clear();
        units.Clear();
        moveCells.Clear();
        attackCells.Clear();
        playerTeamSelection.Clear();
        cpuTeamSelection.Clear();
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

        EnsureCharacterRoster();
        BuildMaterials();
        cellMesh = BuildHexPrismMesh(hexRadius, tileHeight);

        BuildBoard();
        ConfigureCamera();
        RefreshVisuals();
    }

    private void EnsureRoots()
    {
        boardRoot = GetOrCreateChild("Board").transform;
        unitsRoot = GetOrCreateChild("Units").transform;
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
        if (!autoPopulateDefaultRoster || characterRoster.Count > 0)
        {
            return;
        }

        characterRoster = new List<CharacterDefinition>
        {
            new("角冠先锋", "牡鹿，均衡近战", 12, 3, 4, 2, UnitVisualArchetype.Stag),
            new("林地游骑", "母鹿，快速突击", 9, 4, 4, 3, UnitVisualArchetype.Doe),
            new("巨角卫士", "驼鹿，高血量前排", 16, 2, 5, 1, UnitVisualArchetype.Elk),
            new("幼鹿斥候", "幼鹿，低 cost 高机动", 7, 2, 2, 4, UnitVisualArchetype.Fawn),
            new("猛虎斗士", "孟加拉虎，爆发输出", 8, 5, 5, 2, UnitVisualArchetype.Tiger),
            new("霜牙猎手", "白虎，稳定追击", 11, 4, 4, 3, UnitVisualArchetype.WhiteTiger)
        };
    }

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
