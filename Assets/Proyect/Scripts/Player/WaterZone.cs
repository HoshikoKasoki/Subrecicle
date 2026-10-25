using UnityEngine;

public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player != null)
        {
            player.SetLocomotionMode(FirstPersonController.LocomotionMode.Swimming);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player != null)
        {
            player.SetLocomotionMode(FirstPersonController.LocomotionMode.Grounded);
        }
    }
}