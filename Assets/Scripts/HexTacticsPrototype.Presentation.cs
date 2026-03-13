using System.Collections.Generic;
using UnityEngine;

public sealed partial class HexTacticsPrototype
{
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

}
