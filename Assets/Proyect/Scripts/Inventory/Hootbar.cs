using System;
using UnityEngine;

namespace InventorySystem
{
    public class ToolHotbar : MonoBehaviour
    {
        public const int SlotCount = 8;

        [System.Serializable]
        public struct HotbarSlotData
        {
            public string itemId;
            public int amount;
        }

        [SerializeField]
        private HotbarSlotData[] toolSlots = new HotbarSlotData[SlotCount];

        [SerializeField, Range(0, SlotCount - 1)]
        private int selectedSlot = 0;

        public int SelectedSlot => selectedSlot;

        public event Action OnHotbarChanged;

        [Header("Hand Equipment")]
        [SerializeField] private Transform handPoint;
        private GameObject currentEquippedObject;

        
        public bool HasItemInHand => currentEquippedObject != null;

        public string GetTool(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return null;

            return toolSlots[slot].itemId;
        }

        public int GetAmount(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return 0;

            return toolSlots[slot].amount;
        }

        public bool HasTool(int slot)
        {
            return !string.IsNullOrEmpty(GetTool(slot));
        }

        public void SetTool(int slot, string toolId, int amount = 1)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            toolSlots[slot].itemId = toolId;
            toolSlots[slot].amount = amount;
            OnHotbarChanged?.Invoke();

            if (slot == selectedSlot)
                UpdateEquippedHand();
        }

        public void RemoveAmount(int slot, int amountToRemove)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            toolSlots[slot].amount -= amountToRemove;
            if (toolSlots[slot].amount <= 0)
            {
                ClearSlot(slot);
            }
            else
            {
                OnHotbarChanged?.Invoke();
            }
        }

        public void ClearSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            toolSlots[slot].itemId = string.Empty;
            toolSlots[slot].amount = 0;
            OnHotbarChanged?.Invoke();

            if (slot == selectedSlot)
            {
                UnequipHand();
            }
        }

        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            
            if (selectedSlot == slot)
            {
                if (HasItemInHand)
                {
                    
                    UnequipHand();
                }
                else
                {
                    
                    UpdateEquippedHand();
                }
                return;
            }

            
            selectedSlot = slot;
            OnHotbarChanged?.Invoke();

            UpdateEquippedHand();
        }

        public void UnequipHand()
        {
            if (currentEquippedObject != null)
            {
                Destroy(currentEquippedObject);
                currentEquippedObject = null;
            }
        }

        public void UpdateEquippedHand()
        {
            UnequipHand();

            string activeToolId = GetTool(selectedSlot);
            if (string.IsNullOrEmpty(activeToolId))
                return;

            ItemDefinition def = FindItemDefinitionSafe(activeToolId);
            if (def != null && def.WorldPrefab != null && handPoint != null)
            {
                currentEquippedObject = Instantiate(def.WorldPrefab, handPoint);
                currentEquippedObject.transform.localPosition = Vector3.zero;
                currentEquippedObject.transform.localRotation = Quaternion.identity;

                Rigidbody rb = currentEquippedObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    Collider col = currentEquippedObject.GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        private ItemDefinition FindItemDefinitionSafe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            ItemDefinition[] allDefinitions = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            foreach (var def in allDefinitions)
            {
                if (def != null && def.ItemID == itemId)
                    return def;
            }
            return null;
        }

        private void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                    break;
                }
            }
        }
    }
}
