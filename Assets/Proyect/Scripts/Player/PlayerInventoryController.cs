using UnityEngine;
using UnityEngine.InputSystem;

namespace InventorySystem
{
    public class PlayerInventoryController : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private Inventory inventory;

        [Header("Interaction")]
        [SerializeField] private float interactionDistance = 2.5f;
        [SerializeField] private LayerMask pickupLayer = ~0;

        private Camera playerCamera;
        private PickupItem focusedPickup;
        private SellTable focusedSellTable;
        private RecyclingMachine focusedRecyclingMachine;
        private Wallet wallet;

        public Inventory Inventory => inventory;
        public PickupItem FocusedPickup => focusedPickup;
        public Wallet Wallet => wallet;
        public string InteractionMessage { get; private set; }

        private void Awake()
        {
            playerCamera = Camera.main;

            if (inventory == null)
                inventory = GetComponent<Inventory>();

            if (inventory == null)
                Debug.LogError("PlayerInventoryController necesita un componente Inventory.", this);

            wallet = GetComponent<Wallet>();
            if (wallet == null)
                wallet = gameObject.AddComponent<Wallet>();
        }

        private void Update()
        {
            FindInteraction();
            if (focusedPickup != null)
                InteractionMessage = $"[CLIC IZQ.] RECOGER  {focusedPickup.DisplayName}" +
                    (focusedPickup.Amount > 1 ? $"  x{focusedPickup.Amount}" : string.Empty);
            else if (focusedSellTable != null)
                InteractionMessage = $"[CLIC IZQ.] VENDER OBJETOS  //  {focusedSellTable.StationName}";
            else if (focusedRecyclingMachine != null)
                InteractionMessage = focusedRecyclingMachine.IsProcessing
                    ? $"{focusedRecyclingMachine.StationName}  //  PROCESANDO"
                    : $"[CLIC IZQ.] ABRIR FABRICADORA  //  {focusedRecyclingMachine.StationName}";
            else
                InteractionMessage = string.Empty;

            HandleInteraction();
        }

        private void HandleInteraction()
        {
            InventoryUIController inventoryUI = GetComponent<InventoryUIController>();
            if (inventoryUI != null && inventoryUI.IsOpen)
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (inventory == null)
                return;

            if (focusedPickup != null && !focusedPickup.Pickup(inventory))
                InteractionMessage = "INVENTARIO SIN ESPACIO";
            else if (focusedSellTable != null && inventoryUI != null)
                inventoryUI.OpenSellMode(focusedSellTable);
            else if (focusedRecyclingMachine != null)
                focusedRecyclingMachine.Interactuar();
        }

        private void FindInteraction()
        {
            focusedPickup = null;
            focusedSellTable = null;
            focusedRecyclingMachine = null;
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerCamera == null)
                return;

            Ray ray = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            if (Physics.SphereCast(
                ray,
                0.12f,
                out RaycastHit hit,
                interactionDistance,
                pickupLayer))
            {
                focusedPickup = hit.collider.GetComponentInParent<PickupItem>();
                if (focusedPickup == null)
                {
                    // Algunas máquinas reutilizan un prefab que también posee
                    // SellTable. La función de fabricadora debe tener prioridad.
                    focusedRecyclingMachine =
                        hit.collider.GetComponentInParent<RecyclingMachine>();
                    if (focusedRecyclingMachine == null)
                        focusedSellTable = hit.collider.GetComponentInParent<SellTable>();
                }
            }

            if (focusedPickup != null || focusedSellTable != null || focusedRecyclingMachine != null)
                return;

            // Las estaciones usan la misma mirada que RecyclingMachine para que
            // el mensaje visible y la interacción con E apunten al mismo objeto.
            if (Physics.SphereCast(
                ray,
                0.12f,
                out hit,
                interactionDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                focusedRecyclingMachine =
                    hit.collider.GetComponentInParent<RecyclingMachine>();
            }
        }
    }
}
