using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class InventoryItemUI : MonoBehaviour
    {
        private ItemInstance item;

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        public void Setup(ItemInstance item)
        {
            this.item = item;

            if (image == null)
                image = GetComponent<Image>();

            if (image == null)
            {
                Debug.LogError(
                    "InventoryItemUI necesita un componente Image."
                );

                return;
            }

            if (item == null || item.Definition == null)
            {
                Debug.LogError(
                    "InventoryItemUI recibió un ItemInstance inválido."
                );

                return;
            }

            Sprite icon =
                item.Definition.Icon;

            if (icon != null)
            {
                image.sprite = icon;
                image.enabled = true;
                image.preserveAspect = true;
            }
            else
            {
                // Para poder ver el objeto incluso sin icono.
                image.enabled = true;
                image.sprite = null;
                image.color = Color.white;
            }
        }
    }
}