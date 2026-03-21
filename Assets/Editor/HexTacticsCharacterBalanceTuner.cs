using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexTacticsCharacterBalanceTuner
{
    private static readonly Dictionary<string, int> MoveRangeByAssetName = new()
    {
        ["StagVanguard"] = 1,
        ["DoeRider"] = 2,
        ["ElkGuardian"] = 1,
        ["FawnScout"] = 2,
        ["TigerFighter"] = 1,
        ["WhiteTigerHunter"] = 2,
        ["Bat"] = 2,
        ["BattleBee"] = 2,
        ["Beholder"] = 1,
        ["BishopKnight"] = 1,
        ["BlackKnight"] = 1,
        ["Cactus"] = 1,
        ["ChestMonster"] = 1,
        ["CrabMonster"] = 1,
        ["Cyclops"] = 1,
        ["DemonKing"] = 1,
        ["Dragon"] = 1,
        ["EvilMage"] = 1,
        ["Fishman"] = 2,
        ["FlyingDemon"] = 2,
        ["Golem"] = 1,
        ["LizardWarrior"] = 1,
        ["MonsterPlant"] = 1,
        ["MushroomAngry"] = 1,
        ["MushroomSmile"] = 1,
        ["NagaWizard"] = 1,
        ["Orc"] = 1,
        ["RatAssassin"] = 2,
        ["Salamander"] = 2,
        ["Skeleton"] = 2,
        ["Slime"] = 1,
        ["Specter"] = 2,
        ["Spider"] = 2,
        ["StingRay"] = 2,
        ["TurtleShell"] = 1,
        ["Werewolf"] = 2,
        ["WormMonster"] = 1
    };

    [MenuItem("Tools/Hex Tactics/Rebalance Character Mobility")]
    public static void RunFromMenu()
    {
        RunInternal(exitOnComplete: false);
    }

    public static void RunBatchMode()
    {
        RunInternal(exitOnComplete: true);
    }

    private static void RunInternal(bool exitOnComplete)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var issues = new List<string>();
        var updatedCount = 0;
        var oneTileCount = 0;
        var twoTileCount = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:HexTacticsCharacterConfig", new[] { HexTacticsAssetPaths.CharacterConfigFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<HexTacticsCharacterConfig>(path);
            if (config == null)
            {
                issues.Add($"Character config could not be loaded: {path}");
                continue;
            }

            var assetName = Path.GetFileNameWithoutExtension(path);
            if (!MoveRangeByAssetName.TryGetValue(assetName, out var targetMoveRange))
            {
                issues.Add($"No mobility balance rule defined for '{assetName}'.");
                continue;
            }

            if (targetMoveRange <= 1)
            {
                oneTileCount++;
            }
            else if (targetMoveRange == 2)
            {
                twoTileCount++;
            }

            if (config.MoveRange == targetMoveRange)
            {
                continue;
            }

            var serializedObject = new SerializedObject(config);
            serializedObject.FindProperty("moveRange").intValue = targetMoveRange;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            updatedCount++;
        }

        if (MoveRangeByAssetName.Count != oneTileCount + twoTileCount)
        {
            issues.Add("Mobility balance table includes unsupported move ranges or duplicate tracking counts.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        if (issues.Count == 0)
        {
            Debug.Log($"[HexTacticsCharacterBalanceTuner] Updated {updatedCount} character config(s). 1-tile units: {oneTileCount}, 2-tile units: {twoTileCount}.");
            ExitIfNeeded(exitOnComplete, 0);
            return;
        }

        foreach (var issue in issues)
        {
            Debug.LogError("[HexTacticsCharacterBalanceTuner] " + issue);
        }

        ExitIfNeeded(exitOnComplete, 1);
    }

    private static void ExitIfNeeded(bool exitOnComplete, int exitCode)
    {
        if (exitOnComplete && Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
