using System.Collections.Generic;
using UnityEngine;

public enum HexTacticsEffectAnchorKind
{
    Auto,
    CenterMass,
    Head,
    Mouth,
    RightHand,
    LeftHand,
    Weapon
}

[DisallowMultipleComponent]
public sealed class HexTacticsEffectAnchorMap : MonoBehaviour
{
    [System.Serializable]
    private struct EffectAnchorEntry
    {
        public HexTacticsEffectAnchorKind kind;
        public Transform target;
    }

    [SerializeField] private List<EffectAnchorEntry> anchors = new();

    public bool TryResolve(HexTacticsEffectAnchorKind kind, out Transform target)
    {
        target = null;
        if (anchors == null || anchors.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < anchors.Count; i++)
        {
            if (anchors[i].kind != kind || anchors[i].target == null)
            {
                continue;
            }

            target = anchors[i].target;
            return true;
        }

        return false;
    }
}
