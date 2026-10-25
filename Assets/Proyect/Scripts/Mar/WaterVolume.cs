using UnityEngine;

/// <summary>
/// A box-shaped water region. Keep the box covering the full water depth.
/// Its top visual surface can be controlled by surfaceReference or Surface Offset.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WaterVolume : MonoBehaviour
{
    [Header("Superficie del agua")]
    [Tooltip("Transform situado a la altura media del agua. Si esta vacio, se usa la Y de este objeto + Surface Offset.")]
    [SerializeField] private Transform surfaceReference;
    [SerializeField] private float surfaceOffset = 0f;

    [Header("Fisica (unidades de juego)")]
    [Min(0f)] public float waterDensity = 1f;

    private BoxCollider box;

    public float SurfaceY => (surfaceReference != null ? surfaceReference.position.y : transform.position.y + surfaceOffset);

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnValidate()
    {
        BoxCollider currentBox = GetComponent<BoxCollider>();
        if (currentBox != null) currentBox.isTrigger = true;
    }

    /// <summary>Does a world-space point lie inside this water box and below the surface?</summary>
    public bool IsPointUnderwater(Vector3 worldPoint)
    {
        if (box == null) box = GetComponent<BoxCollider>();
        if (box == null || !box.enabled || !isActiveAndEnabled) return false;

        // Test in the collider's local space; works even when the water volume is rotated or scaled.
        Vector3 point = transform.InverseTransformPoint(worldPoint) - box.center;
        Vector3 half = box.size * 0.5f;
        bool inside = Mathf.Abs(point.x) <= half.x &&
                      Mathf.Abs(point.y) <= half.y &&
                      Mathf.Abs(point.z) <= half.z;

        return inside && worldPoint.y < SurfaceY;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.TryGetComponent(out WaterBuoyancy buoyancy))
        {
            buoyancy.EnterWater(this);
        }

        FirstPersonController player =
            other.GetComponentInParent<FirstPersonController>();

        if (player != null)
            player.EnterWater(this);
    }

    private void OnTriggerStay(Collider other)
    {
        // También cubre al jugador si la escena comienza con él ya dentro
        // del agua o si el volumen se activa durante la partida.
        FirstPersonController player =
            other.GetComponentInParent<FirstPersonController>();

        if (player != null)
            player.EnterWater(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.TryGetComponent(out WaterBuoyancy buoyancy))
        {
            buoyancy.ExitWater(this);
        }

        FirstPersonController player =
            other.GetComponentInParent<FirstPersonController>();

        if (player != null)
            player.ExitWater(this);
    }
}
