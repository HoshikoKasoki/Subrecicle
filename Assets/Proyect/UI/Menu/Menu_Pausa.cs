using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuUI;

    [Header("Referencias del Sistema")]
    [SerializeField] private MainMenuExpress mainMenuManager; 
    [SerializeField] private FirstPersonController playerController; 
    [SerializeField] private InventoryUIController inventoryUIController;

    public static bool isPaused = false;
    private bool wasInventoryOpenBeforePause = false;

    private void Awake()
    {
        if (inventoryUIController == null)
        {
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (mainMenuManager != null && !mainMenuManager.IsGameStarted)
            {
                return; 
            }

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        if (inventoryUIController != null)
        {
            wasInventoryOpenBeforePause = inventoryUIController.IsOpen;
        }
        else
        {
            wasInventoryOpenBeforePause = false;
        }

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;

        if (playerController != null)
            playerController.SetInputLocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        if (wasInventoryOpenBeforePause && inventoryUIController != null && inventoryUIController.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController != null)
                playerController.SetInputLocked(true);
        }
        else
        {
            if (playerController != null)
            {
                playerController.SetInputLocked(false);
                playerController.SetExternalCameraControl(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        isPaused = false;
        
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}