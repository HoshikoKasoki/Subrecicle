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
            earned = 0;
            if (inventory == null || wallet == null || item == null || item.Definition == null) return false;
            if (item.Definition.SellPrice <= 0) return false;

            earned = item.Definition.SellPrice * item.Amount;
            if (!inventory.TryRemoveAmount(item, item.Amount))
            {
                earned = 0;
                return false;
            }

            wallet.AddFunds(earned);
            return true;
        }
    }
}
