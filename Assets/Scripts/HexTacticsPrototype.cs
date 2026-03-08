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

    [Header("Gameplay")]
    [SerializeField, Min(0.05f)] private float moveDuration = 0.18f;

    private static readonly HexCoord[] NeighborDirections =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    };

    private readonly Dictionary<HexCoord, HexCell> cells = new();
    private readonly Dictionary<Collider, HexCell> cellLookups = new();
    private readonly Dictionary<Collider, HexUnit> unitLookups = new();
    private readonly List<HexUnit> units = new();
    private readonly List<HexCell> reachableCells = new();
    private readonly List<Material> runtimeMaterials = new();

    private Transform boardRoot;
    private Transform unitsRoot;
    private Mesh cellMesh;
    private Material tilePrimaryMaterial;
    private Material tileSecondaryMaterial;
    private Material tileReachableMaterial;
    private Material tileSelectedMaterial;
    private Material platformMaterial;
    private Material playerBodyMaterial;
    private Material playerRingMaterial;
    private Material enemyBodyMaterial;
    private Material enemyRingMaterial;
    private Material selectedRingMaterial;
    private HexUnit selectedUnit;
    private bool isAnimating;

    private void Awake()
    {
        BuildPrototype();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandlePointerInput();
        }

        if (selectedUnit == null || isAnimating || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(-1, 0));
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(0, -1));
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(1, -1));
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(1, 0));
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(0, 1));
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            TryMoveSelectedUnit(new HexCoord(-1, 1));
        }
    }

    private void OnDestroy()
    {
        ReleaseGeneratedAssets();
    }

    private void BuildPrototype()
    {
        ReleaseGeneratedAssets();
        cells.Clear();
        cellLookups.Clear();
        unitLookups.Clear();
        units.Clear();
        reachableCells.Clear();
        selectedUnit = null;
        isAnimating = false;

        EnsureRoots();
        BuildMaterials();
        cellMesh = BuildHexPrismMesh(hexRadius, tileHeight);

        BuildBoard();
        SpawnUnits();
        ConfigureCamera();
        RefreshVisuals();
    }

    private void EnsureRoots()
    {
        boardRoot = GetOrCreateChild("Board").transform;
        unitsRoot = GetOrCreateChild("Units").transform;
        DestroyChildren(boardRoot);
        DestroyChildren(unitsRoot);
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

    private void BuildMaterials()
    {
        tilePrimaryMaterial = CreateLitMaterial(new Color(0.82f, 0.77f, 0.64f), new Color(0.05f, 0.03f, 0.01f));
        tileSecondaryMaterial = CreateLitMaterial(new Color(0.72f, 0.68f, 0.56f), new Color(0.04f, 0.03f, 0.02f));
        tileReachableMaterial = CreateLitMaterial(new Color(0.44f, 0.88f, 0.72f), new Color(0.10f, 0.25f, 0.18f));
        tileSelectedMaterial = CreateLitMaterial(new Color(1.00f, 0.85f, 0.42f), new Color(0.25f, 0.18f, 0.04f));
        platformMaterial = CreateLitMaterial(new Color(0.23f, 0.29f, 0.25f), new Color(0.02f, 0.03f, 0.02f));
        playerBodyMaterial = CreateLitMaterial(new Color(0.18f, 0.45f, 0.82f), new Color(0.03f, 0.06f, 0.12f));
        playerRingMaterial = CreateLitMaterial(new Color(0.36f, 0.66f, 0.98f), new Color(0.07f, 0.11f, 0.18f));
        enemyBodyMaterial = CreateLitMaterial(new Color(0.82f, 0.30f, 0.26f), new Color(0.11f, 0.04f, 0.03f));
        enemyRingMaterial = CreateLitMaterial(new Color(0.95f, 0.58f, 0.36f), new Color(0.16f, 0.06f, 0.02f));
        selectedRingMaterial = CreateLitMaterial(new Color(1.00f, 0.91f, 0.50f), new Color(0.32f, 0.24f, 0.05f));
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

                var cell = new HexCell(coord, cellObject.transform, renderer, collider, baseMaterial);
                cells.Add(coord, cell);
                cellLookups.Add(collider, cell);
            }
        }
    }

    private void SpawnUnits()
    {
        var playerSpawns = new[]
        {
            new HexCoord(-boardRadius + 1, 0),
            new HexCoord(-boardRadius + 2, -1),
            new HexCoord(-boardRadius + 1, 1)
        };

        var enemySpawns = new[]
        {
            new HexCoord(boardRadius - 1, 0),
            new HexCoord(boardRadius - 2, 1),
            new HexCoord(boardRadius - 1, -1)
        };

        SpawnTeam(playerSpawns, true, "Blue");
        SpawnTeam(enemySpawns, false, "Red");
    }

    private void SpawnTeam(IEnumerable<HexCoord> spawns, bool isPlayer, string teamName)
    {
        var index = 1;
        foreach (var coord in spawns)
        {
            if (!cells.TryGetValue(coord, out var cell) || cell.Occupant != null)
            {
                continue;
            }

            var root = new GameObject($"{teamName} Unit {index}");
            root.transform.SetParent(unitsRoot, false);
            root.transform.localPosition = CellToUnitPosition(coord);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, -unitHoverHeight * 0.55f, 0f);
            ring.transform.localScale = new Vector3(0.58f, 0.16f, 0.58f);
            var ringFilter = ring.AddComponent<MeshFilter>();
            ringFilter.sharedMesh = cellMesh;
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = isPlayer ? playerRingMaterial : enemyRingMaterial;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.62f, 0.55f);
            body.GetComponent<MeshRenderer>().sharedMaterial = isPlayer ? playerBodyMaterial : enemyBodyMaterial;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.52f, 0f);
            head.transform.localScale = Vector3.one * 0.34f;
            head.GetComponent<MeshRenderer>().sharedMaterial = isPlayer ? playerRingMaterial : enemyRingMaterial;

            var unit = new HexUnit(
                $"{teamName} Unit {index}",
                isPlayer,
                coord,
                root.transform,
                ringRenderer,
                isPlayer ? playerRingMaterial : enemyRingMaterial);

            RegisterUnitCollider(body.GetComponent<Collider>(), unit);
            RegisterUnitCollider(head.GetComponent<Collider>(), unit);

            cell.Occupant = unit;
            units.Add(unit);
            index++;
        }
    }

    private void RegisterUnitCollider(Collider collider, HexUnit unit)
    {
        if (collider == null)
        {
            return;
        }

        unitLookups[collider] = unit;
    }

    private void ConfigureCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var focus = new Vector3(0f, tileHeight * 0.5f, 0f);
        var distance = boardRadius * hexRadius * 2.45f;
        var offset = Quaternion.Euler(0f, 32f, 0f) * new Vector3(0f, distance * 0.82f, -distance * 0.94f);

        mainCamera.transform.position = focus + offset;
        mainCamera.transform.LookAt(focus + new Vector3(0f, 0.55f, 0f));
        mainCamera.fieldOfView = 42f;
        mainCamera.backgroundColor = new Color(0.16f, 0.19f, 0.22f);
    }

    private void HandlePointerInput()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null || isAnimating)
        {
            return;
        }

        var pointerPosition = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out var hit, 200f))
        {
            SelectUnit(null);
            return;
        }

        if (unitLookups.TryGetValue(hit.collider, out var clickedUnit) && clickedUnit.IsPlayer)
        {
            SelectUnit(selectedUnit == clickedUnit ? null : clickedUnit);
            return;
        }

        if (selectedUnit != null && cellLookups.TryGetValue(hit.collider, out var clickedCell) && reachableCells.Contains(clickedCell))
        {
            MoveUnit(selectedUnit, clickedCell);
            return;
        }

        if (cellLookups.ContainsKey(hit.collider))
        {
            SelectUnit(null);
        }
    }

    private void SelectUnit(HexUnit unit)
    {
        selectedUnit = unit;
        reachableCells.Clear();

        if (selectedUnit != null)
        {
            foreach (var direction in NeighborDirections)
            {
                var targetCoord = selectedUnit.Coord + direction;
                if (!cells.TryGetValue(targetCoord, out var cell) || cell.Occupant != null)
                {
                    continue;
                }

                reachableCells.Add(cell);
            }
        }

        RefreshVisuals();
    }

    private void TryMoveSelectedUnit(HexCoord direction)
    {
        if (selectedUnit == null)
        {
            return;
        }

        var destinationCoord = selectedUnit.Coord + direction;
        if (!cells.TryGetValue(destinationCoord, out var destination) || destination.Occupant != null)
        {
            return;
        }

        MoveUnit(selectedUnit, destination);
    }

    private void MoveUnit(HexUnit unit, HexCell destination)
    {
        if (!cells.TryGetValue(unit.Coord, out var source))
        {
            return;
        }

        source.Occupant = null;
        destination.Occupant = unit;
        unit.Coord = destination.Coord;
        StartCoroutine(AnimateMove(unit, CellToUnitPosition(destination.Coord)));
    }

    private IEnumerator AnimateMove(HexUnit unit, Vector3 targetPosition)
    {
        isAnimating = true;

        var startPosition = unit.Transform.localPosition;
        var elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / moveDuration);
            var curvedT = Mathf.SmoothStep(0f, 1f, t);
            var position = Vector3.Lerp(startPosition, targetPosition, curvedT);
            position.y += Mathf.Sin(curvedT * Mathf.PI) * 0.18f;
            unit.Transform.localPosition = position;
            yield return null;
        }

        unit.Transform.localPosition = targetPosition;
        isAnimating = false;
        SelectUnit(unit);
    }

    private void RefreshVisuals()
    {
        foreach (var cell in cells.Values)
        {
            var material = cell.BaseMaterial;

            if (selectedUnit != null && cell.Coord == selectedUnit.Coord)
            {
                material = tileSelectedMaterial;
            }
            else if (reachableCells.Contains(cell))
            {
                material = tileReachableMaterial;
            }

            cell.Renderer.sharedMaterial = material;
        }

        foreach (var unit in units)
        {
            unit.RingRenderer.sharedMaterial = unit == selectedUnit ? selectedRingMaterial : unit.DefaultRingMaterial;
        }
    }

    private Vector3 CellToUnitPosition(HexCoord coord)
    {
        var cellCenter = HexToWorld(coord);
        return cellCenter + new Vector3(0f, tileHeight * 0.5f + unitHoverHeight, 0f);
    }

    private Vector3 HexToWorld(HexCoord coord)
    {
        var x = hexRadius * Mathf.Sqrt(3f) * (coord.Q + coord.R * 0.5f);
        var z = hexRadius * 1.5f * coord.R;
        return new Vector3(x, 0f, z);
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
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.Box(new Rect(18f, 18f, 370f, 108f), GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(32f, 30f, 340f, 24f), "Hex Tactics Prototype");
        GUI.Label(new Rect(32f, 54f, 340f, 22f), "鼠标：点击蓝色棋子选择，再点击高亮相邻格移动");
        GUI.Label(new Rect(32f, 74f, 340f, 22f), "键盘：Q / W / E / A / S / D 对应六个方向");

        var selectedLabel = selectedUnit == null
            ? "当前未选择棋子"
            : $"当前选择：{selectedUnit.Name}  位置：({selectedUnit.Coord.Q}, {selectedUnit.Coord.R})";
        GUI.Label(new Rect(32f, 94f, 340f, 22f), selectedLabel);
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

    private sealed class HexCell
    {
        public HexCell(HexCoord coord, Transform transform, MeshRenderer renderer, Collider collider, Material baseMaterial)
        {
            Coord = coord;
            Transform = transform;
            Renderer = renderer;
            Collider = collider;
            BaseMaterial = baseMaterial;
        }

        public HexCoord Coord { get; }
        public Transform Transform { get; }
        public MeshRenderer Renderer { get; }
        public Collider Collider { get; }
        public Material BaseMaterial { get; }
        public HexUnit Occupant { get; set; }
    }

    private sealed class HexUnit
    {
        public HexUnit(string name, bool isPlayer, HexCoord coord, Transform transform, MeshRenderer ringRenderer, Material defaultRingMaterial)
        {
            Name = name;
            IsPlayer = isPlayer;
            Coord = coord;
            Transform = transform;
            RingRenderer = ringRenderer;
            DefaultRingMaterial = defaultRingMaterial;
        }

        public string Name { get; }
        public bool IsPlayer { get; }
        public HexCoord Coord { get; set; }
        public Transform Transform { get; }
        public MeshRenderer RingRenderer { get; }
        public Material DefaultRingMaterial { get; }
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

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }
    }
}
