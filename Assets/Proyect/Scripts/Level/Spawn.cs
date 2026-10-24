using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class GeneradorPrefabs : MonoBehaviour
{
    [Header("Lista de Prefabs")]
    public List<GameObject> prefabsAGenerar = new List<GameObject>();

    [Header("Tiempo de generación")]
    public float tiempoMinimo = 1f;
    public float tiempoMaximo = 4f;

    [Header("Límite de generación")]
    public int maximoGenerados = 10;
    private int generadosActuales;

    [Header("Área de generación")]
    public BoxCollider areaGeneracion;

    void Awake()
    {
        if (areaGeneracion == null)
        {
            areaGeneracion = GetComponent<BoxCollider>();
        }
    }

    void Start()
    {
        ProgramarSiguienteGeneracion();
    }

    void ProgramarSiguienteGeneracion()
    {
        if (generadosActuales >= maximoGenerados)
            return;

        if (prefabsAGenerar == null || prefabsAGenerar.Count == 0)
            return;

        float espera = Random.Range(tiempoMinimo, tiempoMaximo);

        Invoke(nameof(Generar), espera);
    }

    void Generar()
    {
        if (generadosActuales < maximoGenerados &&
            prefabsAGenerar.Count > 0 &&
            areaGeneracion != null)
        {
            // Seleccionar un prefab aleatorio
            int indiceAleatorio = Random.Range(0, prefabsAGenerar.Count);

            GameObject prefabSeleccionado = prefabsAGenerar[indiceAleatorio];

            if (prefabSeleccionado != null)
            {
                // Obtener una posición aleatoria dentro del collider
                Vector3 posicionAleatoria = ObtenerPosicionAleatoria();

                // Generar el prefab seleccionado
                Instantiate(
                    prefabSeleccionado,
                    posicionAleatoria,
                    transform.rotation
                );

                generadosActuales++;
            }
        }

        ProgramarSiguienteGeneracion();
    }

    Vector3 ObtenerPosicionAleatoria()
    {
        Vector3 centro = areaGeneracion.center;
        Vector3 tamaño = areaGeneracion.size;

        float x = Random.Range(-tamaño.x / 2f, tamaño.x / 2f);
        float y = Random.Range(-tamaño.y / 2f, tamaño.y / 2f);
        float z = Random.Range(-tamaño.z / 2f, tamaño.z / 2f);

        Vector3 posicionLocal = centro + new Vector3(x, y, z);

        return areaGeneracion.transform.TransformPoint(posicionLocal);
    }
}