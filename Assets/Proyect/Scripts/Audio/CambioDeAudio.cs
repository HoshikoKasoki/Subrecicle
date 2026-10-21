using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambioDeAudio : MonoBehaviour
{
    public AudioClip clipPasos;
    public GameObject player;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Debug.Log("Hola");
            player.GetComponent<AudioSource>().clip = clipPasos;
        }
    }
}