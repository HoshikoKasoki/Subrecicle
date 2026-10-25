using InventorySystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controla el menú provisional de emotes y una cámara orbital temporal.
/// R abre/cierra el menú; R o Escape cancelan un emote activo.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public sealed class EmoteController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private FirstPersonController movement;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject emoteCanvas;
    [SerializeField] private Button emote1Button;
    [SerializeField] private Button emote2Button;
    [SerializeField] private Button emote3Button;
    [SerializeField] private Button emote4Button;
    [SerializeField] private Button emote5Button;
    [SerializeField] private Button emote6Button;
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private Animator animator;

    [Header("Cámara del emote")]
    [Tooltip("Distancia de la cámara al centro del jugador durante el emote.")]
    [Min(0.5f)]
    [SerializeField] private float cameraDistance = 5f;
    [Tooltip("Altura del punto alrededor del cual orbita la cámara.")]
    [SerializeField] private float targetHeight = 1.45f;
    [SerializeField] private float orbitSensitivity = 0.18f;
    [SerializeField] private Vector2 verticalLimits = new Vector2(-20f, 70f);
    [Min(0f)]
    [SerializeField] private float cameraSmoothTime = 0.08f;

    [Header("Animación opcional")]
    [Tooltip("Crea este Trigger en el Animator cuando agregues la animación. Déjalo vacío para usar sólo la vista previa.")]
    [SerializeField] private string emote1Trigger = "Emote1";
    [SerializeField] private string emote2Trigger = "Emote2";
    [SerializeField] private string emote3Trigger = "Emote3";
    [SerializeField] private string emote4Trigger = "Emote4";
    [SerializeField] private string emote5Trigger = "Emote5";
    [SerializeField] private string emote6Trigger = "Emote6";

    private bool menuOpen;
    private bool emotePlaying;
    private float orbitYaw;
    private float orbitPitch = 12f;
    private Vector3 cameraVelocity;

    public bool IsMenuOpen => menuOpen;
    public bool IsEmotePlaying => emotePlaying;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<FirstPersonController>();
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
        if (inventoryUI == null)
            inventoryUI = GetComponent<InventoryUIController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (emote1Button != null)
            emote1Button.onClick.AddListener(PlayEmote1);
        if (emote2Button != null)
            emote2Button.onClick.AddListener(PlayEmote2);
        if (emote3Button != null)
            emote3Button.onClick.AddListener(PlayEmote3);
        if (emote4Button != null)
            emote4Button.onClick.AddListener(PlayEmote4);
        if (emote5Button != null)
            emote5Button.onClick.AddListener(PlayEmote5);
        if (emote6Button != null)
            emote6Button.onClick.AddListener(PlayEmote6);

        SetCanvasVisible(false);
    }

    private void OnDestroy()
    {
        if (emote1Button != null)
            emote1Button.onClick.RemoveListener(PlayEmote1);
        if (emote2Button != null)
            emote2Button.onClick.RemoveListener(PlayEmote2);
        if (emote3Button != null)
            emote3Button.onClick.RemoveListener(PlayEmote3);
        if (emote4Button != null)
            emote4Button.onClick.RemoveListener(PlayEmote4);
        if (emote5Button != null)
            emote5Button.onClick.RemoveListener(PlayEmote5);
        if (emote6Button != null)
            emote6Button.onClick.RemoveListener(PlayEmote6);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Si se abrió el inventario u otra estación, cedemos el control sin
        // desbloquear el movimiento que esa interfaz necesita bloquear.
        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            if (menuOpen || emotePlaying)
                CloseEmoteMode(false);
            return;
        }

        if (emotePlaying)
        {
            ReadOrbitInput();

            if (keyboard.rKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                CloseEmoteMode(true);

            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            if (menuOpen)
                CloseEmoteMode(true);
            else
                OpenMenu();

            return;
        }

        if (menuOpen && keyboard.escapeKey.wasPressedThisFrame)
            CloseEmoteMode(true);
    }

    private void LateUpdate()
    {
        if (!emotePlaying || playerCamera == null)
            return;

        Vector3 target = transform.position + Vector3.up * targetHeight;
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 desiredPosition = target + orbitRotation * (Vector3.back * cameraDistance);

        if (cameraSmoothTime <= 0f)
            playerCamera.position = desiredPosition;
        else
            playerCamera.position = Vector3.SmoothDamp(
                playerCamera.position,
                desiredPosition,
                ref cameraVelocity,
                cameraSmoothTime);

        Vector3 lookDirection = target - playerCamera.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            playerCamera.rotation = cameraSmoothTime <= 0f
                ? desiredRotation
                : Quaternion.Slerp(playerCamera.rotation, desiredRotation, 1f - Mathf.Exp(-20f * Time.deltaTime));
        }
    }

    private void OpenMenu()
    {
        menuOpen = true;
        emotePlaying = false;
        SetCanvasVisible(true);

        if (movement != null)
            movement.SetInputLocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayEmote1()
    {
        PlayEmote(emote1Trigger);
    }

    public void PlayEmote2()
    {
        PlayEmote(emote2Trigger);
    }

    public void PlayEmote3()
    {
        PlayEmote(emote3Trigger);
    }

    public void PlayEmote4()
    {
        PlayEmote(emote4Trigger);
    }

    public void PlayEmote5()
    {
        PlayEmote(emote5Trigger);
    }

    public void PlayEmote6()
    {
        PlayEmote(emote6Trigger);
    }

    private void PlayEmote(string animatorTrigger)
    {
        if (!menuOpen)
            return;

        menuOpen = false;
        emotePlaying = true;
        SetCanvasVisible(false);

        orbitYaw = transform.eulerAngles.y;
        orbitPitch = Mathf.Clamp(orbitPitch, verticalLimits.x, verticalLimits.y);
        cameraVelocity = Vector3.zero;

        if (movement != null)
        {
            movement.SetInputLocked(true);
            movement.SetExternalCameraControl(true);
        }

        TrySetAnimatorTrigger(animatorTrigger);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ReadOrbitInput()
    {
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        orbitYaw += delta.x * orbitSensitivity;
        orbitPitch = Mathf.Clamp(
            orbitPitch - delta.y * orbitSensitivity,
            verticalLimits.x,
            verticalLimits.y);
    }

    private void CloseEmoteMode(bool restoreGameplayInput)
    {
        menuOpen = false;
        emotePlaying = false;
        cameraVelocity = Vector3.zero;
        SetCanvasVisible(false);

        if (movement != null)
        {
            movement.SetExternalCameraControl(false);
            if (restoreGameplayInput)
                movement.SetInputLocked(false);
        }

        Cursor.lockState = restoreGameplayInput ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !restoreGameplayInput;
    }

    private void SetCanvasVisible(bool visible)
    {
        if (emoteCanvas != null)
            emoteCanvas.SetActive(visible);
    }

    private void TrySetAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }

        Debug.Log(
            "EmoteController: la vista previa funciona, pero falta crear el Trigger '" +
            triggerName + "' en el Animator para reproducir la animación.",
            this);
    }

    private void OnDisable()
    {
        if (menuOpen || emotePlaying)
            CloseEmoteMode(true);
    }
}
