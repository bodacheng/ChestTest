using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexTacticsAnimationEventRelay : MonoBehaviour
{
    public int AttackImpactCount { get; private set; }

    // Called from animation events when an attack clip reaches its impact frame.
    public void EmitAttackImpact()
    {
        AttackImpactCount++;
    }
}
