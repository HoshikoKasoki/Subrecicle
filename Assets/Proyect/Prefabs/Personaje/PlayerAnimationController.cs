using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private CharacterController characterController;

    private void Awake()
    {
        // Obtenemos los componentes directamente del mismo GameObject
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (animator == null || characterController == null) return;

        // Calculamos la velocidad horizontal (X, Z) ignorando la gravedad en Y
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Pasamos el valor al parámetro Speed del Animator
        animator.SetFloat("Speed", currentSpeed);
    }
}