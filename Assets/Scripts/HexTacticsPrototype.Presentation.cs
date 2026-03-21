using System.Collections.Generic;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
    private void RefreshVisuals()
    {
        foreach (var cell in cells.Values)
        {
            var material = cell.BaseMaterial;
            if (currentFlowState == FlowState.TeamBuilder)
            {
                if (blueDeploySlotLookup.Contains(cell.Coord))
                {
                    material = cell.Occupant != null ? tileSelectedMaterial : tileMoveMaterial;
                }
            }
            else if (currentFlowState == FlowState.Planning)
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
            if (unit.RingRenderer == null)
            {
                continue;
            }

            var showMarker = false;
            if (currentFlowState == FlowState.Planning && unit == selectedUnit)
            {
                unit.RingRenderer.sharedMaterial = selectedRingMaterial;
                showMarker = true;
            }
            else if (currentFlowState == FlowState.Planning && unit.Team == Team.Blue && unit.HasAssignedCommand)
            {
                unit.RingRenderer.sharedMaterial = plannedRingMaterial;
                showMarker = true;
            }
            else
            {
                unit.RingRenderer.sharedMaterial = unit.DefaultRingMaterial;
            }

            unit.RingRenderer.enabled = showMarker;
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
            AddUnitCameraFramingPoints(points, unit);
        }

        return points;
    }

    private void AddUnitCameraFramingPoints(List<Vector3> points, HexUnit unit, float radiusScale = 1f, float heightScale = 1f)
    {
        if (points == null || unit?.Transform == null)
        {
            return;
        }

        var center = unit.Transform.position;
        var height = Mathf.Max(baseUnitVisualHeight, unit.VisualHeight) * Mathf.Max(0.1f, heightScale);
        var radius = Mathf.Max(hexRadius * 0.22f, unit.SelectionRadius * Mathf.Max(0.1f, radiusScale));
        var midHeight = height * 0.55f;

        points.Add(center + Vector3.up * height);
        points.Add(center + new Vector3(radius, midHeight, 0f));
        points.Add(center + new Vector3(-radius, midHeight, 0f));
        points.Add(center + new Vector3(0f, midHeight, radius));
        points.Add(center + new Vector3(0f, midHeight, -radius));
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

    private string DescribeCommand(HexUnit unit, bool compact)
    {
        if (!unit.HasAssignedCommand)
        {
            return compact ? "未设置" : "尚未设置命令";
        }

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

    private static int GetAttackReach(int attackRange)
    {
        return Mathf.Clamp(attackRange, 0, 1) + 1;
    }

    private static int GetAttackReach(HexUnit unit)
    {
        return unit != null ? GetAttackReach(unit.AttackRange) : 1;
    }

    private static bool IsWithinAttackRange(HexCoord origin, HexCoord target, int attackRange)
    {
        // Attack range is evaluated by hex distance, so "corner" directions are
        // counted the same way as straight approaches on the hex board.
        return origin != target && HexDistance(origin, target) <= GetAttackReach(attackRange);
    }

    private static bool IsWithinAttackRange(HexUnit attacker, HexUnit defender)
    {
        return attacker != null &&
               defender != null &&
               IsWithinAttackRange(attacker.Coord, defender.Coord, attacker.AttackRange);
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

}
