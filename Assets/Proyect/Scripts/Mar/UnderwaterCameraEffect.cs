using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Mezcla el postprocesado submarino en funcion de la altura de la camara.
/// Colocar en la camara del jugador (que normalmente representa sus ojos).
/// </summary>
public class UnderwaterCameraEffect : MonoBehaviour
{
    [Header("Agua")]
    [SerializeField] private WaterVolume[] waterVolumes;

    [Header("Transicion de superficie")]
    [Tooltip("A cuantos metros POR ENCIMA de la superficie empieza el efecto visual.")]
    [Min(0f)][SerializeField] private float startAboveSurface = 0.15f;

    [Tooltip("Cuantos metros POR DEBAJO de la superficie se alcanza el efecto completo.")]
    [Min(0.01f)][SerializeField] private float fullEffectBelowSurface = 0.15f;

    [Header("Postprocesado URP")]
    [SerializeField] private Volume underwaterVolume;
    [Tooltip("Velocidad maxima de cambio del peso del Volume (1/segundo).")]
    [Min(0.01f)][SerializeField] private float fadeSpeed = 5f;

    [Header("Audio submarino (opcional)")]
    [SerializeField] private AudioLowPassFilter audioLowPass;
    [Range(100f, 22000f)][SerializeField] private float underwaterCutoff = 850f;
    [Range(100f, 22000f)][SerializeField] private float outsideCutoff = 22000f;

    private void OnEnable()
    {
        if (underwaterVolume != null)
            underwaterVolume.weight = 0f;
    }

    private void LateUpdate()
    {
        if (underwaterVolume == null)
            return;

        float targetWeight = 0f;
        Vector3 cameraPosition = transform.position;

        if (waterVolumes != null)
        {
            foreach (WaterVolume water in waterVolumes)
            {
                if (water == null)
                    continue;

                // Comprobamos que la camara este sobre el area del volumen.
                // La posicion de prueba se limita justo bajo la superficie para
                // que el collider no descarte a la camara cuando aun esta un
                // poco POR ENCIMA del agua.
                Vector3 probePosition = cameraPosition;
                probePosition.y = Mathf.Min(cameraPosition.y, water.SurfaceY - 0.01f);

                if (!water.IsPointUnderwater(probePosition))
                    continue;

                // 0 al estar sobre la superficie + margen;
                // 1 al alcanzar la profundidad indicada.
                float surfaceStart = water.SurfaceY + startAboveSurface;
                float surfaceFull = water.SurfaceY - fullEffectBelowSurface;
                float weight = Mathf.Clamp01(
                    (surfaceStart - cameraPosition.y) / (surfaceStart - surfaceFull)
                );

                // Si hay varios volumenes, aplicamos el efecto mas intenso.
                targetWeight = Mathf.Max(targetWeight, weight);
            }
        }

        underwaterVolume.weight = Mathf.MoveTowards(
            underwaterVolume.weight,
            targetWeight,
            fadeSpeed * Time.deltaTime
        );

        if (audioLowPass != null)
        {
            audioLowPass.cutoffFrequency = Mathf.Lerp(
                outsideCutoff,
                underwaterCutoff,
                underwaterVolume.weight
            );
        }
    }

    private void OnDisable()
    {
        if (underwaterVolume != null)
            underwaterVolume.weight = 0f;

        if (audioLowPass != null)
            audioLowPass.cutoffFrequency = outsideCutoff;
    }
}
