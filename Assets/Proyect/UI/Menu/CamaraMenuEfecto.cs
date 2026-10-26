using UnityEngine;

public class CamaraMenuEfecto : MonoBehaviour
{
    [Header("Efecto de Movimiento")]
    public float velocidadRotacion = 2.0f; 
    public float anguloMaximo = 5.0f;       

    [Header("Efecto Flotación / Respiración")]
    public float velocidadFlotacion = 0.5f;
    public float alturaFlotacion = 0.1f;

    private Quaternion rotacionInicial;
    private Vector3 posicionInicial;

    private void Start()
    {
        rotacionInicial = transform.rotation;
        posicionInicial = transform.position;
    }

    private void Update()
    {

        float anguloY = Mathf.Sin(Time.time * velocidadRotacion) * anguloMaximo;
        transform.rotation = rotacionInicial * Quaternion.Euler(0, anguloY, 0);


        float offsetY = Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotacion;
        transform.position = posicionInicial + new Vector3(0, offsetY, 0);
    }
}