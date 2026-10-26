using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EfectosLogoMarino : MonoBehaviour
{
    [Header("1. Flotación Marítima (Bobbing & Sway)")]
    public float velocidadFlotacion = 2.0f;
    public float amplitudFlotacion = 8.0f;
    public float velocidadInclinacion = 1.5f;
    public float maxAnguloInclinacion = 2.5f;

    [Header("2. Destello Blanco (Shine Effect)")]
    public Image imagenDestello;
    public float intervaloEntreDestellos = 4.0f;
    public float duracionDestello = 0.8f;

    // ⚪ Blanco con 50% de transparencia
    public Color colorDestelloBlanco = new Color(1f, 1f, 1f, 0.5f);

    private RectTransform rectLogo;
    private RectTransform rectDestello;
    private Vector2 posicionInicialLogo;

    private void Awake()
    {
        rectLogo = GetComponent<RectTransform>();
        posicionInicialLogo = rectLogo.anchoredPosition;

        if (imagenDestello != null)
        {
            rectDestello = imagenDestello.GetComponent<RectTransform>();
            imagenDestello.color = colorDestelloBlanco;

            // 🎯 ESCONDER LA BARRITA FUERA DE PANTALLA DESDE EL SEGUNDO 0
            float anchoLogo = rectLogo.rect.width;
            rectDestello.anchoredPosition = new Vector2(-anchoLogo * 1.5f, rectDestello.anchoredPosition.y);

            // Apagamos la imagen para que ni siquiera renderice mientras espera
            imagenDestello.enabled = false;
        }
    }

    private void Start()
    {
        if (imagenDestello != null)
        {
            StartCoroutine(RutinaDestelloBlanco());
        }
    }

    private void Update()
    {
        // Movimiento de flotación y balanceo
        float nuevoY = posicionInicialLogo.y + Mathf.Sin(Time.time * velocidadFlotacion) * amplitudFlotacion;
        rectLogo.anchoredPosition = new Vector2(posicionInicialLogo.x, nuevoY);

        float rotacionZ = Mathf.Sin(Time.time * velocidadInclinacion) * maxAnguloInclinacion;
        rectLogo.localRotation = Quaternion.Euler(0f, 0f, rotacionZ);
    }

    private IEnumerator RutinaDestelloBlanco()
    {
        float anchoLogo = rectLogo.rect.width;
        float posicionInicioX = -anchoLogo * 1.5f;
        float posicionFinX = anchoLogo * 1.5f;

        while (true)
        {
            // Mantiene la barra oculta durante el tiempo de espera
            if (imagenDestello != null) imagenDestello.enabled = false;

            yield return new WaitForSeconds(intervaloEntreDestellos);

            if (rectDestello != null && imagenDestello != null)
            {
                // Encendemos la barra justo cuando va a cruzar
                imagenDestello.enabled = true;

                float tiempoTranscurrido = 0f;

                while (tiempoTranscurrido < duracionDestello)
                {
                    tiempoTranscurrido += Time.deltaTime;
                    float progreso = tiempoTranscurrido / duracionDestello;

                    float xActual = Mathf.Lerp(posicionInicioX, posicionFinX, Mathf.SmoothStep(0f, 1f, progreso));
                    rectDestello.anchoredPosition = new Vector2(xActual, rectDestello.anchoredPosition.y);

                    yield return null;
                }

                // Al terminar de pasar, se esconde y apaga
                rectDestello.anchoredPosition = new Vector2(posicionInicioX, rectDestello.anchoredPosition.y);
                imagenDestello.enabled = false;
            }
        }
    }
}