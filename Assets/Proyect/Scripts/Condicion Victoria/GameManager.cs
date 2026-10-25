using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Interfaz de Victoria")]
    [SerializeField] private GameObject panelVictoria;

    private bool juegoGanado = false;

    private void Start()
    {
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(false);
        }
    }

    private void Update()
    {
        if (juegoGanado) return;

        GameObject[] basuraRestante = GameObject.FindGameObjectsWithTag("Basura");

        if (basuraRestante.Length == 0)
        {
            GanarJuego();
        }
    }

    private void GanarJuego()
    {
        juegoGanado = true;
        Debug.Log("¡Felicidades! Has limpiado toda la basura del mapa.");

        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}