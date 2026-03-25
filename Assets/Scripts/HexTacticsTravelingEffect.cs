using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexTacticsTravelingEffect : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float duration = 0.1f;
    private float arcHeight;
    private float lateralSway;
    private float spinDegreesPerSecond;
    private float elapsed;
    private Vector3 baseScale = Vector3.one;
    private bool isInitialized;

    public void Initialize(
        Vector3 start,
        Vector3 end,
        float travelDuration,
        float travelArcHeight,
        float travelLateralSway,
        float travelSpinDegreesPerSecond)
    {
        startPosition = start;
        endPosition = end;
        duration = Mathf.Max(0.02f, travelDuration);
        arcHeight = Mathf.Max(0f, travelArcHeight);
        lateralSway = Mathf.Max(0f, travelLateralSway);
        spinDegreesPerSecond = Mathf.Max(0f, travelSpinDegreesPerSecond);
        elapsed = 0f;
        baseScale = transform.localScale;
        isInitialized = true;
        transform.position = startPosition;
        ApplyPose(0f);
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        elapsed += Time.deltaTime;
        var normalized = Mathf.Clamp01(elapsed / duration);
        ApplyPose(normalized);

        if (normalized >= 1f)
        {
            enabled = false;
        }
    }

    private void ApplyPose(float normalized)
    {
        var eased = Mathf.SmoothStep(0f, 1f, normalized);
        var directPosition = Vector3.Lerp(startPosition, endPosition, eased);
        var planarDirection = endPosition - startPosition;
        planarDirection.y = 0f;
        if (planarDirection.sqrMagnitude < 0.0001f)
        {
            planarDirection = Vector3.forward;
        }

        planarDirection.Normalize();
        var sideDirection = Vector3.Cross(Vector3.up, planarDirection);
        if (sideDirection.sqrMagnitude < 0.0001f)
        {
            sideDirection = Vector3.right;
        }

        sideDirection.Normalize();
        var arcOffset = Vector3.up * (Mathf.Sin(normalized * Mathf.PI) * arcHeight);
        var swayEnvelope = 1f - Mathf.SmoothStep(0.72f, 1f, normalized);
        var swayOffset = sideDirection * (Mathf.Sin(normalized * Mathf.PI * 2.4f) * lateralSway * swayEnvelope);
        var currentPosition = directPosition + arcOffset + swayOffset;
        transform.position = currentPosition;

        var lookAheadNormalized = Mathf.Clamp01(normalized + 0.02f);
        var lookAheadPosition = Vector3.Lerp(startPosition, endPosition, Mathf.SmoothStep(0f, 1f, lookAheadNormalized)) +
            Vector3.up * (Mathf.Sin(lookAheadNormalized * Mathf.PI) * arcHeight) +
            sideDirection * (Mathf.Sin(lookAheadNormalized * Mathf.PI * 2.4f) * lateralSway * swayEnvelope);
        var velocity = lookAheadPosition - currentPosition;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up) *
                Quaternion.AngleAxis(elapsed * spinDegreesPerSecond, Vector3.forward);
        }

        var scalePulse = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.08f;
        transform.localScale = Vector3.Scale(baseScale, Vector3.one * scalePulse);
    }
}
