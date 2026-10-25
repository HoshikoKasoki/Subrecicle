using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FirstPersonController))]
public class PlayerAnimationBridge : MonoBehaviour
{
    [Header("Referencias de Animación")]
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private FirstPersonController fpController;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        fpController = GetComponent<FirstPersonController>();

        // Si no asignaste el Animator manualmente en el Inspector, lo busca en sí mismo o en sus hijos
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (animator == null || controller == null) return;

        // 1. Detectamos si está tocando tierra firme
        bool isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        // 2. Comprobamos si está en modo agua usando la variable de tu FirstPersonController
        bool inWater = fpController.CurrentLocomotionMode != FirstPersonController.LocomotionMode.Grounded;
        animator.SetBool("InWater", inWater);

        // 3. Calculamos la velocidad horizontal (X, Z) ignorando el eje Y
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Ajustamos la escala para el Blend Tree:
        // Si va corriendo en tierra (presionando Shift), ampliamos el rango hacia 2.0f
        if (isGrounded && !inWater && currentSpeed > 0.1f)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed)
            {
                currentSpeed = 2.0f; // Dispara la animación de Running en el Blend Tree
            }
        }

        animator.SetFloat("speed", currentSpeed);

        // 4. Salto (solo si presiona espacio estando en tierra)
        if (isGrounded && !inWater && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animator.SetTrigger("IsJumping");
        }
    }
}