using UnityEngine;

namespace InventorySystem
{
    public class PickupItem : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Interaction")]
        [SerializeField] private float pickupDistance = 2.5f;
        [SerializeField] private bool animate = true;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float rotationSpeed = 65f;
        [Header("Audio")]
        [SerializeField] private AudioClip sonidoRecoger;

        public ItemDefinition ItemDefinition => itemDefinition;
        public int Amount => amount;
        public string DisplayName => itemDefinition == null ? "Objeto desconocido" : itemDefinition.ItemName;

        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(1, newAmount);
        }

        private Vector3 startPosition;

        private void Awake()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (!animate)
                return;

            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public bool CanPickup(Transform player)
        {
            if (player == null || itemDefinition == null)
                return false;

            return Vector3.Distance(transform.position, player.position) <= pickupDistance;
        }

        public bool Pickup(Inventory inventory)
        {
            if (inventory == null || itemDefinition == null)
                return false;

            bool stored = inventory.TryAddItem(itemDefinition, amount);

            if (stored)
            {
                if (sonidoRecoger != null)
                {
                    AudioSource.PlayClipAtPoint(sonidoRecoger, transform.position);
                }
                Destroy(gameObject);
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, pickupDistance);
        }
#endif
    }
}
