using UnityEngine;

namespace InventorySystem
{
    /// <summary>Estación de venta: abre la mochila y compra el lote seleccionado.</summary>
    public class SellTable : MonoBehaviour
    {
        [SerializeField] private string stationName = "Mesa de recuperación";
        public string StationName => stationName;

        public bool TrySell(Inventory inventory, Wallet wallet, ItemInstance item, out int earned)
        {
            return TrySellAmount(inventory, wallet, item, item == null ? 0 : item.Amount, out earned);
        }

        public bool TrySellAmount(Inventory inventory, Wallet wallet, ItemInstance item, int amount, out int earned)
        {
            earned = 0;
            if (inventory == null || wallet == null || item == null || item.Definition == null)
                return false;
            if (item.Definition.SellPrice <= 0 || amount <= 0 || amount > item.Amount)
                return false;

            earned = item.Definition.SellPrice * amount;
            if (!inventory.TryRemoveAmount(item, amount))
            {
                earned = 0;
                return false;
            }

            wallet.AddFunds(earned);
            return true;
        }
    }
}
