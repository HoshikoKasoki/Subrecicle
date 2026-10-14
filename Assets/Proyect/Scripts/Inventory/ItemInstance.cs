using System;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private int amount = 1;
        [SerializeField] private Vector2Int position;
        [SerializeField] private bool rotated;

        public ItemDefinition Definition => definition;
        public int Amount => amount;
        public Vector2Int Position => position;
        public bool Rotated => rotated;

        public Vector2Int Size
        {
            get
            {
                if (definition == null)
                    return Vector2Int.one;

                Vector2Int size = definition.Size;
                return rotated ? new Vector2Int(size.y, size.x) : size;
            }
        }

        public ItemInstance(ItemDefinition definition, int amount = 1)
        {
            this.definition = definition;
            this.amount = Mathf.Max(1, amount);
            position = Vector2Int.zero;
            rotated = false;
        }

        public void SetPosition(Vector2Int newPosition)
        {
            position = newPosition;
        }

        public void SetRotation(bool isRotated)
        {
            rotated = isRotated;
        }

        public void Rotate()
        {
            rotated = !rotated;
        }

        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(1, newAmount);
        }

        public void AddAmount(int value)
        {
            amount = Mathf.Max(1, amount + value);
        }

        public ItemInstance Copy()
        {
            ItemInstance copy = new ItemInstance(definition, amount);
            copy.SetPosition(position);
            copy.SetRotation(rotated);
            return copy;
        }

        public bool IsSameDefinition(ItemDefinition other)
        {
            return definition == other;
        }
    }
}
