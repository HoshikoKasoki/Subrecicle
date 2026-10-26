using UnityEngine;

public class MainMenuExpress : MonoBehaviour
{
    [Header("Cámaras")]
    public GameObject menuCamera;
    public GameObject playerCamera;

    [Header("Referencias del Jugador y UI")]
    public FirstPersonController playerController;
    public GameObject mainCanvasUI;
    public GameObject hotbarUI;

    private bool isGameStarted = false; // 👈 Bandera para saber si ya le diste a JUGAR

    private void Start()
    {
        isGameStarted = false;

        if (hotbarUI != null) hotbarUI.SetActive(false);

        if (menuCamera != null) menuCamera.SetActive(true);
        if (playerCamera != null) playerCamera.SetActive(false);

        if (playerController != null)
        {
            playerController.SetInputLocked(true);
            playerController.SetExternalCameraControl(true);
        }

        LiberarCursor();
    }

    private void Update()
    {
        // 🎯 Si el juego NO ha empezado, forzamos en cada frame que el cursor siga visible y suelto
        if (!isGameStarted)
        {
            LiberarCursor();
        }
    }

    public void StartGame()
    {
        isGameStarted = true; // 👈 Marcamos que el juego ya empezó

        if (mainCanvasUI != null) mainCanvasUI.SetActive(false);

        if (hotbarUI != null) hotbarUI.SetActive(true);

        if (menuCamera != null) menuCamera.SetActive(false);
        if (playerCamera != null) playerCamera.SetActive(true);

        if (playerController != null)
        {
            playerController.SetExternalCameraControl(false);
            playerController.SetInputLocked(false);
        }

        BloquearCursor();
    }

    private void LiberarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void BloquearCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SalirJuego()
    {
#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Si es el juego final exportado (.exe / .app)
        Application.Quit();
#endif
    }
}