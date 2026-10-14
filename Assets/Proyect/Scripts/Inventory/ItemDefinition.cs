using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemID;
        [SerializeField] private string itemName;
        [TextArea(2, 4)] [SerializeField] private string description;
        [SerializeField] private ItemCategory category;

        [Header("Inventory")]
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [SerializeField] private bool stackable = false;
        [Min(1)]
        [SerializeField] private int maxStack = 1;

        [Header("Economy")]
        [Min(0)] [SerializeField] private int sellPrice;

        [Header("Visual")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color fallbackColor = Color.white;

        public string ItemID => itemID;
        public string ItemName => itemName;
        public string Description => description;
        public ItemCategory Category => category;
        public Vector2Int Size => size;
        public bool Stackable => stackable;
        public int MaxStack => Mathf.Max(1, maxStack);
        public int SellPrice => Mathf.Max(0, sellPrice);
        public Sprite Icon => icon;
        public Color FallbackColor => fallbackColor;

#if UNITY_EDITOR
        private void OnValidate()
        {
            size.x = Mathf.Max(1, size.x);
            size.y = Mathf.Max(1, size.y);

            if (!stackable)
                maxStack = 1;

            if (!string.IsNullOrWhiteSpace(itemID) && !ItemIDUtility.IsValid(itemID))
                Debug.LogWarning($"ItemDefinition '{name}' tiene un ID inválido: {itemID}", this);

            if (!string.IsNullOrWhiteSpace(itemID) && !ItemIDUtility.CategoryMatchesID(itemID, category))
                Debug.LogWarning($"El ID '{itemID}' no coincide con la categoría {category}.", this);
        }
#endif
    }
}
