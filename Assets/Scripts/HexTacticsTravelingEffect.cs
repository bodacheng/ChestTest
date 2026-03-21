using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexTacticsTravelingEffect : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float duration = 0.1f;
    private float elapsed;
    private bool isInitialized;

    public void Initialize(Vector3 start, Vector3 end, float travelDuration)
    {
        startPosition = start;
        endPosition = end;
        duration = Mathf.Max(0.02f, travelDuration);
        elapsed = 0f;
        isInitialized = true;
        transform.position = startPosition;

        var direction = endPosition - startPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        elapsed += Time.deltaTime;
        var normalized = Mathf.Clamp01(elapsed / duration);
        var eased = Mathf.SmoothStep(0f, 1f, normalized);
        transform.position = Vector3.Lerp(startPosition, endPosition, eased);

        if (normalized >= 1f)
        {
            enabled = false;
        }
    }
}
