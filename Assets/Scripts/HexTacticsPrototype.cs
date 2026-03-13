using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class HexTacticsPrototype : MonoBehaviour
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

    private Material CreateLitMaterial(Color color, Color emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        runtimeMaterials.Add(material);
        return material;
    }

    private void BuildBoard()
    {
        var platform = new GameObject("Platform");
        platform.transform.SetParent(boardRoot, false);
        platform.transform.localPosition = new Vector3(0f, -tileHeight * 0.72f, 0f);

        var platformFilter = platform.AddComponent<MeshFilter>();
        platformFilter.sharedMesh = BuildHexPrismMesh(hexRadius * (boardRadius * 1.9f + 1.5f), tileHeight * 1.25f);
        var platformRenderer = platform.AddComponent<MeshRenderer>();
        platformRenderer.sharedMaterial = platformMaterial;

        for (var q = -boardRadius; q <= boardRadius; q++)
        {
            var minR = Mathf.Max(-boardRadius, -q - boardRadius);
            var maxR = Mathf.Min(boardRadius, -q + boardRadius);

            for (var r = minR; r <= maxR; r++)
            {
                var coord = new HexCoord(q, r);
                var cellObject = new GameObject($"Hex {q},{r}");
                cellObject.transform.SetParent(boardRoot, false);
                cellObject.transform.localPosition = HexToWorld(coord);

                var filter = cellObject.AddComponent<MeshFilter>();
                filter.sharedMesh = cellMesh;

                var renderer = cellObject.AddComponent<MeshRenderer>();
                var baseMaterial = ((q - r) & 1) == 0 ? tilePrimaryMaterial : tileSecondaryMaterial;
                renderer.sharedMaterial = baseMaterial;

                var collider = cellObject.AddComponent<MeshCollider>();
                collider.sharedMesh = cellMesh;

                var cell = new HexCell(coord, renderer, baseMaterial);
                cells.Add(coord, cell);
                cellLookups.Add(collider, cell);
            }
        }
    }

    private void ConfigureCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var framingPoints = CollectCameraFramingPoints();
        if (framingPoints.Count == 0)
        {
            return;
        }

        var bounds = new Bounds(framingPoints[0], Vector3.zero);
        for (var i = 1; i < framingPoints.Count; i++)
        {
            bounds.Encapsulate(framingPoints[i]);
        }

        var rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        var inverseRotation = Quaternion.Inverse(rotation);
        var focus = bounds.center;
        var verticalHalfFov = cameraFieldOfView * Mathf.Deg2Rad * 0.5f;
        var tanVertical = Mathf.Tan(verticalHalfFov);
        var tanHorizontal = tanVertical * mainCamera.aspect;

        var requiredDistance = cameraMinDistance;
        foreach (var point in framingPoints)
        {
            var localPoint = inverseRotation * (point - focus);
            localPoint.x *= cameraFitPadding;
            localPoint.y *= cameraFitPadding;

            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.x) / tanHorizontal - localPoint.z);
            requiredDistance = Mathf.Max(requiredDistance, Mathf.Abs(localPoint.y) / tanVertical - localPoint.z);
        }

        var forward = rotation * Vector3.forward;
        mainCamera.transform.SetPositionAndRotation(focus - forward * requiredDistance, rotation);
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = Mathf.Max(200f, requiredDistance * 6f);
        mainCamera.fieldOfView = cameraFieldOfView;
        mainCamera.backgroundColor = new Color(0.72f, 0.79f, 0.78f);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void StartCpuMode()
    {
        currentFlowState = FlowState.TeamBuilder;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToModeSelect()
    {
        currentFlowState = FlowState.ModeSelect;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToTeamBuilder()
    {
        currentFlowState = FlowState.TeamBuilder;
        builderStatus = string.Empty;
        ReturnToNonBattleBoardState();
    }

    private void ReturnToNonBattleBoardState()
    {
        ClearUnits();
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        winningTeam = null;
        lastResolutionKind = ResolutionKind.None;
        resolvedTurnCount = 0;
        planningRoundNumber = 1;
        isAnimating = false;
        isResolving = false;
        resolutionStatus = string.Empty;
        RefreshVisuals();
    }

    private void ClearUnits()
    {
        unitLookups.Clear();
        units.Clear();
        DestroyChildren(unitsRoot);

        foreach (var cell in cells.Values)
        {
            cell.Occupant = null;
        }
    }

    private void TryAddCharacterToPlayerTeam(int rosterIndex)
    {
        if (currentFlowState != FlowState.TeamBuilder || !IsValidRosterIndex(rosterIndex))
        {
            return;
        }

        var definition = characterRoster[rosterIndex];
        if (GetTeamCost(playerTeamSelection) + definition.cost > playerTeamCostLimit)
        {
            builderStatus = "已超过队伍总 cost 上限";
            return;
        }

        playerTeamSelection.Add(rosterIndex);
        builderStatus = string.Empty;
    }

    private void RemovePlayerCharacterAt(int selectionIndex)
    {
        if (selectionIndex < 0 || selectionIndex >= playerTeamSelection.Count)
        {
            return;
        }

        playerTeamSelection.RemoveAt(selectionIndex);
        builderStatus = string.Empty;
    }

    private bool TryStartCpuBattle()
    {
        if (playerTeamSelection.Count == 0)
        {
            builderStatus = "至少选择 1 名角色才能开始";
            return false;
        }

        if (!BuildCpuTeamSelection())
        {
            builderStatus = "CPU 无法在当前预算下组成队伍，请调整角色配置";
            return false;
        }

        StartBattleFromSelections();
        return true;
    }

    private bool BuildCpuTeamSelection()
    {
        cpuTeamSelection.Clear();

        var remainingCost = cpuTeamCostLimit;
        while (true)
        {
            var affordable = new List<int>();
            for (var i = 0; i < characterRoster.Count; i++)
            {
                if (characterRoster[i].cost <= remainingCost)
                {
                    affordable.Add(i);
                }
            }

            if (affordable.Count == 0)
            {
                break;
            }

            var pick = affordable[Random.Range(0, affordable.Count)];
            cpuTeamSelection.Add(pick);
            remainingCost -= characterRoster[pick].cost;
        }

        return cpuTeamSelection.Count > 0;
    }

    private void StartBattleFromSelections()
    {
        ClearUnits();
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        winningTeam = null;
        planningRoundNumber = 1;
        resolvedTurnCount = 0;
        lastResolutionKind = ResolutionKind.None;
        resolutionStatus = "第 1 轮计划开始";
        isAnimating = false;
        isResolving = false;

        SpawnConfiguredTeam(playerTeamSelection, Team.Blue, GetDeploySlots(Team.Blue), "玩家");
        SpawnConfiguredTeam(cpuTeamSelection, Team.Red, GetDeploySlots(Team.Red), "CPU");
        BeginPlanningRound();
    }

    private void BeginPlanningRound()
    {
        currentFlowState = FlowState.Planning;
        isResolving = false;
        isAnimating = false;
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();

        foreach (var unit in units)
        {
            AssignWaitCommand(unit);
        }

        RefreshVisuals();
    }

    private void SpawnConfiguredTeam(List<int> rosterSelection, Team team, List<HexCoord> deploySlots, string ownerLabel)
    {
        var displayCounts = new Dictionary<string, int>();
        var count = Mathf.Min(rosterSelection.Count, deploySlots.Count);
        for (var i = 0; i < count; i++)
        {
            var rosterIndex = rosterSelection[i];
            if (!IsValidRosterIndex(rosterIndex))
            {
                continue;
            }

            var definition = characterRoster[rosterIndex];
            var coord = deploySlots[i];
            if (!cells.TryGetValue(coord, out var cell) || cell.Occupant != null)
            {
                continue;
            }

            var unitRoot = new GameObject(CreateBattleUnitName(ownerLabel, definition, displayCounts));
            unitRoot.transform.SetParent(unitsRoot, false);
            unitRoot.transform.localPosition = CellToUnitPosition(coord);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(unitRoot.transform, false);
            ring.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.55f, 0f);
            ring.transform.localScale = new Vector3(0.58f, 0.16f, 0.58f);
            var ringFilter = ring.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = cellMesh;
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = team == Team.Blue ? blueRingMaterial : redRingMaterial;

            var unit = new HexUnit(
                unitRoot.name,
                definition.displayName,
                team,
                coord,
                unitRoot.transform,
                ringRenderer,
                team == Team.Blue ? blueRingMaterial : redRingMaterial,
                definition.maxHealth,
                definition.attackPower,
                definition.cost,
                definition.moveRange);

            unit.VisualHeight = baseUnitVisualHeight;
            unit.LabelHeight = baseUnitVisualHeight + worldLabelPadding;
            unit.SelectionRadius = hexRadius * 0.42f;

            if (!TryAttachAnimatedVisual(unit, definition))
            {
                AttachFallbackUnitVisual(unit);
            }

            ApplyRingScale(unit);
            FaceUnitTowards(unit, unit.Transform.position + GetTeamFacingDirection(team), immediate: true);
            SetUnitIdle(unit);

            cell.Occupant = unit;
            units.Add(unit);
        }
    }

    private static string CreateBattleUnitName(string ownerLabel, CharacterDefinition definition, Dictionary<string, int> displayCounts)
    {
        if (!displayCounts.TryGetValue(definition.displayName, out var count))
        {
            count = 0;
        }

        count++;
        displayCounts[definition.displayName] = count;
        return $"{ownerLabel} {definition.displayName} {count}";
    }

    private void RegisterUnitCollider(Collider collider, HexUnit unit)
    {
        if (collider != null)
        {
            unitLookups[collider] = unit;
        }
    }

    private bool TryAttachAnimatedVisual(HexUnit unit, CharacterDefinition definition)
    {
        var prefab = LoadUnitVisualPrefab(definition.visualArchetype);
        if (prefab == null)
        {
            return false;
        }

        var visualInstance = Instantiate(prefab);
        visualInstance.name = $"{definition.visualArchetype} Visual";
        visualInstance.transform.SetParent(unit.Transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        PrepareVisualInstance(visualInstance);

        if (!TryFitVisualToCell(unit, visualInstance.transform, definition.visualArchetype, out var fittedBounds))
        {
            if (Application.isPlaying)
            {
                Destroy(visualInstance);
            }
            else
            {
                DestroyImmediate(visualInstance);
            }

            return false;
        }

        unit.VisualRoot = visualInstance.transform;
        unit.Animator = visualInstance.GetComponentInChildren<Animator>(true);
        ConfigureAnimator(unit.Animator);
        UpdateUnitPresentationMetrics(unit, fittedBounds);
        ConfigureUnitSelectionCollider(unit, fittedBounds);
        return true;
    }

    private void AttachFallbackUnitVisual(HexUnit unit)
    {
        var teamBodyMaterial = unit.Team == Team.Blue ? blueBodyMaterial : redBodyMaterial;
        var teamAccentMaterial = unit.Team == Team.Blue ? blueRingMaterial : redRingMaterial;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(unit.Transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.62f, 0.55f);
        body.GetComponent<MeshRenderer>().sharedMaterial = teamBodyMaterial;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(unit.Transform, false);
        head.transform.localPosition = new Vector3(0f, 1.52f, 0f);
        head.transform.localScale = Vector3.one * 0.34f;
        head.GetComponent<MeshRenderer>().sharedMaterial = teamAccentMaterial;

        unit.VisualHeight = 1.78f;
        unit.LabelHeight = 2.12f;
        unit.SelectionRadius = hexRadius * 0.42f;

        RegisterUnitCollider(body.GetComponent<Collider>(), unit);
        RegisterUnitCollider(head.GetComponent<Collider>(), unit);
    }

    private GameObject LoadUnitVisualPrefab(UnitVisualArchetype archetype)
    {
        if (unitVisualPrefabCache.TryGetValue(archetype, out var cachedPrefab) && cachedPrefab != null)
        {
            return cachedPrefab;
        }

        var resourcePath = GetVisualResourcePath(archetype);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null)
        {
            unitVisualPrefabCache[archetype] = prefab;
        }

        return prefab;
    }

    private static string GetVisualResourcePath(UnitVisualArchetype archetype)
    {
        return archetype switch
        {
            UnitVisualArchetype.Stag => "BattleUnits/Stag",
            UnitVisualArchetype.Doe => "BattleUnits/Doe",
            UnitVisualArchetype.Elk => "BattleUnits/Elk",
            UnitVisualArchetype.Fawn => "BattleUnits/Fawn",
            UnitVisualArchetype.Tiger => "BattleUnits/Tiger",
            UnitVisualArchetype.WhiteTiger => "BattleUnits/WhiteTiger",
            _ => string.Empty
        };
    }

    private float GetTargetVisualHeight(UnitVisualArchetype archetype)
    {
        return archetype switch
        {
            UnitVisualArchetype.Elk => baseUnitVisualHeight * 1.20f,
            UnitVisualArchetype.Stag => baseUnitVisualHeight * 1.08f,
            UnitVisualArchetype.Doe => baseUnitVisualHeight * 0.98f,
            UnitVisualArchetype.Fawn => baseUnitVisualHeight * 0.76f,
            UnitVisualArchetype.Tiger => baseUnitVisualHeight * 0.96f,
            UnitVisualArchetype.WhiteTiger => baseUnitVisualHeight * 1.02f,
            _ => baseUnitVisualHeight
        };
    }

    private void PrepareVisualInstance(GameObject visualInstance)
    {
        foreach (var collider in visualInstance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (var rigidBody in visualInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidBody.useGravity = false;
            rigidBody.isKinematic = true;
            rigidBody.detectCollisions = false;
        }

        foreach (var trailRenderer in visualInstance.GetComponentsInChildren<TrailRenderer>(true))
        {
            trailRenderer.enabled = false;
        }

        foreach (var lineRenderer in visualInstance.GetComponentsInChildren<LineRenderer>(true))
        {
            lineRenderer.enabled = false;
        }

        foreach (var particleSystem in visualInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.gameObject.SetActive(false);
        }

        foreach (var audioSource in visualInstance.GetComponentsInChildren<AudioSource>(true))
        {
            audioSource.enabled = false;
        }
    }

    private bool TryFitVisualToCell(HexUnit unit, Transform visualRoot, UnitVisualArchetype archetype, out Bounds fittedBounds)
    {
        fittedBounds = default;
        if (!TryGetRenderableBounds(visualRoot, out var initialBounds))
        {
            return false;
        }

        var targetHeight = GetTargetVisualHeight(archetype);
        var safeHeight = Mathf.Max(0.01f, initialBounds.size.y);
        var scaleFactor = targetHeight / safeHeight;
        visualRoot.localScale *= scaleFactor;

        if (!TryGetRenderableBounds(visualRoot, out var scaledBounds))
        {
            return false;
        }

        var anchor = unit.Transform.position;
        var offset = new Vector3(
            anchor.x - scaledBounds.center.x,
            anchor.y - scaledBounds.min.y,
            anchor.z - scaledBounds.center.z);
        visualRoot.position += offset;

        if (!TryGetRenderableBounds(visualRoot, out fittedBounds))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetRenderableBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private void UpdateUnitPresentationMetrics(HexUnit unit, Bounds bounds)
    {
        unit.VisualHeight = Mathf.Max(baseUnitVisualHeight * 0.75f, bounds.max.y - unit.Transform.position.y);
        unit.LabelHeight = unit.VisualHeight + worldLabelPadding;
        unit.SelectionRadius = Mathf.Clamp(
            Mathf.Max(bounds.extents.x, bounds.extents.z) * selectionFootprintPadding,
            hexRadius * 0.26f,
            hexRadius * 0.58f);
    }

    private void ConfigureUnitSelectionCollider(HexUnit unit, Bounds bounds)
    {
        var collider = unit.Transform.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = unit.Transform.gameObject.AddComponent<CapsuleCollider>();
        }

        var centerWorld = new Vector3(unit.Transform.position.x, bounds.center.y, unit.Transform.position.z);
        collider.center = unit.Transform.InverseTransformPoint(centerWorld);
        collider.radius = unit.SelectionRadius;
        collider.height = Mathf.Max(0.95f, bounds.size.y);
        collider.direction = 1;
        collider.isTrigger = false;
        collider.enabled = true;
        RegisterUnitCollider(collider, unit);
    }

    private void ApplyRingScale(HexUnit unit)
    {
        var ringScale = Mathf.Clamp((unit.SelectionRadius / hexRadius) * 1.35f, 0.42f, 0.92f);
        unit.RingRenderer.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.55f, 0f);
        unit.RingRenderer.transform.localScale = new Vector3(ringScale, 0.16f, ringScale);
    }

    private static Vector3 GetTeamFacingDirection(Team team)
    {
        return team == Team.Blue ? Vector3.right : Vector3.left;
    }

    private List<HexCoord> GetDeploySlots(Team team)
    {
        var allCoords = new List<HexCoord>(cells.Keys);
        allCoords.Sort((left, right) =>
        {
            var leftWorld = HexToWorld(left);
            var rightWorld = HexToWorld(right);
            var xCompare = leftWorld.x.CompareTo(rightWorld.x);
            if (xCompare != 0)
            {
                return xCompare;
            }

            return leftWorld.z.CompareTo(rightWorld.z);
        });

        var blueSlotCount = (allCoords.Count + 1) / 2;
        var deploySlots = team == Team.Blue
            ? allCoords.GetRange(0, blueSlotCount)
            : allCoords.GetRange(blueSlotCount, allCoords.Count - blueSlotCount);

        deploySlots.Sort((left, right) =>
        {
            var leftWorld = HexToWorld(left);
            var rightWorld = HexToWorld(right);

            var sideCompare = team == Team.Blue
                ? leftWorld.x.CompareTo(rightWorld.x)
                : rightWorld.x.CompareTo(leftWorld.x);
            if (sideCompare != 0)
            {
                return sideCompare;
            }

            var spreadCompare = Mathf.Abs(rightWorld.z).CompareTo(Mathf.Abs(leftWorld.z));
            if (spreadCompare != 0)
            {
                return spreadCompare;
            }

            return leftWorld.z.CompareTo(rightWorld.z);
        });
        return deploySlots;
    }

    private void HandlePlanningPointerInput()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var pointerPosition = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(pointerPosition);
        var hits = Physics.RaycastAll(ray, 200f);
        if (hits.Length == 0)
        {
            SelectUnit(null);
            return;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        if (TryResolvePlanningUnitHit(hits, out var clickedUnit))
        {
            HandlePlanningUnitClick(clickedUnit);
            return;
        }

        if (TryResolvePlanningCellHit(hits, out var clickedCell))
        {
            if (clickedCell.Occupant != null)
            {
                HandlePlanningUnitClick(clickedCell.Occupant);
                return;
            }

            HandlePlanningCellClick(clickedCell);
            return;
        }

        SelectUnit(null);
    }

    private bool TryResolvePlanningUnitHit(RaycastHit[] hits, out HexUnit clickedUnit)
    {
        clickedUnit = null;
        var bestPriority = int.MaxValue;
        var bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!unitLookups.TryGetValue(hit.collider, out var candidate))
            {
                continue;
            }

            var priority = GetPlanningUnitHitPriority(candidate);
            if (priority >= 10)
            {
                continue;
            }

            if (priority < bestPriority || (priority == bestPriority && hit.distance < bestDistance))
            {
                bestPriority = priority;
                bestDistance = hit.distance;
                clickedUnit = candidate;
            }
        }

        return clickedUnit != null;
    }

    private bool TryResolvePlanningCellHit(RaycastHit[] hits, out HexCell clickedCell)
    {
        clickedCell = null;
        foreach (var hit in hits)
        {
            if (cellLookups.TryGetValue(hit.collider, out clickedCell))
            {
                return true;
            }
        }

        return false;
    }

    private int GetPlanningUnitHitPriority(HexUnit unit)
    {
        if (unit == null)
        {
            return 10;
        }

        if (unit.Team != Team.Blue)
        {
            return selectedUnit != null && selectedUnit.Team == Team.Blue && IsAttackOption(unit.Coord) ? 0 : 10;
        }

        if (selectedUnit != null && selectedUnit != unit && IsMoveOption(unit.Coord))
        {
            return 1;
        }

        return 2;
    }

    private void HandlePlanningUnitClick(HexUnit clickedUnit)
    {
        if (clickedUnit.Team == Team.Blue)
        {
            if (selectedUnit != null && selectedUnit != clickedUnit && IsMoveOption(clickedUnit.Coord))
            {
                SetUnitMoveCommand(selectedUnit, clickedUnit.Coord);
                return;
            }

            SelectUnit(selectedUnit == clickedUnit ? null : clickedUnit);
            return;
        }

        if (selectedUnit != null && selectedUnit.Team == Team.Blue && IsAttackOption(clickedUnit.Coord))
        {
            SetUnitAttackCommand(selectedUnit, clickedUnit.Coord);
            return;
        }

        SelectUnit(null);
    }

    private void HandlePlanningCellClick(HexCell clickedCell)
    {
        if (selectedUnit != null && IsMoveOption(clickedCell.Coord))
        {
            SetUnitMoveCommand(selectedUnit, clickedCell.Coord);
            return;
        }

        SelectUnit(null);
    }

    private void AssignDirectionalCommand(HexUnit unit, HexCoord direction)
    {
        var target = unit.Coord + direction;
        if (!cells.TryGetValue(target, out var targetCell))
        {
            return;
        }

        if (targetCell.Occupant != null && targetCell.Occupant.Team != unit.Team)
        {
            SetUnitAttackCommand(unit, target);
        }
        else
        {
            SetUnitMoveCommand(unit, target);
        }
    }

    private void SelectUnit(HexUnit unit)
    {
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();

        if (unit != null && unit.Team == Team.Blue && currentFlowState == FlowState.Planning)
        {
            selectedUnit = unit;
            foreach (var cell in GetMoveOptions(unit))
            {
                moveCells.Add(cell);
            }

            foreach (var cell in GetAttackOptions(unit))
            {
                attackCells.Add(cell);
            }
        }

        RefreshVisuals();
    }

    private bool IsMoveOption(HexCoord coord)
    {
        foreach (var cell in moveCells)
        {
            if (cell.Coord == coord)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAttackOption(HexCoord coord)
    {
        foreach (var cell in attackCells)
        {
            if (cell.Coord == coord)
            {
                return true;
            }
        }

        return false;
    }

    private void SetUnitMoveCommand(HexUnit unit, HexCoord target)
    {
        if (unit == null || unit.Team != Team.Blue)
        {
            return;
        }

        if (IsMoveCommand(unit) && unit.PlannedMoveTarget == target)
        {
            AssignWaitCommand(unit);
            SelectUnit(null);
            return;
        }

        if (TryAssignMoveCommand(unit, target))
        {
            SelectUnit(null);
        }
    }

    private void SetUnitAttackCommand(HexUnit unit, HexCoord target)
    {
        if (unit == null || unit.Team != Team.Blue || !CanSelectAttackTarget(unit, target))
        {
            return;
        }

        if (IsEnemyCommand(unit) && unit.PlannedEnemyTargetUnit != null && unit.PlannedEnemyTargetUnit.Coord == target)
        {
            AssignWaitCommand(unit);
            SelectUnit(null);
            return;
        }

        TryAssignAttackCommand(unit, target);
        SelectUnit(null);
    }

    private void SetUnitWaitCommand(HexUnit unit)
    {
        if (unit == null || unit.Team != Team.Blue)
        {
            return;
        }

        AssignWaitCommand(unit);
        SelectUnit(null);
    }

    private void AutoPlanCpuCommands()
    {
        foreach (var unit in units)
        {
            if (unit.Team != Team.Red)
            {
                continue;
            }

            AssignWaitCommand(unit);

            var attackTarget = ChooseCpuAttackTarget(unit);
            if (attackTarget != null)
            {
                TryAssignAttackCommand(unit, attackTarget.Coord);
            }

            if (!IsEnemyCommand(unit))
            {
                var moveTarget = ChooseCpuMoveTarget(unit);
                if (moveTarget.HasValue)
                {
                    TryAssignMoveCommand(unit, moveTarget.Value);
                }
            }
        }
    }

    private HexUnit ChooseCpuAttackTarget(HexUnit unit)
    {
        HexUnit bestTarget = null;
        var bestScore = int.MinValue;

        foreach (var candidate in units)
        {
            if (candidate.Team != Team.Blue || !CanSelectAttackTarget(unit, candidate.Coord))
            {
                continue;
            }

            var score = candidate.AttackPower * 10 - candidate.CurrentHealth - HexDistance(unit.Coord, candidate.Coord) * 2;
            if (candidate.CurrentHealth <= unit.AttackPower)
            {
                score += 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private HexUnit ChooseCpuAttackTargetFromOrigin(HexUnit unit, HexCoord origin, Team enemyTeam)
    {
        HexUnit bestTarget = null;
        var bestScore = int.MinValue;

        foreach (var direction in NeighborDirections)
        {
            var coord = origin + direction;
            if (!cells.TryGetValue(coord, out var cell) || cell.Occupant == null || cell.Occupant.Team != enemyTeam)
            {
                continue;
            }

            var score = cell.Occupant.AttackPower * 10 - cell.Occupant.CurrentHealth;
            if (cell.Occupant.CurrentHealth <= unit.AttackPower)
            {
                score += 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = cell.Occupant;
            }
        }

        return bestTarget;
    }

    private HexCoord? ChooseCpuMoveTarget(HexUnit unit)
    {
        HexCoord? bestTarget = null;
        var bestScore = int.MaxValue;

        foreach (var cell in GetMoveOptions(unit))
        {
            var target = cell.Coord;
            var distance = GetDistanceToNearestEnemy(target, Team.Blue);
            var centerBias = HexDistance(target, new HexCoord(0, 0));
            var occupantPenalty = cell.Occupant == null ? 0 : 6;
            var score = distance * 10 + centerBias + occupantPenalty;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private int GetDistanceToNearestEnemy(HexCoord origin, Team enemyTeam)
    {
        var bestDistance = int.MaxValue;
        foreach (var unit in units)
        {
            if (unit.Team != enemyTeam)
            {
                continue;
            }

            var distance = HexDistance(origin, unit.Coord);
            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        return bestDistance == int.MaxValue ? 0 : bestDistance;
    }

    private void TryResolvePlanningRound()
    {
        if (currentFlowState != FlowState.Planning || isResolving || isAnimating)
        {
            return;
        }

        AutoPlanCpuCommands();
        StartCoroutine(ResolvePlanningRound());
    }

    private IEnumerator ResolvePlanningRound()
    {
        isResolving = true;
        isAnimating = true;
        currentFlowState = FlowState.Resolving;
        selectedUnit = null;
        moveCells.Clear();
        attackCells.Clear();
        ResetRoundResolutionState();

        yield return new WaitForSeconds(cpuThinkDelay);

        var openingMoveAttacks = CollectOpeningMoveCommandAttacks();
        var openingEnemyAttacks = CollectReadyEnemyCommandAttacks();
        var openingAttacks = new List<AttackEvent>(openingMoveAttacks);
        openingAttacks.AddRange(openingEnemyAttacks);
        if (openingAttacks.Count > 0)
        {
            lastResolutionKind = ResolutionKind.Attack;
            resolutionStatus = $"第 {planningRoundNumber} 轮进入攻击回合：{openingAttacks.Count} 次攻击同时结算，之后继续执行移动命令";
            MarkAttackUsage(openingMoveAttacks, lockMovementAfterAttack: false);
            MarkAttackUsage(openingEnemyAttacks, lockMovementAfterAttack: true);
            yield return ResolveAttackTurn(openingAttacks);
            resolvedTurnCount++;

            if (CheckVictory())
            {
                isAnimating = false;
                isResolving = false;
                currentFlowState = FlowState.Victory;
                RefreshVisuals();
                yield break;
            }
        }

        var moveTurnIndex = 1;
        while (true)
        {
            if (!HasPendingMoveSteps())
            {
                break;
            }

            var stepResult = EvaluateMoveTurn();
            lastResolutionKind = ResolutionKind.Move;
            yield return ResolveMoveTurn(resolvedTurnCount > 0, moveTurnIndex, stepResult);
            if (stepResult.SuccessfulMoves.Count == 0)
            {
                break;
            }

            AdvanceMovePaths(stepResult.SuccessfulMoves);
            resolvedTurnCount++;

            if (CheckVictory())
            {
                isAnimating = false;
                isResolving = false;
                currentFlowState = FlowState.Victory;
                RefreshVisuals();
                yield break;
            }

            var followUpAttacks = CollectReadyEnemyCommandAttacks();
            if (followUpAttacks.Count > 0)
            {
                lastResolutionKind = ResolutionKind.Attack;
                resolutionStatus = $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合后，{followUpAttacks.Count} 名棋子到达目标并完成攻击";
                MarkAttackUsage(followUpAttacks, lockMovementAfterAttack: true);
                yield return ResolveAttackTurn(followUpAttacks);
                resolvedTurnCount++;

                if (CheckVictory())
                {
                    isAnimating = false;
                    isResolving = false;
                    currentFlowState = FlowState.Victory;
                    RefreshVisuals();
                    yield break;
                }
            }

            moveTurnIndex++;
        }

        isAnimating = false;
        isResolving = false;

        if (CheckVictory())
        {
            currentFlowState = FlowState.Victory;
            RefreshVisuals();
            yield break;
        }

        planningRoundNumber++;
        BeginPlanningRound();
    }

    private void ResetRoundResolutionState()
    {
        foreach (var unit in units)
        {
            unit.AttackConsumed = false;
            unit.MovementLocked = false;
            unit.PlannedMoveProgress = 0;
        }
    }

    private List<AttackEvent> CollectOpeningMoveCommandAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (!IsMoveCommand(unit) || unit.AttackConsumed)
            {
                continue;
            }

            var target = ChooseCpuAttackTargetFromOrigin(unit, unit.Coord, unit.Team == Team.Blue ? Team.Red : Team.Blue);
            if (target == null)
            {
                continue;
            }

            unit.PlannedAttackTarget = target.Coord;
            events.Add(new AttackEvent(unit, target));
        }

        return events;
    }

    private List<AttackEvent> CollectReadyEnemyCommandAttacks()
    {
        var events = new List<AttackEvent>();
        foreach (var unit in units)
        {
            if (!IsEnemyCommand(unit) || unit.AttackConsumed)
            {
                continue;
            }

            if (!IsUnitAlive(unit.PlannedEnemyTargetUnit))
            {
                unit.MovementLocked = true;
                continue;
            }

            unit.PlannedAttackTarget = unit.PlannedEnemyTargetUnit.Coord;
            if (!AreAdjacent(unit.Coord, unit.PlannedEnemyTargetUnit.Coord))
            {
                continue;
            }

            events.Add(new AttackEvent(unit, unit.PlannedEnemyTargetUnit));
        }

        return events;
    }

    private void MarkAttackUsage(List<AttackEvent> attacks, bool lockMovementAfterAttack)
    {
        foreach (var attack in attacks)
        {
            attack.Attacker.AttackConsumed = true;
            if (lockMovementAfterAttack)
            {
                attack.Attacker.MovementLocked = true;
            }
        }
    }

    private void ConfigureAnimator(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.Rebind();
        animator.Update(0f);
        SetAnimatorLocomotion(animator, 0f, false);
        animator.SetBool(Attack1Hash, false);
        animator.SetBool(DamagedHash, false);
        animator.SetBool(StandHash, false);
        animator.SetBool(SwimHash, false);
        animator.SetBool(FallHash, false);
        animator.SetBool(ActionHash, false);
        animator.SetBool(StunnedHash, false);
    }

    private static void SetAnimatorLocomotion(Animator animator, float vertical, bool shift)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(VerticalHash, vertical);
        animator.SetFloat(HorizontalHash, 0f);
        animator.SetBool(ShiftHash, shift);
    }

    private void SetUnitIdle(HexUnit unit)
    {
        if (unit == null || unit.Animator == null)
        {
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.Animator.SetBool(Attack1Hash, false);
        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetBool(ActionHash, false);
        unit.Animator.SetBool(FallHash, false);
        unit.Animator.SetBool(SwimHash, false);
        unit.Animator.SetBool(StunnedHash, false);
    }

    private void SetUnitMoving(HexUnit unit, Vector3 worldTarget)
    {
        if (unit == null)
        {
            return;
        }

        FaceUnitTowards(unit, worldTarget, immediate: false);
        if (unit.Animator == null)
        {
            return;
        }

        var usesFastStride = unit.MoveRange >= 3;
        SetAnimatorLocomotion(unit.Animator, usesFastStride ? 1.15f : 0.82f, usesFastStride);
    }

    private void PlayUnitAttack(HexUnit unit, Vector3 worldTarget)
    {
        if (unit == null)
        {
            return;
        }

        FaceUnitTowards(unit, worldTarget, immediate: true);
        if (unit.Animator == null)
        {
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.Animator.SetBool(Attack1Hash, true);
        StartCoroutine(ResetAnimatorBoolAfterDelay(unit.Animator, Attack1Hash, attackDuration * 0.45f));
    }

    private void PlayUnitDamaged(HexUnit unit)
    {
        if (unit == null || unit.Animator == null)
        {
            return;
        }

        unit.Animator.SetBool(DamagedHash, true);
        StartCoroutine(ResetAnimatorBoolAfterDelay(unit.Animator, DamagedHash, 0.16f));
    }

    private void PlayUnitDeath(HexUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        SetSelectionColliderEnabled(unit, false);
        if (unit.Animator == null)
        {
            return;
        }

        SetAnimatorLocomotion(unit.Animator, 0f, false);
        unit.Animator.SetBool(Attack1Hash, false);
        unit.Animator.SetBool(DamagedHash, false);
        unit.Animator.SetTrigger(DeathHash);
    }

    private IEnumerator ResetAnimatorBoolAfterDelay(Animator animator, int parameterHash, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
        {
            animator.SetBool(parameterHash, false);
        }
    }

    private static void SetSelectionColliderEnabled(HexUnit unit, bool enabled)
    {
        if (unit?.Transform == null)
        {
            return;
        }

        var collider = unit.Transform.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = enabled;
        }
    }

    private void FaceUnitTowards(HexUnit unit, Vector3 worldTarget, bool immediate)
    {
        if (unit?.Transform == null)
        {
            return;
        }

        var direction = worldTarget - unit.Transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        unit.Transform.rotation = immediate
            ? targetRotation
            : Quaternion.Slerp(unit.Transform.rotation, targetRotation, 0.4f);
    }

    private IEnumerator ResolveAttackTurn(List<AttackEvent> attacks)
    {
        var attackerStarts = new Dictionary<HexUnit, Vector3>();
        var animatedAttackers = new HashSet<HexUnit>();
        foreach (var attack in attacks)
        {
            if (!attackerStarts.ContainsKey(attack.Attacker))
            {
                attackerStarts.Add(attack.Attacker, attack.Attacker.Transform.localPosition);
            }

            if (animatedAttackers.Add(attack.Attacker))
            {
                PlayUnitAttack(attack.Attacker, attack.Defender.Transform.position);
            }
        }

        var elapsed = 0f;
        var hitApplied = false;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / attackDuration);
            var lungeT = t < 0.5f ? t / 0.5f : 1f - ((t - 0.5f) / 0.5f);
            var smoothed = Mathf.SmoothStep(0f, 1f, lungeT);

            foreach (var attack in attacks)
            {
                if (!attackerStarts.TryGetValue(attack.Attacker, out var start))
                {
                    continue;
                }

                var direction = attack.Defender.Transform.localPosition - start;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.forward;
                }

                FaceUnitTowards(attack.Attacker, attack.Defender.Transform.position, immediate: false);
                direction.Normalize();
                var target = start + direction * (hexRadius * 0.48f);
                attack.Attacker.Transform.localPosition = Vector3.Lerp(start, target, smoothed);
            }

            if (!hitApplied && t >= 0.5f)
            {
                hitApplied = true;

                var damageMap = new Dictionary<HexUnit, int>();
                foreach (var attack in attacks)
                {
                    if (!damageMap.ContainsKey(attack.Defender))
                    {
                        damageMap.Add(attack.Defender, 0);
                    }

                    damageMap[attack.Defender] += attack.Attacker.AttackPower;
                }

                foreach (var pair in damageMap)
                {
                    pair.Key.CurrentHealth = Mathf.Max(0, pair.Key.CurrentHealth - pair.Value);
                    if (pair.Key.CurrentHealth > 0)
                    {
                        PlayUnitDamaged(pair.Key);
                    }
                }
            }

            yield return null;
        }

        foreach (var pair in attackerStarts)
        {
            pair.Key.Transform.localPosition = pair.Value;
            if (pair.Key.CurrentHealth > 0)
            {
                SetUnitIdle(pair.Key);
            }
        }

        var defeated = new List<HexUnit>();
        foreach (var unit in units)
        {
            if (unit.CurrentHealth <= 0)
            {
                defeated.Add(unit);
            }
        }

        foreach (var attack in attacks)
        {
            if (attack.Defender.CurrentHealth > 0)
            {
                SetUnitIdle(attack.Defender);
            }
        }

        if (defeated.Count > 0)
        {
            yield return AnimateUnitDefeat(defeated);
            foreach (var unit in defeated)
            {
                RemoveUnit(unit);
            }
        }

        RefreshVisuals();
    }

    private IEnumerator ResolveMoveTurn(bool followsAttackPhase, int moveTurnIndex, MoveStepResolution stepResult = default)
    {
        var effectiveResult = stepResult.HasValue ? stepResult : EvaluateMoveTurn();
        if (effectiveResult.Intents.Count == 0)
        {
            resolutionStatus = followsAttackPhase
                ? $"第 {planningRoundNumber} 轮攻击回合结束，移动第 {moveTurnIndex} 回合没有可执行的移动命令"
                : $"第 {planningRoundNumber} 轮进入移动第 {moveTurnIndex} 回合：所有棋子待机";
            yield return new WaitForSeconds(moveDuration * 0.6f);
            RefreshVisuals();
            yield break;
        }

        resolutionStatus = effectiveResult.ConflictCount > 0
            ? $"第 {planningRoundNumber} 轮移动第 {moveTurnIndex} 回合：{effectiveResult.ConflictCount} 处同阵营抢格已随机判定"
            : followsAttackPhase
                ? $"第 {planningRoundNumber} 轮攻击回合结束，{effectiveResult.SuccessfulMoves.Count} 名棋子继续同步前进一格"
                : $"第 {planningRoundNumber} 轮进入移动第 {moveTurnIndex} 回合：{effectiveResult.SuccessfulMoves.Count} 名棋子同步前进一格";

        if (effectiveResult.SuccessfulMoves.Count == 0)
        {
            yield return new WaitForSeconds(moveDuration * 0.6f);
            RefreshVisuals();
            yield break;
        }

        yield return AnimateMoveTurn(effectiveResult.SuccessfulMoves);
        ApplyMoveResults(effectiveResult.SuccessfulMoves);
        RefreshVisuals();
    }

    private List<MoveIntent> CollectMoveIntents()
    {
        var intents = new List<MoveIntent>();
        foreach (var unit in units)
        {
            if (!HasPendingMoveStep(unit))
            {
                continue;
            }

            if (!TryGetNextMoveStep(unit, out var nextStep))
            {
                continue;
            }

            intents.Add(new MoveIntent(unit, nextStep));
        }

        return intents;
    }

    private HexCoord? GetMoveGoal(HexUnit unit)
    {
        if (unit == null || !unit.HasPlannedMove)
        {
            return null;
        }

        if (IsEnemyCommand(unit))
        {
            return IsUnitAlive(unit.PlannedEnemyTargetUnit) ? unit.PlannedEnemyTargetUnit.Coord : null;
        }

        return unit.PlannedMoveTarget;
    }

    private bool IsMoveCommand(HexUnit unit)
    {
        return unit != null && unit.HasPlannedMove && !IsEnemyCommand(unit);
    }

    private bool IsEnemyCommand(HexUnit unit)
    {
        return unit != null && unit.HasPlannedMove && unit.HasPlannedAttack && unit.PlannedEnemyTargetUnit != null;
    }

    private bool IsUnitAlive(HexUnit unit)
    {
        return unit != null && units.Contains(unit) && unit.CurrentHealth > 0;
    }

    private bool TryGetNextMoveStep(HexUnit unit, out HexCoord nextStep)
    {
        nextStep = default;
        if (!HasPendingMoveStep(unit))
        {
            return false;
        }

        var moveGoal = GetMoveGoal(unit);
        if (!moveGoal.HasValue)
        {
            return false;
        }

        var remainingSteps = Mathf.Max(0, unit.MoveRange - unit.PlannedMoveProgress);
        if (remainingSteps <= 0)
        {
            return false;
        }

        var shouldStopAdjacent = IsEnemyCommand(unit);
        var path = FindShortestPath(
            unit,
            unit.Coord,
            moveGoal.Value,
            remainingSteps,
            shouldStopAdjacent,
            allowFriendlyWaitingOccupants: false);

        if (path.Count <= 1)
        {
            return false;
        }

        nextStep = path[1];
        return true;
    }

    private int GetMoveStepPriority(HexUnit unit, HexCell candidateCell)
    {
        var occupant = candidateCell.Occupant;
        if (occupant == null)
        {
            return 0;
        }

        if (occupant.Team == unit.Team)
        {
            return HasPendingMoveStep(occupant) ? 1 : 2;
        }

        return 3;
    }

    private List<HexCoord> FindShortestPath(
        HexUnit mover,
        HexCoord start,
        HexCoord goal,
        int maxSteps,
        bool stopAdjacentToGoal,
        bool allowFriendlyWaitingOccupants)
    {
        var path = new List<HexCoord>();
        if (mover == null || maxSteps < 0 || !cells.ContainsKey(start) || !cells.ContainsKey(goal))
        {
            return path;
        }

        if (stopAdjacentToGoal)
        {
            if (start == goal)
            {
                return path;
            }

            if (AreAdjacent(start, goal))
            {
                path.Add(start);
                return path;
            }
        }
        else if (start == goal)
        {
            path.Add(start);
            return path;
        }

        var frontier = new Queue<HexCoord>();
        var parents = new Dictionary<HexCoord, HexCoord>();
        var distances = new Dictionary<HexCoord, int>();
        var bestCoord = start;
        var bestGoalDistance = GetPathGoalDistance(start, goal, stopAdjacentToGoal);
        var bestTravelDistance = 0;

        frontier.Enqueue(start);
        distances[start] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            var distance = distances[current];
            if (distance >= maxSteps)
            {
                continue;
            }

            foreach (var neighbor in GetPathNeighbors(mover, start, current, goal, stopAdjacentToGoal, allowFriendlyWaitingOccupants))
            {
                if (distances.ContainsKey(neighbor))
                {
                    continue;
                }

                distances[neighbor] = distance + 1;
                parents[neighbor] = current;
                if (IsPathGoalReached(neighbor, goal, stopAdjacentToGoal))
                {
                    return ReconstructPath(start, neighbor, parents);
                }

                var goalDistance = GetPathGoalDistance(neighbor, goal, stopAdjacentToGoal);
                var travelDistance = distance + 1;
                if (goalDistance < bestGoalDistance ||
                    (goalDistance == bestGoalDistance && travelDistance > bestTravelDistance))
                {
                    bestCoord = neighbor;
                    bestGoalDistance = goalDistance;
                    bestTravelDistance = travelDistance;
                }

                frontier.Enqueue(neighbor);
            }
        }

        if (bestCoord == start)
        {
            path.Add(start);
            return path;
        }

        return ReconstructPath(start, bestCoord, parents);
    }

    private IEnumerable<HexCoord> GetPathNeighbors(
        HexUnit mover,
        HexCoord start,
        HexCoord origin,
        HexCoord goal,
        bool stopAdjacentToGoal,
        bool allowFriendlyWaitingOccupants)
    {
        var candidates = new List<HexCoord>();
        foreach (var direction in NeighborDirections)
        {
            var candidate = origin + direction;
            if (!cells.TryGetValue(candidate, out var candidateCell))
            {
                continue;
            }

            if (!CanTraversePathCell(mover, start, origin, candidateCell, goal, stopAdjacentToGoal, allowFriendlyWaitingOccupants))
            {
                continue;
            }

            candidates.Add(candidate);
        }

        candidates.Sort((left, right) =>
        {
            var leftPriority = GetPathCellPriority(mover, cells[left], goal, allowFriendlyWaitingOccupants);
            var rightPriority = GetPathCellPriority(mover, cells[right], goal, allowFriendlyWaitingOccupants);
            if (leftPriority != rightPriority)
            {
                return leftPriority.CompareTo(rightPriority);
            }

            var leftGoalDistance = HexDistance(left, goal);
            var rightGoalDistance = HexDistance(right, goal);
            if (leftGoalDistance != rightGoalDistance)
            {
                return leftGoalDistance.CompareTo(rightGoalDistance);
            }

            var leftCenterBias = HexDistance(left, new HexCoord(0, 0));
            var rightCenterBias = HexDistance(right, new HexCoord(0, 0));
            if (leftCenterBias != rightCenterBias)
            {
                return leftCenterBias.CompareTo(rightCenterBias);
            }

            if (left.Q != right.Q)
            {
                return left.Q.CompareTo(right.Q);
            }

            return left.R.CompareTo(right.R);
        });

        return candidates;
    }

    private bool CanTraversePathCell(
        HexUnit mover,
        HexCoord start,
        HexCoord origin,
        HexCell candidateCell,
        HexCoord goal,
        bool stopAdjacentToGoal,
        bool allowFriendlyWaitingOccupants)
    {
        if (candidateCell == null)
        {
            return false;
        }

        if (stopAdjacentToGoal && candidateCell.Coord == goal)
        {
            return false;
        }

        var occupant = candidateCell.Occupant;
        if (occupant == null || occupant == mover)
        {
            return true;
        }

        if (occupant.Team != mover.Team)
        {
            return false;
        }

        if (allowFriendlyWaitingOccupants || candidateCell.Coord == goal)
        {
            return true;
        }

        var isImmediateStep = origin == start;
        if (isImmediateStep && HasPendingMoveStep(occupant))
        {
            return true;
        }

        if (!HasPendingMoveStep(occupant))
        {
            return false;
        }

        return false;
    }

    private int GetPathCellPriority(HexUnit mover, HexCell candidateCell, HexCoord goal, bool allowFriendlyWaitingOccupants)
    {
        var occupant = candidateCell.Occupant;
        if (occupant == null || occupant == mover)
        {
            return 0;
        }

        if (occupant.Team != mover.Team)
        {
            return 3;
        }

        if (HasPendingMoveStep(occupant))
        {
            return 1;
        }

        return allowFriendlyWaitingOccupants ? 2 : 3;
    }

    private static bool IsPathGoalReached(HexCoord coord, HexCoord goal, bool stopAdjacentToGoal)
    {
        return stopAdjacentToGoal ? AreAdjacent(coord, goal) : coord == goal;
    }

    private static int GetPathGoalDistance(HexCoord coord, HexCoord goal, bool stopAdjacentToGoal)
    {
        var distance = HexDistance(coord, goal);
        return stopAdjacentToGoal ? Mathf.Max(0, distance - 1) : distance;
    }

    private static List<HexCoord> ReconstructPath(HexCoord start, HexCoord end, Dictionary<HexCoord, HexCoord> parents)
    {
        var path = new List<HexCoord> { end };
        var current = end;
        while (current != start)
        {
            current = parents[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private MoveStepResolution EvaluateMoveTurn()
    {
        var intents = CollectMoveIntents();
        if (intents.Count == 0)
        {
            return new MoveStepResolution(intents, new Dictionary<HexUnit, HexCoord>(), 0, new Dictionary<HexUnit, HashSet<HexUnit>>());
        }

        var winners = ChooseMoveWinners(intents, out var conflictCount);
        var successfulMoves = FilterSuccessfulMoves(winners);
        return new MoveStepResolution(intents, successfulMoves, conflictCount, new Dictionary<HexUnit, HashSet<HexUnit>>());
    }

    private Dictionary<HexUnit, HashSet<HexUnit>> CollectEnemyMoveContacts(List<MoveIntent> intents)
    {
        var contacts = new Dictionary<HexUnit, HashSet<HexUnit>>();
        var groupedByTarget = new Dictionary<HexCoord, List<HexUnit>>();

        foreach (var intent in intents)
        {
            if (!groupedByTarget.TryGetValue(intent.Target, out var contenders))
            {
                contenders = new List<HexUnit>();
                groupedByTarget.Add(intent.Target, contenders);
            }

            contenders.Add(intent.Unit);
        }

        foreach (var pair in groupedByTarget)
        {
            for (var i = 0; i < pair.Value.Count; i++)
            {
                for (var j = i + 1; j < pair.Value.Count; j++)
                {
                    if (pair.Value[i].Team == pair.Value[j].Team)
                    {
                        continue;
                    }

                    RegisterContact(contacts, pair.Value[i], pair.Value[j]);
                }
            }
        }

        foreach (var intent in intents)
        {
            if (!cells.TryGetValue(intent.Target, out var targetCell) || targetCell.Occupant == null)
            {
                continue;
            }

            if (targetCell.Occupant.Team == intent.Unit.Team)
            {
                continue;
            }

            RegisterContact(contacts, intent.Unit, targetCell.Occupant);
        }

        return contacts;
    }

    private void RegisterContact(Dictionary<HexUnit, HashSet<HexUnit>> contacts, HexUnit left, HexUnit right)
    {
        if (!contacts.TryGetValue(left, out var leftTargets))
        {
            leftTargets = new HashSet<HexUnit>();
            contacts.Add(left, leftTargets);
        }

        if (!contacts.TryGetValue(right, out var rightTargets))
        {
            rightTargets = new HashSet<HexUnit>();
            contacts.Add(right, rightTargets);
        }

        leftTargets.Add(right);
        rightTargets.Add(left);
    }

    private List<AttackEvent> BuildContactAttackEvents(Dictionary<HexUnit, HashSet<HexUnit>> contacts)
    {
        var events = new List<AttackEvent>();
        foreach (var pair in contacts)
        {
            var attacker = pair.Key;
            if (!units.Contains(attacker) || attacker.CurrentHealth <= 0 || attacker.AttackConsumed)
            {
                continue;
            }

            HexUnit bestTarget = null;
            var bestScore = int.MinValue;
            foreach (var candidate in pair.Value)
            {
                if (!units.Contains(candidate) || candidate.CurrentHealth <= 0)
                {
                    continue;
                }

                var score = candidate.AttackPower * 10 - candidate.CurrentHealth;
                if (candidate.CurrentHealth <= attacker.AttackPower)
                {
                    score += 50;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null)
            {
                events.Add(new AttackEvent(attacker, bestTarget));
            }
        }

        return events;
    }

    private void LockUnitsForEnemyContact(Dictionary<HexUnit, HashSet<HexUnit>> contacts)
    {
        foreach (var pair in contacts)
        {
            pair.Key.MovementLocked = true;
        }
    }

    private Dictionary<HexUnit, HexCoord> ChooseMoveWinners(List<MoveIntent> intents, out int conflictCount)
    {
        var grouped = new Dictionary<HexCoord, List<HexUnit>>();
        foreach (var intent in intents)
        {
            if (!grouped.TryGetValue(intent.Target, out var contenders))
            {
                contenders = new List<HexUnit>();
                grouped.Add(intent.Target, contenders);
            }

            contenders.Add(intent.Unit);
        }

        var winners = new Dictionary<HexUnit, HexCoord>();
        conflictCount = 0;

        foreach (var pair in grouped)
        {
            if (pair.Value.Count > 1)
            {
                conflictCount++;
            }

            var winner = pair.Value[Random.Range(0, pair.Value.Count)];
            winners[winner] = pair.Key;
        }

        return winners;
    }

    private Dictionary<HexUnit, HexCoord> FilterSuccessfulMoves(Dictionary<HexUnit, HexCoord> winners)
    {
        var movable = new HashSet<HexUnit>(winners.Keys);
        var changed = true;

        while (changed)
        {
            changed = false;
            var snapshot = new List<HexUnit>(movable);
            foreach (var unit in snapshot)
            {
                var target = winners[unit];
                var occupant = cells[target].Occupant;
                if (occupant == null)
                {
                    continue;
                }

                if (!movable.Contains(occupant))
                {
                    movable.Remove(unit);
                    changed = true;
                }
            }
        }

        var successful = new Dictionary<HexUnit, HexCoord>();
        foreach (var unit in movable)
        {
            successful[unit] = winners[unit];
        }

        return successful;
    }

    private IEnumerator AnimateMoveTurn(Dictionary<HexUnit, HexCoord> moves)
    {
        if (moves.Count == 0)
        {
            yield return new WaitForSeconds(moveDuration * 0.6f);
            yield break;
        }

        var startPositions = new Dictionary<HexUnit, Vector3>();
        foreach (var pair in moves)
        {
            startPositions[pair.Key] = pair.Key.Transform.localPosition;
            SetUnitMoving(pair.Key, unitsRoot.TransformPoint(CellToUnitPosition(pair.Value)));
        }

        var elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / moveDuration);
            var curvedT = Mathf.SmoothStep(0f, 1f, t);

            foreach (var pair in moves)
            {
                var start = startPositions[pair.Key];
                var target = CellToUnitPosition(pair.Value);
                FaceUnitTowards(pair.Key, unitsRoot.TransformPoint(target), immediate: false);
                var position = Vector3.Lerp(start, target, curvedT);
                position.y += Mathf.Sin(curvedT * Mathf.PI) * 0.15f;
                pair.Key.Transform.localPosition = position;
            }

            yield return null;
        }

        foreach (var pair in moves)
        {
            pair.Key.Transform.localPosition = CellToUnitPosition(pair.Value);
            SetUnitIdle(pair.Key);
        }
    }

    private void ApplyMoveResults(Dictionary<HexUnit, HexCoord> moves)
    {
        foreach (var pair in moves)
        {
            if (cells.TryGetValue(pair.Key.Coord, out var sourceCell) && sourceCell.Occupant == pair.Key)
            {
                sourceCell.Occupant = null;
            }
        }

        foreach (var pair in moves)
        {
            pair.Key.Coord = pair.Value;
        }

        foreach (var pair in moves)
        {
            if (cells.TryGetValue(pair.Value, out var destinationCell))
            {
                destinationCell.Occupant = pair.Key;
            }
        }
    }

    private void AdvanceMovePaths(Dictionary<HexUnit, HexCoord> moves)
    {
        foreach (var pair in moves)
        {
            pair.Key.PlannedMoveProgress = Mathf.Min(pair.Key.PlannedMoveProgress + 1, pair.Key.MoveRange);
        }
    }

    private IEnumerator AnimateUnitDefeat(List<HexUnit> defeatedUnits)
    {
        var startScales = new Dictionary<HexUnit, Vector3>();
        foreach (var unit in defeatedUnits)
        {
            startScales[unit] = unit.Transform.localScale;
            PlayUnitDeath(unit);
        }

        yield return new WaitForSeconds(0.22f);

        const float defeatDuration = 0.16f;
        var elapsed = 0f;
        while (elapsed < defeatDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / defeatDuration);
            foreach (var pair in startScales)
            {
                pair.Key.Transform.localScale = Vector3.Lerp(pair.Value, Vector3.zero, t);
            }

            yield return null;
        }

        foreach (var pair in startScales)
        {
            pair.Key.Transform.localScale = Vector3.zero;
        }
    }

    private void RemoveUnit(HexUnit unit)
    {
        if (cells.TryGetValue(unit.Coord, out var source))
        {
            source.Occupant = null;
        }

        var collidersToRemove = new List<Collider>();
        foreach (var pair in unitLookups)
        {
            if (pair.Value == unit)
            {
                collidersToRemove.Add(pair.Key);
            }
        }

        foreach (var collider in collidersToRemove)
        {
            unitLookups.Remove(collider);
        }

        units.Remove(unit);

        if (Application.isPlaying)
        {
            Destroy(unit.Transform.gameObject);
        }
        else
        {
            DestroyImmediate(unit.Transform.gameObject);
        }
    }

    private bool CheckVictory()
    {
        var blueCount = CountAliveUnits(Team.Blue);
        var redCount = CountAliveUnits(Team.Red);

        if (blueCount <= 0)
        {
            winningTeam = Team.Red;
        }
        else if (redCount <= 0)
        {
            winningTeam = Team.Blue;
        }

        return winningTeam.HasValue;
    }

    private int CountAliveUnits(Team team)
    {
        var count = 0;
        foreach (var unit in units)
        {
            if (unit.Team == team)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsValidRosterIndex(int rosterIndex)
    {
        return rosterIndex >= 0 && rosterIndex < characterRoster.Count;
    }

    private int GetTeamCost(List<int> selection)
    {
        var total = 0;
        foreach (var rosterIndex in selection)
        {
            if (IsValidRosterIndex(rosterIndex))
            {
                total += characterRoster[rosterIndex].cost;
            }
        }

        return total;
    }

    private bool TryAssignMoveCommand(HexUnit unit, HexCoord target)
    {
        if (!CanSelectMoveTarget(unit, target))
        {
            return false;
        }

        unit.HasPlannedMove = true;
        unit.HasPlannedAttack = false;
        unit.PlannedMoveTarget = target;
        unit.PlannedEnemyTargetUnit = null;
        unit.PlannedAttackTarget = unit.Coord;
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
        return true;
    }

    private void TryAssignAttackCommand(HexUnit unit, HexCoord target)
    {
        if (!cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null || targetCell.Occupant.Team == unit.Team)
        {
            return;
        }

        unit.HasPlannedMove = true;
        unit.HasPlannedAttack = true;
        unit.PlannedMoveTarget = unit.Coord;
        unit.PlannedEnemyTargetUnit = targetCell.Occupant;
        unit.PlannedAttackTarget = targetCell.Occupant.Coord;
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
    }

    private void AssignWaitCommand(HexUnit unit)
    {
        unit.HasPlannedMove = false;
        unit.HasPlannedAttack = false;
        unit.PlannedMoveTarget = unit.Coord;
        unit.PlannedAttackTarget = unit.Coord;
        unit.PlannedEnemyTargetUnit = null;
        unit.PlannedAttackTiming = AttackTiming.BeforeMove;
        unit.PlannedPath.Clear();
        unit.PlannedMoveProgress = 0;
        unit.AttackConsumed = false;
        unit.MovementLocked = false;
    }

    private List<HexCell> GetMoveOptions(HexUnit unit)
    {
        var options = new List<HexCell>();
        if (unit == null)
        {
            return options;
        }

        foreach (var cell in cells.Values)
        {
            if (CanSelectMoveTarget(unit, cell.Coord))
            {
                options.Add(cell);
            }
        }

        return options;
    }

    private List<HexCell> GetAttackOptions(HexUnit unit)
    {
        var options = new List<HexCell>();
        if (unit == null)
        {
            return options;
        }

        foreach (var cell in cells.Values)
        {
            if (cell.Occupant != null && cell.Occupant.Team != unit.Team && CanSelectAttackTarget(unit, cell.Coord))
            {
                options.Add(cell);
            }
        }

        return options;
    }

    private bool CanSelectMoveTarget(HexUnit unit, HexCoord target)
    {
        if (unit == null || !cells.TryGetValue(target, out var targetCell))
        {
            return false;
        }

        if (target == unit.Coord)
        {
            return false;
        }

        if (HexDistance(unit.Coord, target) > unit.MoveRange)
        {
            return false;
        }

        if (targetCell.Occupant != null && targetCell.Occupant.Team != unit.Team)
        {
            return false;
        }

        return true;
    }

    private bool CanSelectAttackTarget(HexUnit unit, HexCoord target)
    {
        if (unit == null || !cells.TryGetValue(target, out var targetCell) || targetCell.Occupant == null)
        {
            return false;
        }

        if (targetCell.Occupant.Team == unit.Team)
        {
            return false;
        }

        return true;
    }

    private void ClearMoveCommand(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void ClearAttackCommand(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void ToggleAttackTiming(HexUnit unit)
    {
        AssignWaitCommand(unit);
    }

    private void SanitizeAttackPlan(HexUnit unit)
    {
        if (unit != null && IsEnemyCommand(unit) && !IsUnitAlive(unit.PlannedEnemyTargetUnit))
        {
            AssignWaitCommand(unit);
        }
    }

    private bool HasPendingMoveStep(HexUnit unit)
    {
        if (unit == null || !unit.HasPlannedMove || unit.MovementLocked || unit.PlannedMoveProgress < 0 || unit.PlannedMoveProgress >= unit.MoveRange)
        {
            return false;
        }

        if (IsEnemyCommand(unit))
        {
            return IsUnitAlive(unit.PlannedEnemyTargetUnit) && HexDistance(unit.Coord, unit.PlannedEnemyTargetUnit.Coord) > 1;
        }

        return unit.Coord != unit.PlannedMoveTarget;
    }

    private bool HasPendingMoveSteps()
    {
        foreach (var unit in units)
        {
            if (HasPendingMoveStep(unit))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshVisuals()
    {
        foreach (var cell in cells.Values)
        {
            var material = cell.BaseMaterial;
            if (currentFlowState == FlowState.Planning)
            {
                if (selectedUnit != null && cell.Coord == selectedUnit.Coord)
                {
                    material = tileSelectedMaterial;
                }
                else if (IsAttackOption(cell.Coord))
                {
                    material = tileAttackMaterial;
                }
                else if (IsMoveOption(cell.Coord))
                {
                    material = tileMoveMaterial;
                }
            }

            cell.Renderer.sharedMaterial = material;
        }

        foreach (var unit in units)
        {
            if (currentFlowState == FlowState.Planning && unit == selectedUnit)
            {
                unit.RingRenderer.sharedMaterial = selectedRingMaterial;
            }
            else if (currentFlowState == FlowState.Planning && unit.Team == Team.Blue && (unit.HasPlannedMove || unit.HasPlannedAttack))
            {
                unit.RingRenderer.sharedMaterial = plannedRingMaterial;
            }
            else
            {
                unit.RingRenderer.sharedMaterial = unit.DefaultRingMaterial;
            }
        }

        if (isAnimating)
        {
            return;
        }

        foreach (var unit in units)
        {
            if (currentFlowState == FlowState.Planning && unit.HasPlannedMove)
            {
                var targetPoint = IsEnemyCommand(unit) && IsUnitAlive(unit.PlannedEnemyTargetUnit)
                    ? unit.PlannedEnemyTargetUnit.Transform.position
                    : unitsRoot.TransformPoint(CellToUnitPosition(unit.PlannedMoveTarget));
                FaceUnitTowards(unit, targetPoint, immediate: true);
            }

            if (unit.CurrentHealth > 0)
            {
                SetUnitIdle(unit);
            }
        }
    }

    private Vector3 CellToUnitPosition(HexCoord coord)
    {
        var center = HexToWorld(coord);
        return center + new Vector3(0f, tileHeight * 0.5f + unitHoverHeight, 0f);
    }

    private Vector3 HexToWorld(HexCoord coord)
    {
        var x = hexRadius * Mathf.Sqrt(3f) * (coord.Q + coord.R * 0.5f);
        var z = hexRadius * 1.5f * coord.R;
        return new Vector3(x, 0f, z);
    }

    private List<Vector3> CollectCameraFramingPoints()
    {
        var points = new List<Vector3>(cells.Count * 7 + units.Count * 5);
        var boardTopHeight = tileHeight * 0.5f + unitHoverHeight + baseUnitVisualHeight * 1.25f;

        foreach (var cell in cells.Values)
        {
            var center = HexToWorld(cell.Coord);
            points.Add(center);
            points.Add(center + Vector3.up * boardTopHeight);

            for (var i = 0; i < 6; i++)
            {
                var radians = (60f * i + 30f) * Mathf.Deg2Rad;
                var cornerOffset = new Vector3(Mathf.Cos(radians) * hexRadius, 0f, Mathf.Sin(radians) * hexRadius);
                points.Add(center + cornerOffset);
            }
        }

        foreach (var unit in units)
        {
            var center = unit.Transform.position;
            var height = Mathf.Max(baseUnitVisualHeight, unit.VisualHeight);
            var radius = Mathf.Max(hexRadius * 0.22f, unit.SelectionRadius);
            var midHeight = height * 0.55f;

            points.Add(center + Vector3.up * height);
            points.Add(center + new Vector3(radius, midHeight, 0f));
            points.Add(center + new Vector3(-radius, midHeight, 0f));
            points.Add(center + new Vector3(0f, midHeight, radius));
            points.Add(center + new Vector3(0f, midHeight, -radius));
        }

        return points;
    }

    private void DestroyChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void ReleaseGeneratedAssets()
    {
        if (cellMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(cellMesh);
            }
            else
            {
                DestroyImmediate(cellMesh);
            }

            cellMesh = null;
        }

        foreach (var material in runtimeMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }

        runtimeMaterials.Clear();
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        switch (currentFlowState)
        {
            case FlowState.ModeSelect:
                DrawModeSelectUi();
                break;
            case FlowState.TeamBuilder:
                DrawTeamBuilderUi();
                break;
            case FlowState.Planning:
                DrawPlanningHud();
                DrawWorldLabels();
                break;
            case FlowState.Resolving:
                DrawResolvingHud();
                DrawWorldLabels();
                break;
            case FlowState.Victory:
                DrawResolvingHud();
                DrawWorldLabels();
                DrawVictoryOverlay();
                break;
        }
    }

    private void EnsureGuiStyles()
    {
        if (centeredLabelStyle == null)
        {
            centeredLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }

        if (worldLabelStyle == null)
        {
            worldLabelStyle = new GUIStyle(centeredLabelStyle)
            {
                fontSize = 11
            };
        }

        if (titleLabelStyle == null)
        {
            titleLabelStyle = new GUIStyle(centeredLabelStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }
    }

    private void DrawModeSelectUi()
    {
        var panel = new Rect(Screen.width * 0.5f - 220f, 56f, 440f, 220f);

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, panel.width - 40f, 28f), "六方向战棋原型", titleLabelStyle);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 62f, panel.width - 40f, 24f), "模式选择");
        GUI.Label(new Rect(panel.x + 20f, panel.y + 92f, panel.width - 40f, 40f), "当前提供“对战 CPU”模式。先配置队伍，再进入同步指令结算战斗。");

        if (GUI.Button(new Rect(panel.x + 120f, panel.y + 150f, 200f, 38f), "对战 CPU"))
        {
            StartCpuMode();
        }
    }

    private void DrawTeamBuilderUi()
    {
        var leftPanel = new Rect(24f, 18f, 470f, Screen.height - 36f);
        var rightPanel = new Rect(510f, 18f, Screen.width - 534f, Screen.height - 36f);

        GUI.color = new Color(0f, 0f, 0f, 0.56f);
        GUI.Box(leftPanel, GUIContent.none);
        GUI.Box(rightPanel, GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(leftPanel.x + 18f, leftPanel.y + 18f, 320f, 24f), "角色选择", titleLabelStyle);
        GUI.Label(new Rect(leftPanel.x + 18f, leftPanel.y + 50f, 420f, 22f), $"玩家总 cost 上限：{playerTeamCostLimit}    CPU 总 cost 上限：{cpuTeamCostLimit}");
        GUI.Label(new Rect(leftPanel.x + 18f, leftPanel.y + 72f, 420f, 22f), $"当前已选：{playerTeamSelection.Count}    同角色可重复选择，直到 cost 用尽");

        var rowY = leftPanel.y + 108f;
        for (var i = 0; i < characterRoster.Count; i++)
        {
            DrawRosterRow(leftPanel.x + 16f, rowY, leftPanel.width - 32f, i);
            rowY += 84f;
        }

        GUI.Label(new Rect(rightPanel.x + 18f, rightPanel.y + 18f, rightPanel.width - 36f, 24f), "我的队伍", titleLabelStyle);
        GUI.Label(new Rect(rightPanel.x + 18f, rightPanel.y + 52f, rightPanel.width - 36f, 22f), $"已用 cost：{GetTeamCost(playerTeamSelection)} / {playerTeamCostLimit}");
        GUI.Label(new Rect(rightPanel.x + 18f, rightPanel.y + 74f, rightPanel.width - 36f, 22f), "CPU 会按同一套角色池自动组队");

        var selectedRowY = rightPanel.y + 112f;
        if (playerTeamSelection.Count == 0)
        {
            GUI.Label(new Rect(rightPanel.x + 18f, selectedRowY, rightPanel.width - 36f, 22f), "还没有选择任何角色");
        }
        else
        {
            for (var i = 0; i < playerTeamSelection.Count; i++)
            {
                DrawSelectedRosterRow(rightPanel.x + 16f, selectedRowY, rightPanel.width - 32f, i);
                selectedRowY += 56f;
            }
        }

        GUI.color = new Color(1f, 0.90f, 0.72f);
        GUI.Label(new Rect(rightPanel.x + 18f, rightPanel.yMax - 102f, rightPanel.width - 36f, 44f), builderStatus);
        GUI.color = Color.white;

        if (GUI.Button(new Rect(rightPanel.x + 18f, rightPanel.yMax - 54f, 130f, 34f), "返回模式"))
        {
            ReturnToModeSelect();
        }

        GUI.enabled = playerTeamSelection.Count > 0 && GetTeamCost(playerTeamSelection) <= playerTeamCostLimit;
        if (GUI.Button(new Rect(rightPanel.xMax - 158f, rightPanel.yMax - 54f, 140f, 34f), "开始对战"))
        {
            TryStartCpuBattle();
        }

        GUI.enabled = true;
    }

    private void DrawRosterRow(float x, float y, float width, int rosterIndex)
    {
        var definition = characterRoster[rosterIndex];
        var canAdd = GetTeamCost(playerTeamSelection) + definition.cost <= playerTeamCostLimit;

        GUI.color = new Color(0f, 0f, 0f, 0.22f);
        GUI.Box(new Rect(x, y, width, 72f), GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + 12f, y + 8f, width - 140f, 22f), $"{definition.displayName}  [{definition.description}]");
        GUI.Label(new Rect(x + 12f, y + 30f, width - 140f, 22f), $"HP {definition.maxHealth}  ATK {definition.attackPower}  MOVE {definition.moveRange}  COST {definition.cost}");
        GUI.Label(new Rect(x + 12f, y + 50f, width - 140f, 18f), "可重复加入队伍，只受总 cost 限制");

        GUI.enabled = canAdd;
        if (GUI.Button(new Rect(x + width - 104f, y + 18f, 88f, 32f), "加入"))
        {
            TryAddCharacterToPlayerTeam(rosterIndex);
        }

        GUI.enabled = true;
    }

    private void DrawSelectedRosterRow(float x, float y, float width, int selectionIndex)
    {
        var rosterIndex = playerTeamSelection[selectionIndex];
        var definition = characterRoster[rosterIndex];

        GUI.color = new Color(0f, 0f, 0f, 0.20f);
        GUI.Box(new Rect(x, y, width, 46f), GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + 12f, y + 6f, width - 110f, 18f), $"{selectionIndex + 1}. {definition.displayName}");
        GUI.Label(new Rect(x + 12f, y + 24f, width - 110f, 18f), $"HP {definition.maxHealth}  ATK {definition.attackPower}  MOVE {definition.moveRange}  COST {definition.cost}");

        if (GUI.Button(new Rect(x + width - 92f, y + 8f, 76f, 28f), "移除"))
        {
            RemovePlayerCharacterAt(selectionIndex);
        }
    }

    private void DrawPlanningHud()
    {
        var blueUnitCount = CountAliveUnits(Team.Blue);
        var panelX = 18f;
        var panelY = 18f;
        var panelWidth = 530f;
        var labelX = panelX + 14f;
        var labelWidth = panelWidth - 32f;
        var commandStartY = panelY + 156f;
        var listedRows = Mathf.Max(1, blueUnitCount);
        var buttonY = commandStartY + listedRows * 28f + 10f;
        var panelHeight = Mathf.Min(Screen.height - 36f, buttonY - panelY + 42f);

        GUI.color = new Color(0f, 0f, 0f, 0.48f);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(labelX, panelY + 12f, labelWidth, 24f), $"第 {planningRoundNumber} 轮计划  |  已同步结算 {resolvedTurnCount} 个回合");
        GUI.Label(new Rect(labelX, panelY + 36f, labelWidth, 22f), $"蓝方剩余 {blueUnitCount} 名  |  红方剩余 {CountAliveUnits(Team.Red)} 名");
        GUI.Label(new Rect(labelX, panelY + 60f, labelWidth, 22f), "先为所有蓝方棋子下达指令，再确认同步结算");
        GUI.Label(new Rect(labelX, panelY + 82f, labelWidth, 22f), "每名棋子一轮只设置 1 次命令：点任意格子是移动命令，点任意敌人是追击命令");
        GUI.Label(new Rect(labelX, panelY + 104f, labelWidth, 22f), "范围外目标也能指定；本轮到不了时会尽量接近。追击命令只攻击指定敌人，若最终贴身则攻击；移动命令起手贴身会先自动攻击");

        var selectedLabel = selectedUnit == null
            ? "当前未选择棋子"
            : $"当前选择：{selectedUnit.RoleName}  HP {selectedUnit.CurrentHealth}/{selectedUnit.MaxHealth}  ATK {selectedUnit.AttackPower}  MOVE {selectedUnit.MoveRange}";
        GUI.Label(new Rect(labelX, panelY + 128f, labelWidth, 22f), selectedLabel);

        var columnX = labelX;
        var rowY = commandStartY;
        foreach (var unit in units)
        {
            if (unit.Team != Team.Blue)
            {
                continue;
            }

            DrawPlannedCommandRow(columnX, rowY, 480f, unit);
            rowY += 28f;
        }

        if (blueUnitCount == 0)
        {
            GUI.Label(new Rect(columnX, rowY, 480f, 22f), "当前没有可下令的蓝方棋子");
        }

        if (GUI.Button(new Rect(labelX, buttonY, 110f, 28f), "清空选择"))
        {
            SelectUnit(null);
        }

        GUI.enabled = selectedUnit != null;
        if (GUI.Button(new Rect(labelX + 122f, buttonY, 110f, 28f), "当前待机"))
        {
            SetUnitWaitCommand(selectedUnit);
        }

        GUI.enabled = true;
        if (GUI.Button(new Rect(panelX + panelWidth - 124f, buttonY, 110f, 28f), "确认指令"))
        {
            TryResolvePlanningRound();
        }
    }

    private void DrawPlannedCommandRow(float x, float y, float width, HexUnit unit)
    {
        var selectLabel = selectedUnit == unit ? "已选中" : "选择";
        GUI.Label(new Rect(x, y, width - 180f, 22f), $"{unit.Name} -> {DescribeCommand(unit, false)}");

        if (GUI.Button(new Rect(x + width - 170f, y - 2f, 70f, 24f), selectLabel))
        {
            SelectUnit(unit);
        }

        if (GUI.Button(new Rect(x + width - 88f, y - 2f, 70f, 24f), "待机"))
        {
            SetUnitWaitCommand(unit);
        }
    }

    private void DrawResolvingHud()
    {
        var panelX = 18f;
        var panelY = 18f;
        var panelWidth = 500f;

        GUI.color = new Color(0f, 0f, 0f, 0.46f);
        GUI.Box(new Rect(panelX, panelY, panelWidth, 176f), GUIContent.none);
        GUI.color = Color.white;

        var turnTypeLabel = lastResolutionKind == ResolutionKind.Attack ? "攻击回合" : "移动回合";
        if (lastResolutionKind == ResolutionKind.None)
        {
            turnTypeLabel = "准备中";
        }

        GUI.Label(new Rect(panelX + 14f, panelY + 12f, 452f, 24f), $"第 {planningRoundNumber} 轮同步结算  |  {turnTypeLabel}");
        GUI.Label(new Rect(panelX + 14f, panelY + 36f, 452f, 22f), $"蓝方剩余 {CountAliveUnits(Team.Blue)} 名  |  红方剩余 {CountAliveUnits(Team.Red)} 名");
        GUI.Label(new Rect(panelX + 14f, panelY + 62f, 452f, 44f), resolutionStatus);
        GUI.Label(new Rect(panelX + 14f, panelY + 110f, 452f, 22f), "范围外的格子或敌人也能作为目标；本轮会按最短路尽量接近，追击只有在最终贴到指定敌人时才会攻击");
        GUI.Label(new Rect(panelX + 14f, panelY + 132f, 452f, 22f), "同一格抢位会随机判定胜者；若目标格暂时被别的棋子占住，本回合会原地等待，等后续空间腾出再继续尝试");
    }

    private void DrawVictoryOverlay()
    {
        var panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 96f, 440f, 192f);

        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, panel.width - 40f, 28f), "战斗结束", titleLabelStyle);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 66f, panel.width - 40f, 24f), $"{TeamDisplayName(winningTeam ?? Team.Blue)}完成歼灭");
        GUI.Label(new Rect(panel.x + 20f, panel.y + 94f, panel.width - 40f, 22f), "胜利条件：一方所有棋子被击败");

        if (GUI.Button(new Rect(panel.x + 40f, panel.y + 136f, 150f, 34f), "返回编队"))
        {
            ReturnToTeamBuilder();
        }

        if (GUI.Button(new Rect(panel.x + panel.width - 190f, panel.y + 136f, 150f, 34f), "再次挑战"))
        {
            TryStartCpuBattle();
        }
    }

    private void DrawWorldLabels()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        foreach (var unit in units)
        {
            var worldPoint = unit.Transform.position + new Vector3(0f, unit.LabelHeight, 0f);
            var screenPoint = mainCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                continue;
            }

            var x = screenPoint.x - 74f;
            var y = Screen.height - screenPoint.y - 18f;

            GUI.color = new Color(0f, 0f, 0f, 0.50f);
            GUI.Box(new Rect(x, y, 148f, 38f), GUIContent.none);

            GUI.color = unit.Team == Team.Blue
                ? new Color(0.72f, 0.88f, 1.00f)
                : new Color(1.00f, 0.83f, 0.72f);

            GUI.Label(new Rect(x + 4f, y + 3f, 140f, 14f), unit.Name, worldLabelStyle);

            var detail = currentFlowState == FlowState.Planning
                ? DescribeCommand(unit, true)
                : $"HP {unit.CurrentHealth}/{unit.MaxHealth}  ATK {unit.AttackPower}  MOVE {unit.MoveRange}";
            GUI.Label(new Rect(x + 4f, y + 18f, 140f, 14f), detail, worldLabelStyle);
        }

        GUI.color = Color.white;
    }

    private string DescribeCommand(HexUnit unit, bool compact)
    {
        if (!unit.HasPlannedMove)
        {
            return "待机";
        }

        if (IsEnemyCommand(unit))
        {
            var targetLabel = IsUnitAlive(unit.PlannedEnemyTargetUnit)
                ? unit.PlannedEnemyTargetUnit.Name
                : $"({unit.PlannedAttackTarget.Q},{unit.PlannedAttackTarget.R})";
            return compact
                ? $"追击 -> {targetLabel}"
                : $"追击目标 {targetLabel}";
        }

        return compact
            ? $"移动 -> ({unit.PlannedMoveTarget.Q},{unit.PlannedMoveTarget.R})"
            : $"移动到 ({unit.PlannedMoveTarget.Q},{unit.PlannedMoveTarget.R})";
    }

    private static Mesh BuildHexPrismMesh(float radius, float height)
    {
        var topY = height * 0.5f;
        var bottomY = -topY;
        var topCorners = new Vector3[6];
        var bottomCorners = new Vector3[6];

        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.Deg2Rad * (60f * i + 30f);
            var corner = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            topCorners[i] = corner + Vector3.up * topY;
            bottomCorners[i] = corner + Vector3.up * bottomY;
        }

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();

        AddFace(vertices, triangles, normals, Vector3.up * topY, topCorners, true);
        AddFace(vertices, triangles, normals, Vector3.up * bottomY, bottomCorners, false);

        for (var i = 0; i < 6; i++)
        {
            var next = (i + 1) % 6;
            AddQuad(vertices, triangles, normals, topCorners[i], topCorners[next], bottomCorners[next], bottomCorners[i]);
        }

        var mesh = new Mesh
        {
            name = $"HexPrism_{radius:0.00}_{height:0.00}"
        };

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 center,
        IReadOnlyList<Vector3> corners,
        bool topFace)
    {
        for (var i = 0; i < corners.Count; i++)
        {
            var next = (i + 1) % corners.Count;
            if (topFace)
            {
                AddTriangle(vertices, triangles, normals, center, corners[next], corners[i]);
            }
            else
            {
                AddTriangle(vertices, triangles, normals, center, corners[i], corners[next]);
            }
        }
    }

    private static void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        AddTriangle(vertices, triangles, normals, a, b, c);
        AddTriangle(vertices, triangles, normals, a, c, d);
    }

    private static void AddTriangle(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a).normalized;
        var start = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
    }

    private static bool AreAdjacent(HexCoord a, HexCoord b)
    {
        foreach (var direction in NeighborDirections)
        {
            if (a + direction == b)
            {
                return true;
            }
        }

        return false;
    }

    private static int HexDistance(HexCoord a, HexCoord b)
    {
        var dq = a.Q - b.Q;
        var dr = a.R - b.R;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
    }

    private static string TeamDisplayName(Team team)
    {
        return team == Team.Blue ? "蓝方" : "红方";
    }

    private sealed class HexCell
    {
        public HexCell(HexCoord coord, MeshRenderer renderer, Material baseMaterial)
        {
            Coord = coord;
            Renderer = renderer;
            BaseMaterial = baseMaterial;
        }

        public HexCoord Coord { get; }
        public MeshRenderer Renderer { get; }
        public Material BaseMaterial { get; }
        public HexUnit Occupant { get; set; }
    }

    private sealed class HexUnit
    {
        public HexUnit(
            string name,
            string roleName,
            Team team,
            HexCoord coord,
            Transform transform,
            MeshRenderer ringRenderer,
            Material defaultRingMaterial,
            int maxHealth,
            int attackPower,
            int cost,
            int moveRange)
        {
            Name = name;
            RoleName = roleName;
            Team = team;
            Coord = coord;
            Transform = transform;
            RingRenderer = ringRenderer;
            DefaultRingMaterial = defaultRingMaterial;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackPower = attackPower;
            Cost = cost;
            MoveRange = moveRange;
            PlannedMoveTarget = coord;
            PlannedAttackTarget = coord;
            PlannedEnemyTargetUnit = null;
            PlannedAttackTiming = AttackTiming.BeforeMove;
        }

        public string Name { get; }
        public string RoleName { get; }
        public Team Team { get; }
        public HexCoord Coord { get; set; }
        public Transform Transform { get; }
        public MeshRenderer RingRenderer { get; }
        public Material DefaultRingMaterial { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; set; }
        public int AttackPower { get; }
        public int Cost { get; }
        public int MoveRange { get; }
        public bool HasPlannedMove { get; set; }
        public HexCoord PlannedMoveTarget { get; set; }
        public bool HasPlannedAttack { get; set; }
        public HexCoord PlannedAttackTarget { get; set; }
        public HexUnit PlannedEnemyTargetUnit { get; set; }
        public AttackTiming PlannedAttackTiming { get; set; }
        public int PlannedMoveProgress { get; set; }
        public bool AttackConsumed { get; set; }
        public bool MovementLocked { get; set; }
        public Transform VisualRoot { get; set; }
        public Animator Animator { get; set; }
        public float VisualHeight { get; set; }
        public float LabelHeight { get; set; }
        public float SelectionRadius { get; set; }
        public List<HexCoord> PlannedPath { get; } = new();
    }

    private readonly struct MoveIntent
    {
        public MoveIntent(HexUnit unit, HexCoord target)
        {
            Unit = unit;
            Target = target;
        }

        public HexUnit Unit { get; }
        public HexCoord Target { get; }
    }

    private readonly struct AttackEvent
    {
        public AttackEvent(HexUnit attacker, HexUnit defender)
        {
            Attacker = attacker;
            Defender = defender;
        }

        public HexUnit Attacker { get; }
        public HexUnit Defender { get; }
    }

    private readonly struct MoveStepResolution
    {
        public MoveStepResolution(
            List<MoveIntent> intents,
            Dictionary<HexUnit, HexCoord> successfulMoves,
            int conflictCount,
            Dictionary<HexUnit, HashSet<HexUnit>> enemyContacts)
        {
            Intents = intents;
            SuccessfulMoves = successfulMoves;
            ConflictCount = conflictCount;
            EnemyContacts = enemyContacts;
        }

        public List<MoveIntent> Intents { get; }
        public Dictionary<HexUnit, HexCoord> SuccessfulMoves { get; }
        public int ConflictCount { get; }
        public Dictionary<HexUnit, HashSet<HexUnit>> EnemyContacts { get; }
        public bool HasEnemyContest => EnemyContacts.Count > 0;
        public bool HasValue => Intents != null;
    }

    [System.Serializable]
    private sealed class CharacterDefinition
    {
        public CharacterDefinition(
            string displayName,
            string description,
            int maxHealth,
            int attackPower,
            int cost,
            int moveRange,
            UnitVisualArchetype visualArchetype)
        {
            this.displayName = displayName;
            this.description = description;
            this.maxHealth = maxHealth;
            this.attackPower = attackPower;
            this.cost = cost;
            this.moveRange = moveRange;
            this.visualArchetype = visualArchetype;
        }

        public string displayName = "角色";
        public string description = "近战";
        [Min(1)] public int maxHealth = 10;
        [Min(1)] public int attackPower = 3;
        [Min(1)] public int cost = 3;
        [Min(1)] public int moveRange = 2;
        public UnitVisualArchetype visualArchetype = UnitVisualArchetype.Stag;
    }

    private enum UnitVisualArchetype
    {
        Stag,
        Doe,
        Elk,
        Fawn,
        Tiger,
        WhiteTiger
    }

    private enum Team
    {
        Blue,
        Red
    }

    private enum FlowState
    {
        ModeSelect,
        TeamBuilder,
        Planning,
        Resolving,
        Victory
    }

    private enum ActionType
    {
        Wait,
        Move,
        Attack
    }

    private enum AttackTiming
    {
        BeforeMove,
        AfterMove
    }

    private enum ResolutionKind
    {
        None,
        Move,
        Attack
    }

    private readonly struct HexCoord : System.IEquatable<HexCoord>
    {
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }

        public static HexCoord operator +(HexCoord left, HexCoord right)
        {
            return new HexCoord(left.Q + right.Q, left.R + right.R);
        }

        public static bool operator ==(HexCoord left, HexCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoord left, HexCoord right)
        {
            return !left.Equals(right);
        }

        public bool Equals(HexCoord other)
        {
            return other.Q == Q && other.R == R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }
    }
}
