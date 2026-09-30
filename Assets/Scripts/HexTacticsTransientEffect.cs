using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HexTacticsTransientEffect : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fallbackLifetime = 1.4f;
    [SerializeField, Min(0f)] private float destroyDelayPadding = 0.12f;
    [SerializeField, Tooltip("Optional authored core. Use this for effects whose brightest point is not their prefab origin.")]
    private Transform visualCore;
    [SerializeField] private Vector3 visualCoreOffset;

    private ParticleSystem[] particleSystems;
    private TrailRenderer[] trailRenderers;
    private Coroutine releaseRoutine;
    private GameObject releaseTarget;

    public void SetReleaseTarget(GameObject target)
    {
        releaseTarget = target;
    }

    public bool TryGetAuthoredVisualCorePosition(out Vector3 position)
    {
        position = visualCore != null ? visualCore.TransformPoint(visualCoreOffset) : Vector3.zero;
        return visualCore != null;
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        CacheComponents();
        RestartEffect();

        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
        }

        releaseRoutine = StartCoroutine(ReleaseAfterDelay(ResolveLifetime()));
    }

    private void CacheComponents()
    {
        particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
        trailRenderers ??= GetComponentsInChildren<TrailRenderer>(true);
    }

    private void RestartEffect()
    {
        if (trailRenderers != null)
        {
            foreach (var trail in trailRenderers)
            {
                if (trail != null)
                {
                    trail.Clear();
                }
            }
        }

        if (particleSystems == null)
        {
            return;
        }

        foreach (var particleSystem in particleSystems)
        {
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
            if (particleSystem.gameObject.activeInHierarchy)
            {
                particleSystem.Play(false);
            }
        }
    }

    private float ResolveLifetime()
    {
        var lifetime = fallbackLifetime;

        if (trailRenderers != null)
        {
            foreach (var trail in trailRenderers)
            {
                if (trail != null)
                {
                    lifetime = Mathf.Max(lifetime, trail.time);
                }
            }
        }

        if (particleSystems != null)
        {
            foreach (var particleSystem in particleSystems)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                if (main.loop)
                {
                    lifetime = Mathf.Max(lifetime, fallbackLifetime);
                    continue;
                }

                var startDelay = ResolveMaxCurveValue(main.startDelay);
                var startLifetime = ResolveMaxCurveValue(main.startLifetime);
                lifetime = Mathf.Max(lifetime, startDelay + main.duration + startLifetime);
            }
        }

        return lifetime + destroyDelayPadding;
    }

    internal static float ResolveMaxCurveValue(ParticleSystem.MinMaxCurve curve)
    {
        return curve.mode switch
        {
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(curve.constantMin, curve.constantMax),
            ParticleSystemCurveMode.Curve => ResolveCurvePeak(curve.curve) * curve.curveMultiplier,
            ParticleSystemCurveMode.TwoCurves => Mathf.Max(
                ResolveCurvePeak(curve.curveMin) * curve.curveMultiplier,
                ResolveCurvePeak(curve.curveMax) * curve.curveMultiplier),
            _ => curve.constant
        };
    }

    private static float ResolveCurvePeak(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return 0f;
        }

        var peak = curve.keys[0].value;
        for (var i = 1; i < curve.length; i++)
        {
            peak = Mathf.Max(peak, curve.keys[i].value);
        }

        return peak;
    }

    private IEnumerator ReleaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(releaseTarget != null ? releaseTarget : gameObject);
    }
}
