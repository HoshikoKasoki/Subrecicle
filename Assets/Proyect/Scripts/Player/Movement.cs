using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public enum LocomotionMode
    {
        Grounded,
        Swimming,
        Diving
    }

    // =============================================================
    // CONFIGURACIÓN
    // =============================================================

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 4f;
    [SerializeField] private float velocidadSprint = 7f;
    [SerializeField] private float fuerzaSalto = 1.2f;
    [SerializeField] private float gravedad = -20f;

    [Header("Agua")]
    [SerializeField] private float velocidadNado = 3f;
    [SerializeField] private float velocidadBuceo = 2.5f;
    [Tooltip("Altura del cuerpo que debe quedar bajo la superficie antes de empezar a nadar.")]
    [Range(0.3f, 0.9f)][SerializeField] private float alturaActivacionNado = 0.65f;
    [Tooltip("Altura de los ojos sobre la superficie mientras el personaje flota.")]
    [Min(0f)][SerializeField] private float margenCabezaSobreAgua = 0.25f;
    [Min(0.1f)][SerializeField] private float velocidadVerticalNado = 2.2f;
    [Min(0.1f)][SerializeField] private float fuerzaFlotacion = 3f;
    [Tooltip("Profundidad de los ojos a partir de la cual se mantiene el modo buceo.")]
    [Min(0f)][SerializeField] private float profundidadParaBucear = 0.2f;

    [Header("Cámara")]
    [SerializeField] private Transform camara;
    [SerializeField] private float sensibilidadMouse = 2f;
    [SerializeField] private float limiteVertical = 89f;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [SerializeField] private string parametroVelocidad = "Speed";
    [SerializeField] private string parametroSuelo = "onGround";
    [SerializeField] private string parametroAgua = "onWater";
    [SerializeField] private string parametroSalto = "IsJumping";

    [Header("Detección de superficies")]
    [SerializeField] private LayerMask capasSuelo;
    [SerializeField] private LayerMask capasAgua;

    [Header("Detección de suelo")]
    [SerializeField] private float margenDeteccionSuelo = 0.1f;

    [Header("Audio")]
    public AudioSource pasos;
    public AudioSource audioSalto;

    // =============================================================
    // VARIABLES PRIVADAS
    // =============================================================

    private CharacterController controller;

    private Vector2 movimientoInput;
    private Vector2 mouseInput;

    private float velocidadVertical;
    private float rotacionVertical;

    // Posición de cámara independiente de animaciones
    private Vector3 offsetCamara;

    private bool agachado;
    private bool inputLocked;
    private bool cameraExternallyControlled;

    private readonly HashSet<WaterVolume> volumenesAguaActivos =
        new HashSet<WaterVolume>();
    private WaterVolume volumenAguaActual;
    private float intensidadMovimientoAgua;

    // Estados físicos
    public bool onGround { get; private set; }
    public bool onWater { get; private set; }

    // Velocidad real del personaje
    public float velocidadReal { get; private set; }

    private LocomotionMode locomotionMode = LocomotionMode.Grounded;

    public LocomotionMode CurrentLocomotionMode => locomotionMode;

    // =============================================================
    // INICIALIZACIÓN
    // =============================================================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ConfigureCharacterPhysics();

        // Evitar que las animaciones muevan al jugador
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // =========================================================
        // INDEPENDIZAR CÁMARA
        // =========================================================

        if (camara != null)
        {
            // Guardar posición relativa al jugador
            offsetCamara = transform.InverseTransformPoint(
                camara.position
            );

            // Conservar la inclinación vertical inicial
            rotacionVertical = Mathf.Clamp(
                Mathf.DeltaAngle(0f, camara.eulerAngles.x),
                -limiteVertical,
                limiteVertical
            );

            // Separar cámara de cualquier jerarquía animada
            camara.SetParent(null, true);

            // Establecer rotación inicial independiente
            camara.rotation = Quaternion.Euler(
                rotacionVertical,
                transform.eulerAngles.y,
                0f
            );
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                 "FirstPersonController: No se asignó una cámara.",
                 this
            );
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =============================================================
    // CONFIGURACIÓN FÍSICA
    // =============================================================

    private void ConfigureCharacterPhysics()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        CapsuleCollider legacyCollider = GetComponent<CapsuleCollider>();

        if (legacyCollider != null)
            legacyCollider.enabled = false;

        controller.center = Vector3.up * (controller.height * 0.5f);

        controller.skinWidth = Mathf.Min(
            0.08f,
            controller.radius * 0.2f
        );
    }

    // =============================================================
    // UPDATE
    // =============================================================

    private void Update()
    {
        ActualizarEstadoAgua();

        if (!inputLocked)
        {
            Movimiento();
            ActualizarRotacionMouse();
        }

        ActualizarContactos();
        ActualizarAnimator();
        ActualizarAudioPasos();
    }

    // =============================================================
    // LATE UPDATE - CÁMARA ESTABLE
    // =============================================================

    private void LateUpdate()
    {
        if (camara == null || cameraExternallyControlled)
            return;

        // Seguir únicamente la posición física del jugador
        Vector3 posicionObjetivo =
            transform.TransformPoint(offsetCamara);

        // Rotación controlada exclusivamente por mouse
        Quaternion rotacionObjetivo = Quaternion.Euler(
            rotacionVertical,
            transform.eulerAngles.y,
            0f
        );

        // Aplicar directamente, sin interpolaciones
        camara.SetPositionAndRotation(
            posicionObjetivo,
            rotacionObjetivo
        );
    }

    // =============================================================
    // MOVIMIENTO
    // =============================================================

    private void Movimiento()
    {
        // =========================================================
        // NATACIÓN Y BUCEO
        // =========================================================

        if (locomotionMode != LocomotionMode.Grounded)
        {
            MoverEnAgua();
            return;
        }

        intensidadMovimientoAgua = 0f;

        Vector3 movimiento =
            transform.right * movimientoInput.x +
            transform.forward * movimientoInput.y;

        // Limitar velocidad diagonal
        movimiento = Vector3.ClampMagnitude(movimiento, 1f);

        // =========================================================
        // SPRINT
        // =========================================================

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

        // =========================================================
        // GRAVEDAD
        // =========================================================

        if (controller.isGrounded && velocidadVertical < 0f)
        {
            velocidadVertical = -2f;
        }

        // =========================================================
        // SALTO
        // =========================================================

        if (
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            controller.isGrounded &&
            DetectarSuelo() &&
            !agachado
        )
        {
            // Activar inmediatamente animación de salto
            if (animator != null)
            {
                animator.SetTrigger(parametroSalto);
            }

            // Aplicar fuerza de salto
            velocidadVertical = Mathf.Sqrt(
                fuerzaSalto * -2f * gravedad
            );

            // Sonido de salto
            if (audioSalto != null)
            {
                audioSalto.Play();
            }
        }

        velocidadVertical += gravedad * Time.deltaTime;

        movimiento.y = velocidadVertical;

        controller.Move(
            movimiento * Time.deltaTime
        );
    }

    private void MoverEnAgua()
    {
        bool subir = Keyboard.current != null &&
                     Keyboard.current.spaceKey.isPressed;
        bool bajar = Keyboard.current != null &&
                     Keyboard.current.leftCtrlKey.isPressed;

        float entradaVertical = (subir ? 1f : 0f) - (bajar ? 1f : 0f);
        Vector3 velocidadDeseada;

        if (locomotionMode == LocomotionMode.Diving)
        {
            Vector3 frente = camara != null ? camara.forward : transform.forward;
            Vector3 derecha = camara != null ? camara.right : transform.right;

            Vector3 direccion =
                derecha * movimientoInput.x +
                frente * movimientoInput.y +
                Vector3.up * entradaVertical;

            direccion = Vector3.ClampMagnitude(direccion, 1f);
            velocidadDeseada = direccion * velocidadBuceo;
            intensidadMovimientoAgua = direccion.magnitude;
        }
        else
        {
            Vector3 direccionHorizontal =
                transform.right * movimientoInput.x +
                transform.forward * movimientoInput.y;
            direccionHorizontal.y = 0f;
            direccionHorizontal = Vector3.ClampMagnitude(direccionHorizontal, 1f);

            float velocidadY;
            float alturaObjetivo = ObtenerAlturaFlotacion();

            if (bajar)
            {
                velocidadY = -velocidadVerticalNado;
            }
            else if (subir && transform.position.y < alturaObjetivo)
            {
                velocidadY = velocidadVerticalNado;
            }
            else
            {
                velocidadY = Mathf.Clamp(
                    (alturaObjetivo - transform.position.y) * fuerzaFlotacion,
                    -velocidadVerticalNado,
                    velocidadVerticalNado
                );
            }

            velocidadDeseada =
                direccionHorizontal * velocidadNado +
                Vector3.up * velocidadY;

            intensidadMovimientoAgua = Mathf.Clamp01(
                Mathf.Max(direccionHorizontal.magnitude, Mathf.Abs(entradaVertical))
            );
        }

        velocidadVertical = 0f;
        controller.Move(velocidadDeseada * Time.deltaTime);
    }

    private float ObtenerAlturaFlotacion()
    {
        if (volumenAguaActual == null)
            return transform.position.y;

        float alturaOjos = camara != null
            ? offsetCamara.y
            : controller.height * 0.8f;

        return volumenAguaActual.SurfaceY +
               margenCabezaSobreAgua -
               alturaOjos;
    }

    // =============================================================
    // DETECCIÓN DE SUELO
    // =============================================================

    private bool DetectarSuelo()
    {
        Vector3 centro =
            transform.TransformPoint(controller.center);

        Vector3 pies =
            centro -
            Vector3.up * (controller.height * 0.5f);

        float radio = controller.radius * 0.9f;

        Vector3 puntoDeteccion =
            pies + Vector3.up * margenDeteccionSuelo;

        return Physics.CheckSphere(
            puntoDeteccion,
            radio,
            capasSuelo,
            QueryTriggerInteraction.Ignore
        );
    }

    // =============================================================
    // DETECCIÓN DE AGUA
    // =============================================================

    private bool DetectarAgua()
    {
        Vector3 centro =
            transform.TransformPoint(controller.center);

        float mitadCapsula = Mathf.Max(
            0f,
            controller.height * 0.5f - controller.radius
        );

        Vector3 puntoSuperior =
            centro + Vector3.up * mitadCapsula;

        Vector3 puntoInferior =
            centro - Vector3.up * mitadCapsula;

        return Physics.CheckCapsule(
            puntoInferior,
            puntoSuperior,
            controller.radius,
            capasAgua,
            QueryTriggerInteraction.Collide
        );
    }

    // =============================================================
    // ACTUALIZAR CONTACTOS
    // =============================================================

    private void ActualizarContactos()
    {
        onGround =
            controller.isGrounded &&
            DetectarSuelo();

        onWater = locomotionMode != LocomotionMode.Grounded;
    }

    // =============================================================
    // ANIMATOR
    // =============================================================

    private void ActualizarAnimator()
    {
        Vector3 velocidadMovimiento = controller.velocity;

        if (inputLocked)
        {
            velocidadReal = 0f;
        }
        else if (locomotionMode == LocomotionMode.Grounded)
        {
            // Ignorar velocidad vertical al caminar
            velocidadMovimiento.y = 0f;

            velocidadReal = velocidadMovimiento.magnitude;
        }
        else
        {
            // La flotación puede mover al CharacterController aunque el
            // jugador esté quieto. Para el blend de agua usamos solo input.
            velocidadReal = intensidadMovimientoAgua;
        }

        if (animator == null)
            return;

        // Velocidad
        animator.SetFloat(
            parametroVelocidad,
            velocidadReal
        );

        // Contacto con suelo
        animator.SetBool(
            parametroSuelo,
            onGround
        );

        // Contacto con agua
        animator.SetBool(
            parametroAgua,
            onWater
        );
    }

    // =============================================================
    // AUDIO DE PASOS
    // =============================================================

    private void ActualizarAudioPasos()
    {
        if (pasos == null)
            return;

        // No reproducir pasos si el personaje está en el aire,
        // nadando o con controles bloqueados
        if (
            inputLocked ||
            !onGround ||
            locomotionMode != LocomotionMode.Grounded
        )
        {
            if (pasos.isPlaying)
                pasos.Pause();

            pasos.pitch = 1f;

            return;
        }

        bool moviendose = velocidadReal > 0.1f;

        bool corriendo =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            movimientoInput.y > 0 &&
            !agachado;

        if (moviendose)
        {
            pasos.pitch = corriendo ? 1.5f : 1f;

            if (!pasos.isPlaying)
                pasos.Play();
        }
        else
        {
            if (pasos.isPlaying)
                pasos.Pause();
        }
    }

    // =============================================================
    // ROTACIÓN POR MOUSE
    // =============================================================

    private void ActualizarRotacionMouse()
    {
        float mouseX =
            mouseInput.x * sensibilidadMouse;

        float mouseY =
            mouseInput.y * sensibilidadMouse;

        // Rotación horizontal del jugador
        transform.Rotate(
            Vector3.up * mouseX
        );

        // Rotación vertical de cámara
        rotacionVertical -= mouseY;

        rotacionVertical = Mathf.Clamp(
            rotacionVertical,
            -limiteVertical,
            limiteVertical
        );

        // La rotación de la cámara se aplica en LateUpdate()
    }

    // =============================================================
    // INPUT SYSTEM
    // =============================================================

    public void OnMove(InputValue value)
    {
        movimientoInput = inputLocked
            ? Vector2.zero
            : value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        mouseInput = inputLocked
            ? Vector2.zero
            : value.Get<Vector2>();
    }

    // =============================================================
    // BLOQUEO DE CONTROLES
    // =============================================================

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            movimientoInput = Vector2.zero;
            mouseInput = Vector2.zero;

            if (pasos != null && pasos.isPlaying)
                pasos.Pause();
        }
    }

    /// <summary>
    /// Permite que sistemas temporales, como la vista de emotes, controlen la
    /// cámara sin competir con el seguimiento de primera persona.
    /// </summary>
    public void SetExternalCameraControl(bool externalControl)
    {
        cameraExternallyControlled = externalControl;
    }

    // =============================================================
    // LOCOMOCIÓN
    // =============================================================

    public void EnterWater(WaterVolume water)
    {
        if (water == null)
            return;

        volumenesAguaActivos.Add(water);
        SeleccionarVolumenAguaActual();
    }

    public void ExitWater(WaterVolume water)
    {
        if (water == null)
            return;

        volumenesAguaActivos.Remove(water);
        SeleccionarVolumenAguaActual();

        if (volumenAguaActual == null)
            SetLocomotionMode(LocomotionMode.Grounded);
    }

    private void ActualizarEstadoAgua()
    {
        bool estabaSiguiendoUnVolumen = volumenAguaActual != null ||
                                        volumenesAguaActivos.Count > 0;

        volumenesAguaActivos.RemoveWhere(
            water => water == null || !water.isActiveAndEnabled
        );
        SeleccionarVolumenAguaActual();

        if (volumenAguaActual == null)
        {
            if (estabaSiguiendoUnVolumen)
                SetLocomotionMode(LocomotionMode.Grounded);

            return;
        }

        float alturaEntrada =
            transform.position.y + controller.height * alturaActivacionNado;

        if (locomotionMode == LocomotionMode.Grounded)
        {
            if (alturaEntrada <= volumenAguaActual.SurfaceY)
                SetLocomotionMode(LocomotionMode.Swimming);
            else
                return;
        }

        bool quiereBajar = Keyboard.current != null &&
                           Keyboard.current.leftCtrlKey.isPressed;
        // Usar la posición física de los ojos, no la posición actual de la
        // cámara: durante un emote la cámara puede estar orbitando lejos.
        float alturaOjos = camara != null
            ? transform.position.y + offsetCamara.y
            : transform.position.y + controller.height * 0.8f;

        locomotionMode = quiereBajar ||
                         alturaOjos < volumenAguaActual.SurfaceY - profundidadParaBucear
            ? LocomotionMode.Diving
            : LocomotionMode.Swimming;
    }

    private void SeleccionarVolumenAguaActual()
    {
        volumenAguaActual = null;

        foreach (WaterVolume water in volumenesAguaActivos)
        {
            if (water == null)
                continue;

            if (volumenAguaActual == null ||
                water.SurfaceY > volumenAguaActual.SurfaceY)
            {
                volumenAguaActual = water;
            }
        }
    }

    public void SetLocomotionMode(LocomotionMode mode)
    {
        locomotionMode = mode;
        velocidadVertical = 0f;

        if (mode == LocomotionMode.Grounded)
            intensidadMovimientoAgua = 0f;
    }
}
