using System.Text.RegularExpressions;

namespace InventorySystem
{
    public static class ItemIDUtility
    {
        // Formato canónico: MAT-WOOD-001, TOOL-DIVE-010, KEY-DOOR-001.
        // El formato previo WOOD_001 se acepta mientras se migran assets existentes.
        public static bool IsValid(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            return Regex.IsMatch(
                id,
                @"^([A-Z]{2,12}-[A-Z0-9]{2,16}-\d{3}|(WOOD|METAL|PAPER|PLASTIC|PORCELAIN)_\d{3})$"
            );
        }

        public static bool CategoryMatchesID(string id, ItemCategory category)
        {
            if (!IsValid(id))
                return false;

            string prefix = category.ToString().ToUpperInvariant();
            return id.StartsWith(prefix + "_") || id.Contains("-" + prefix + "-");
        }
    }
}
