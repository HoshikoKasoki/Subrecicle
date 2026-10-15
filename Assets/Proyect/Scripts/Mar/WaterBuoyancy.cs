using UnityEngine;

/// <summary>
/// Approximate buoyancy using the body's world-space collider bounding box.
/// One Rigidbody and one main non-trigger Collider per object is recommended.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WaterBuoyancy : MonoBehaviour
{
    [Header("Geometria para calcular desplazamiento")]
    [SerializeField] private Collider bodyCollider;

    [Header("Resistencia del agua")]
    [Min(0f)] [SerializeField] private float waterDamping = 2f;
    [Min(0f)] [SerializeField] private float waterAngularDamping = 1.5f;

    private Rigidbody rb;
    private WaterVolume currentWater;
    private float originalDamping;
    private float originalAngularDamping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (bodyCollider == null)
        {
            foreach (Collider candidate in GetComponentsInChildren<Collider>())
            {
                if (!candidate.isTrigger)
                {
                    bodyCollider = candidate;
                    break;
                }
            }
        }
        if (bodyCollider == null)
            Debug.LogWarning($"{name}: WaterBuoyancy necesita un Collider no-trigger.", this);
    }

    public void EnterWater(WaterVolume water)
    {
        if (currentWater == water) return;
        if (currentWater != null) ExitWater(currentWater);

        currentWater = water;
        originalDamping = GetDamping();
        originalAngularDamping = GetAngularDamping();
        SetDamping(waterDamping, waterAngularDamping);
        rb.WakeUp();
    }

    public void ExitWater(WaterVolume water)
    {
        if (currentWater != water) return;
        currentWater = null;
        SetDamping(originalDamping, originalAngularDamping);
    }

    private void OnDisable()
    {
        if (currentWater != null) ExitWater(currentWater);
    }

    private void FixedUpdate()
    {
        if (currentWater == null || bodyCollider == null || rb.isKinematic) return;

        Bounds bounds = bodyCollider.bounds;
        float height = bounds.size.y;
        if (height < 0.0001f) return;

        float submergedHeight = Mathf.Clamp(currentWater.SurfaceY - bounds.min.y, 0f, height);
        if (submergedHeight <= 0f) return;

        float submergedFraction = submergedHeight / height;
        float estimatedVolume = bounds.size.x * bounds.size.y * bounds.size.z;

        // Archimedes: upward force = fluid density * displaced volume * gravity.
        Vector3 buoyancyForce = -Physics.gravity *
                                (currentWater.waterDensity * estimatedVolume * submergedFraction);

        // Apply to the approximate center of the submerged section to produce some rocking.
        Vector3 buoyancyCenter = new Vector3(
            bounds.center.x,
            bounds.min.y + submergedHeight * 0.5f,
            bounds.center.z);

        rb.AddForceAtPosition(buoyancyForce, buoyancyCenter, ForceMode.Force);
    }

    private float GetDamping()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearDamping;
#else
        return rb.drag;
#endif
    }

    private float GetAngularDamping()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.angularDamping;
#else
        return rb.angularDrag;
#endif
    }

    private void SetDamping(float linear, float angular)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = linear;
        rb.angularDamping = angular;
#else
        rb.drag = linear;
        rb.angularDrag = angular;
#endif
    }
}
