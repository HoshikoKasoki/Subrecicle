using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public enum LocomotionMode { Grounded, Swimming, Diving }

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 4f;
    [SerializeField] private float velocidadSprint = 7f;
    [SerializeField] private float fuerzaSalto = 1.2f;
    [SerializeField] private float gravedad = -20f;

    [Header("Agua (preparado para volúmenes futuros)")]
    [SerializeField] private float velocidadNado = 3f;
    [SerializeField] private float velocidadBuceo = 2.5f;

    [Header("Agacharse")]
    [SerializeField] private float alturaNormal = 1.8f;
    [SerializeField] private float alturaAgachado = 1f;
    [SerializeField] private float velocidadAgacharse = 10f;

    [Header("Cámara")]
    [SerializeField] private Transform camara;
    [SerializeField] private float sensibilidadMouse = 2f;
    [SerializeField] private float limiteVertical = 89f;

    private CharacterController controller;
    public AudioSource pasos;
    public AudioSource audioSalto;

    private Vector2 movimientoInput;
    private Vector2 mouseInput;

    private float velocidadVertical;
    private float rotacionVertical;

    private bool agachado;
    private bool inputLocked;

    private LocomotionMode locomotionMode = LocomotionMode.Grounded;

    public LocomotionMode CurrentLocomotionMode => locomotionMode;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        ConfigureCharacterPhysics();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ConfigureCharacterPhysics()
    {
        // CharacterController y Rigidbody no deben mover el mismo objeto.
        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        CapsuleCollider legacyCollider = GetComponent<CapsuleCollider>();

        if (legacyCollider != null)
            legacyCollider.enabled = false;

        controller.height = Mathf.Max(
            alturaNormal,
            controller.radius * 2f
        );

        controller.center = Vector3.up * (controller.height * 0.5f);

        controller.skinWidth = Mathf.Min(
            0.08f,
            controller.radius * 0.2f
        );
    }

    private void Update()
    {
        if (inputLocked)
            return;

        Movimiento();
        Camara();
        Agacharse();
    }

    private void Movimiento()
    {
        Vector3 movimiento =
            transform.right * movimientoInput.x +
            transform.forward * movimientoInput.y;
        // =========================================================
        // AUDIO PASOS
        // =========================================================
        if (pasos != null && controller.isGrounded)
        {
            // Detectamos si el jugador se está moviendo y si además está presionando Shift (corriendo)
            bool estaCorriendo = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && movimientoInput.y > 0 && !agachado;

            // Si hay entrada de movimiento
            if (movimientoInput.magnitude > 0.1f)
            {
                // Ajustamos el pitch (velocidad del audio): más rápido si corre, normal si camina
                pasos.pitch = estaCorriendo ? 1.5f : 1.0f; // Puedes cambiar 1.5f por el valor que mejor suene para ti

                if (!pasos.isPlaying)
                {
                    pasos.Play();
                }
            }
            else
            {
                if (pasos.isPlaying)
                {
                    pasos.Pause();
                }
            }
        }
        else
        {
            // Si está en el aire, saltando o nadando, pausamos el sonido y reseteamos el pitch
            if (pasos != null && pasos.isPlaying)
            {
                pasos.Pause();
                pasos.pitch = 1.0f;
            }
        }
        // =========================================================
       // Salto
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            controller.isGrounded &&
            !agachado)
            {
            velocidadVertical =
                Mathf.Sqrt(
                    fuerzaSalto * -2f * gravedad
                );
            if (audioSalto != null)
            {
                audioSalto.Play();
            }
            }
        
        
        if (locomotionMode != LocomotionMode.Grounded)
        {
            float verticalInput = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.isPressed)
                    verticalInput += 1f;

                if (Keyboard.current.leftCtrlKey.isPressed)
                    verticalInput -= 1f;
            }

            movimiento.y = verticalInput;

            float speed =
                locomotionMode == LocomotionMode.Diving
                    ? velocidadBuceo
                    : velocidadNado;

            controller.Move(
                movimiento.normalized *
                speed *
                Time.deltaTime
            );

            return;


        }

        bool corriendo =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            movimientoInput.y > 0 &&
            !agachado;

        float velocidadActual =
            corriendo
                ? velocidadSprint
                : velocidad;

        movimiento *= velocidadActual;

        // Gravedad
        if (controller.isGrounded && velocidadVertical < 0)
        {
            velocidadVertical = -2f;
        }

        // Salto
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            controller.isGrounded &&
            !agachado)
        {
            velocidadVertical =
                Mathf.Sqrt(
                    fuerzaSalto * -2f * gravedad
                );
        }

        velocidadVertical += gravedad * Time.deltaTime;

        movimiento.y = velocidadVertical;

        controller.Move(
            movimiento * Time.deltaTime
        );
    }

    private void Camara()
    {
        float mouseX =
            mouseInput.x * sensibilidadMouse;

        float mouseY =
            mouseInput.y * sensibilidadMouse;

        // Rotación horizontal del jugador
        transform.Rotate(
            Vector3.up * mouseX
        );

        // Rotación vertical de la cámara
        rotacionVertical -= mouseY;

        rotacionVertical = Mathf.Clamp(
            rotacionVertical,
            -limiteVertical,
            limiteVertical
        );

        camara.localRotation =
            Quaternion.Euler(
                rotacionVertical,
                0f,
                0f
            );
    }

    private void Agacharse()
    {
        // Cambiar estado de agachado
        if (Keyboard.current != null &&
            Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            agachado = !agachado;
        }

        // =========================================================
        // ALTURA DEL CHARACTER CONTROLLER
        // =========================================================

        float alturaObjetivo =
            agachado
                ? alturaAgachado
                : alturaNormal;

        controller.height = Mathf.Lerp(
            controller.height,
            alturaObjetivo,
            velocidadAgacharse * Time.deltaTime
        );

        // =========================================================
        // CENTER DEL CHARACTER CONTROLLER
        // =========================================================
        //
        // Estos son EXACTAMENTE tus valores originales.
        // Solo hacemos que lleguen suavemente a ellos.

        Vector3 centerObjetivo =
            agachado
                ? new Vector3(0f, -0.5f, 0f)
                : new Vector3(0f, 0f, 0f);

        controller.center = Vector3.Lerp(
            controller.center,
            centerObjetivo,
            velocidadAgacharse * Time.deltaTime
        );

        // =========================================================
        // POSICIÓN DE LA CÁMARA
        // =========================================================
        //
        // Agachado:
        // Y = 0
        //
        // Normal:
        // Y = 0.66
        //
        // Solo suavizamos la transición.

        Vector3 posicionCamaraObjetivo =
            agachado
                ? new Vector3(0f, 0f, 0f)
                : new Vector3(0f, 0.66f, 0f);

        camara.localPosition = Vector3.Lerp(
            camara.localPosition,
            posicionCamaraObjetivo,
            velocidadAgacharse * Time.deltaTime
        );
    }

    // =============================================================
    // INPUT SYSTEM
    // =============================================================

    public void OnMove(InputValue value)
    {
        movimientoInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        mouseInput = value.Get<Vector2>();
    }

    // =============================================================
    // INPUT LOCK
    // =============================================================

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        if (pasos != null && pasos.isPlaying)
            {
                pasos.Pause();
                pasos.pitch = 1.0f;
            }
            
        if (locked)
        {
            movimientoInput = Vector2.zero;
            mouseInput = Vector2.zero;
        }
    }

    // =============================================================
    // LOCOMOTION
    // =============================================================

    /// <summary>
    /// Llamado por un volumen de agua cuando se implemente
    /// el nivel acuático.
    /// </summary>
    public void SetLocomotionMode(LocomotionMode mode)
    {
        locomotionMode = mode;
        velocidadVertical = 0f;
    }
}
