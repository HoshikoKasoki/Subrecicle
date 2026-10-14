using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class Inventory : MonoBehaviour
    {
        // =========================================================
        // GRID
        // =========================================================

        // Inventario fijo de 4x4 = 16 espacios.
        public const int InventoryWidth = 4;
        public const int InventoryHeight = 4;
        public const int InventoryCapacity = InventoryWidth * InventoryHeight;

        private readonly List<ItemInstance> items = new List<ItemInstance>();

        public int Width => InventoryWidth;
        public int Height => InventoryHeight;
        public int Capacity => InventoryCapacity;

        public IReadOnlyList<ItemInstance> Items => items;

        public event Action OnInventoryChanged;

        // =========================================================
        // ADD
        // =========================================================

        public bool TryAddItem(ItemDefinition definition, int amount = 1)
        {
            if (definition == null || amount <= 0)
                return false;

            int remaining = amount;

            List<ItemInstance> stacksToFill = new List<ItemInstance>();
            List<int> amountsToAdd = new List<int>();

            if (definition.Stackable)
            {
                for (int i = 0; i < items.Count && remaining > 0; i++)
                {
                    ItemInstance existing = items[i];

                    if (!existing.IsSameDefinition(definition))
                        continue;

                    int freeSpace = definition.MaxStack - existing.Amount;

                    if (freeSpace <= 0)
                        continue;

                    int amountToAdd = Mathf.Min(freeSpace, remaining);

                    stacksToFill.Add(existing);
                    amountsToAdd.Add(amountToAdd);

                    remaining -= amountToAdd;
                }
            }

            List<ItemInstance> additions = new List<ItemInstance>();

            while (remaining > 0)
            {
                if (!TryFindFreePosition(
                        definition,
                        additions,
                        out Vector2Int position,
                        out bool rotated))
                {
                    return false;
                }

                int stackAmount = definition.Stackable
                    ? Mathf.Min(definition.MaxStack, remaining)
                    : 1;

                ItemInstance newItem =
                    new ItemInstance(definition, stackAmount);

                newItem.SetPosition(position);
                newItem.SetRotation(rotated);

                additions.Add(newItem);

                remaining -= stackAmount;
            }

            for (int i = 0; i < stacksToFill.Count; i++)
            {
                stacksToFill[i].AddAmount(amountsToAdd[i]);
            }

            items.AddRange(additions);

            OnInventoryChanged?.Invoke();
            return true;
        }

        // =========================================================
        // REMOVE
        // =========================================================

        public bool TryRemoveItem(ItemInstance item)
        {
            if (item == null)
                return false;

            if (!items.Remove(item))
                return false;

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveAmount(ItemInstance item, int amount)
        {
            if (item == null ||
                amount <= 0 ||
                !items.Contains(item) ||
                amount > item.Amount)
            {
                return false;
            }

            if (amount == item.Amount)
                items.Remove(item);
            else
                item.SetAmount(item.Amount - amount);

            OnInventoryChanged?.Invoke();
            return true;
        }

        // =========================================================
        // MOVE
        // =========================================================

        public bool TryMoveItem(
            ItemInstance item,
            Vector2Int newPosition,
            bool rotated)
        {
            if (item == null || !items.Contains(item))
                return false;

            Vector2Int oldPosition = item.Position;
            bool oldRotation = item.Rotated;

            item.SetPosition(newPosition);
            item.SetRotation(rotated);

            if (!IsPositionValid(item))
            {
                item.SetPosition(oldPosition);
                item.SetRotation(oldRotation);

                return false;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        // =========================================================
        // ROTATE
        // =========================================================

        public bool TryRotateItem(ItemInstance item)
        {
            if (item == null || !items.Contains(item))
                return false;

            bool oldRotation = item.Rotated;

            item.Rotate();

            if (!IsPositionValid(item))
            {
                item.SetRotation(oldRotation);

                return false;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        // =========================================================
        // POSITION
        // =========================================================

        public bool CanPlace(
            ItemDefinition definition,
            Vector2Int position,
            bool rotated = false,
            ItemInstance ignoredItem = null)
        {
            if (definition == null)
                return false;

            Vector2Int size = rotated
                ? new Vector2Int(
                    definition.Size.y,
                    definition.Size.x)
                : definition.Size;

            // Fuera del inventario 4x4.
            if (position.x < 0 ||
                position.y < 0 ||
                position.x + size.x > Width ||
                position.y + size.y > Height)
            {
                return false;
            }

            // Comprobar colisiones.
            foreach (ItemInstance item in items)
            {
                if (item == null || item == ignoredItem)
                    continue;

                if (RectsOverlap(
                    position,
                    size,
                    item.Position,
                    item.Size))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsPositionValid(ItemInstance item)
        {
            if (item == null)
                return false;

            return CanPlace(
                item.Definition,
                item.Position,
                item.Rotated,
                item);
        }

        // =========================================================
        // FIND FREE POSITION
        // =========================================================

        public bool TryFindFreePosition(
            ItemDefinition definition,
            out Vector2Int position,
            out bool rotated)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);

                    if (CanPlace(
                        definition,
                        candidate,
                        false))
                    {
                        position = candidate;
                        rotated = false;

                        return true;
                    }

                    if (definition.Size.x != definition.Size.y &&
                        CanPlace(
                            definition,
                            candidate,
                            true))
                    {
                        position = candidate;
                        rotated = true;

                        return true;
                    }
                }
            }

            position = Vector2Int.zero;
            rotated = false;

            return false;
        }

        // =========================================================
        // GRID
        // =========================================================

        public bool IsCellOccupied(
            Vector2Int cell,
            ItemInstance ignoredItem = null)
        {
            foreach (ItemInstance item in items)
            {
                if (item == null || item == ignoredItem)
                    continue;

                if (CellInsideItem(cell, item))
                    return true;
            }

            return false;
        }

        public ItemInstance GetItemAtCell(Vector2Int cell)
        {
            foreach (ItemInstance item in items)
            {
                if (item != null && CellInsideItem(cell, item))
                    return item;
            }

            return null;
        }

        public int UsedCells
        {
            get
            {
                int total = 0;

                foreach (ItemInstance item in items)
                {
                    if (item != null)
                        total += item.Size.x * item.Size.y;
                }

                return total;
            }
        }

        // =========================================================
        // INTERNAL PLACEMENT
        // =========================================================

        private bool TryFindFreePosition(
            ItemDefinition definition,
            List<ItemInstance> plannedItems,
            out Vector2Int position,
            out bool rotated)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int candidate =
                        new Vector2Int(x, y);

                    if (CanPlaceWithPlanned(
                        definition,
                        candidate,
                        false,
                        plannedItems))
                    {
                        position = candidate;
                        rotated = false;

                        return true;
                    }

                    if (definition.Size.x != definition.Size.y &&
                        CanPlaceWithPlanned(
                            definition,
                            candidate,
                            true,
                            plannedItems))
                    {
                        position = candidate;
                        rotated = true;

                        return true;
                    }
                }
            }

            position = Vector2Int.zero;
            rotated = false;

            return false;
        }

        private bool CanPlaceWithPlanned(
            ItemDefinition definition,
            Vector2Int position,
            bool rotated,
            List<ItemInstance> plannedItems)
        {
            if (!CanPlace(definition, position, rotated))
                return false;

            Vector2Int size = rotated
                ? new Vector2Int(
                    definition.Size.y,
                    definition.Size.x)
                : definition.Size;

            foreach (ItemInstance planned in plannedItems)
            {
                if (RectsOverlap(
                    position,
                    size,
                    planned.Position,
                    planned.Size))
                {
                    return false;
                }
            }

            return true;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private static bool CellInsideItem(
            Vector2Int cell,
            ItemInstance item)
        {
            Vector2Int position = item.Position;
            Vector2Int size = item.Size;

            return
                cell.x >= position.x &&
                cell.x < position.x + size.x &&
                cell.y >= position.y &&
                cell.y < position.y + size.y;
        }

        private static bool RectsOverlap(
            Vector2Int aPosition,
            Vector2Int aSize,
            Vector2Int bPosition,
            Vector2Int bSize)
        {
            return
                aPosition.x < bPosition.x + bSize.x &&
                aPosition.x + aSize.x > bPosition.x &&
                aPosition.y < bPosition.y + bSize.y &&
                aPosition.y + aSize.y > bPosition.y;
        }
    }
}