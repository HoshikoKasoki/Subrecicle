using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Máquina de reciclaje / fabricación.
    ///
    /// No utiliza IInteractable.
    /// No necesita Canvas propio.
    /// Utiliza el mismo InventoryUIController que SellTable.
    ///
    /// Las recetas utilizan ItemDefinition directamente.
    /// Cada ingrediente y resultado tiene:
    /// - ItemDefinition
    /// - cantidad
    ///
    /// Permite:
    /// varios ingredientes -> varios resultados
    /// fabricar varias veces
    /// devolver los resultados al inventario
    /// </summary>
    public class RecyclingMachine : MonoBehaviour
    {
        public enum FabricatorCanvas
        {
            Fabricadora1,
            Fabricadora2
        }

        // =========================================================
        // RECIPE PART
        // =========================================================

        [Serializable]
        public class RecipePart
        {
            [Tooltip("ItemDefinition utilizado por la receta.")]
            public ItemDefinition item;

            [Min(1)]
            public int amount = 1;

            public string ItemID
            {
                get
                {
                    if (item == null)
                        return string.Empty;

                    return item.ItemID;
                }
            }

            // Compatibilidad con código anterior.
            public string itemId
            {
                get
                {
                    return ItemID;
                }
            }
        }

        // =========================================================
        // RECIPE
        // =========================================================

        [Serializable]
        public class CraftingRecipe
        {
            [Header("Información")]
            public string recipeName = "Nueva receta";

            [TextArea(2, 5)]
            public string description;

            [Header("Ingredientes")]
            public List<RecipePart> ingredients =
                new List<RecipePart>();

            [Header("Resultados")]
            public List<RecipePart> outputs =
                new List<RecipePart>();
        }

        // =========================================================
        // CONFIGURACIÓN
        // =========================================================

        [Header("Estación")]
        [SerializeField]
        private string stationName = "Recicladora";

        [Header("Interfaz")]
        [Tooltip("Canvas físico que abrirá esta máquina al hacer clic izquierdo.")]
        [SerializeField]
        private FabricatorCanvas interfaceCanvas = FabricatorCanvas.Fabricadora1;

        [Header("Procesamiento")]
        [SerializeField, Min(0f)]
        private float tiempoProcesamiento = 3.5f;

        [Header("Recetas")]
        [SerializeField]
        private List<CraftingRecipe> recipes =
            new List<CraftingRecipe>();
        // =========================================================
        // AUDIO DE LA MÁQUINA
        // =========================================================
        [Header("Audio de la Máquina")]
        [SerializeField] private AudioSource audioSourceMaquina; 
        [SerializeField] private AudioClip sonidoFabricando;     
        [SerializeField] private AudioClip sonidoCompletado;

        // =========================================================
        // ESTADO
        // =========================================================

        private InventoryUIController inventoryUI;

        private bool estaProcesando;

        private float progreso;

        private string estadoActual =
            "Selecciona una receta.";

        private CraftingRecipe recetaProcesando;

        private int cantidadProcesando;

        public string StationName
        {
            get
            {
                return stationName;
            }
        }

        public FabricatorCanvas InterfaceCanvas
        {
            get
            {
                return interfaceCanvas;
            }
        }

        public bool IsProcessing
        {
            get
            {
                return estaProcesando;
            }
        }

        public float Progress
        {
            get
            {
                return progreso;
            }
        }

        public string StatusMessage
        {
            get
            {
                return estadoActual;
            }
        }

        public IReadOnlyList<CraftingRecipe> Recipes
        {
            get
            {
                return recipes;
            }
        }

        public int RecipeCount
        {
            get
            {
                if (recipes == null)
                    return 0;

                return recipes.Count;
            }
        }

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            inventoryUI =
                FindFirstObjectByType<InventoryUIController>();
        }

        public void Interactuar()
        {
            if (estaProcesando)
                return;

            if (inventoryUI == null)
            {
                inventoryUI =
                    FindFirstObjectByType<InventoryUIController>();
            }

            if (inventoryUI == null)
            {
                Debug.LogWarning(
                    "[RecyclingMachine] " +
                    "No se encontró InventoryUIController."
                );

                return;
            }

            inventoryUI.OpenRecycleMode(this);
        }

        // =========================================================
        // RECETAS
        // =========================================================

        public CraftingRecipe GetRecipe(int index)
        {
            if (recipes == null)
                return null;

            if (index < 0 ||
                index >= recipes.Count)
            {
                return null;
            }

            return recipes[index];
        }

        // =========================================================
        // COMPROBAR SI SE PUEDE FABRICAR
        // =========================================================

        public bool CanCraft(
            Inventory inventory,
            int recipeIndex,
            int recipeAmount)
        {
            return CanCraft(
                inventory,
                GetRecipe(recipeIndex),
                recipeAmount
            );
        }

        public bool CanCraft(
            Inventory inventory,
            CraftingRecipe recipe,
            int recipeAmount)
        {
            if (inventory == null)
                return false;

            if (recipe == null)
                return false;

            if (recipeAmount <= 0)
                return false;

            if (recipe.ingredients == null ||
                recipe.ingredients.Count == 0)
            {
                return false;
            }

            if (recipe.outputs == null ||
                recipe.outputs.Count == 0)
            {
                return false;
            }

            // -----------------------------------------------------
            // INGREDIENTES
            // -----------------------------------------------------

            for (
                int i = 0;
                i < recipe.ingredients.Count;
                i++)
            {
                RecipePart ingredient =
                    recipe.ingredients[i];

                if (ingredient == null ||
                    ingredient.item == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                        ingredient.item.ItemID))
                {
                    return false;
                }

                int required =
                    ingredient.amount *
                    recipeAmount;

                int available =
                    GetTotalAmountById(
                        inventory,
                        ingredient.item.ItemID
                    );

                if (available < required)
                    return false;
            }

            // -----------------------------------------------------
            // RESULTADOS
            // -----------------------------------------------------

            for (
                int i = 0;
                i < recipe.outputs.Count;
                i++)
            {
                RecipePart output =
                    recipe.outputs[i];

                if (output == null ||
                    output.item == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                        output.item.ItemID))
                {
                    return false;
                }
            }

            return true;
        }

        // =========================================================
        // INICIAR FABRICACIÓN
        // =========================================================

        public bool TryStartCraft(
            Inventory inventory,
            int recipeIndex,
            int recipeAmount)
        {
            if (estaProcesando)
                return false;

            CraftingRecipe recipe =
                GetRecipe(recipeIndex);

            if (!CanCraft(
                    inventory,
                    recipe,
                    recipeAmount))
            {
                SetStatus(
                    "No tienes los materiales suficientes."
                );

                return false;
            }

            recetaProcesando =
                recipe;

            cantidadProcesando =
                recipeAmount;

            progreso = 0f;

            if (tiempoProcesamiento <= 0f)
            {
                CompleteCraft(
                    inventory
                );

                return true;
            }

            StartCoroutine(
                CraftCoroutine(
                    inventory
                )
            );

            return true;
        }

        // =========================================================
        // PROCESAMIENTO
        // =========================================================

        private IEnumerator CraftCoroutine(
            Inventory inventory)
        {
            estaProcesando = true;
            // =========================================================
            // REPRODUCIR SONIDO DE FABRICACIÓN (EN BUCLE)
            // =========================================================
            if (audioSourceMaquina != null && sonidoFabricando != null)
            {
                audioSourceMaquina.clip = sonidoFabricando;
                audioSourceMaquina.loop = true;
                audioSourceMaquina.Play();
            }
            // =========================================================

            float elapsed = 0f;

            estadoActual =
                "⚙ Fabricando...";

            NotifyUI();

            while (
                elapsed <
                tiempoProcesamiento)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                progreso =
                    Mathf.Clamp01(
                        elapsed /
                        tiempoProcesamiento
                    );

                int percent =
                    Mathf.RoundToInt(
                        progreso * 100f
                    );

                estadoActual =
                    "⚙ Fabricando... " +
                    percent +
                    "%";

                NotifyUI();

                yield return null;
            }

            progreso = 1f;

            CompleteCraft(
                inventory
            );
        }

        // =========================================================
        // COMPLETAR
        // =========================================================

        private void CompleteCraft(
            Inventory inventory)
        {
            // Detener el sonido de fabricación
            if (audioSourceMaquina != null && audioSourceMaquina.isPlaying)
            {
                audioSourceMaquina.Stop();
                audioSourceMaquina.loop = false;
            }
            if (inventory == null ||
                recetaProcesando == null)
            {
                CancelProcessing(
                    "No se pudo completar la fabricación."
                );

                return;
            }

            CraftingRecipe recipe =
                recetaProcesando;

            int multiplier =
                Mathf.Max(
                    1,
                    cantidadProcesando
                );

            // -----------------------------------------------------
            // REVALIDAR
            // -----------------------------------------------------

            if (!CanCraft(
                    inventory,
                    recipe,
                    multiplier))
            {
                CancelProcessing(
                    "Los materiales ya no están disponibles."
                );

                return;
            }

            // -----------------------------------------------------
            // LISTA DE INGREDIENTES A CONSUMIR
            // -----------------------------------------------------

            List<RecipePart> consumed =
                new List<RecipePart>();

            for (
                int i = 0;
                i < recipe.ingredients.Count;
                i++)
            {
                RecipePart source =
                    recipe.ingredients[i];

                if (source == null ||
                    source.item == null)
                {
                    CancelProcessing(
                        "La receta contiene un ingrediente inválido."
                    );

                    return;
                }

                RecipePart copy =
                    new RecipePart();

                copy.item =
                    source.item;

                copy.amount =
                    source.amount *
                    multiplier;

                consumed.Add(copy);
            }

            // -----------------------------------------------------
            // CONSUMIR INGREDIENTES
            // -----------------------------------------------------

            for (
                int i = 0;
                i < consumed.Count;
                i++)
            {
                RecipePart part =
                    consumed[i];

                bool removed =
                    RemoveAmountById(
                        inventory,
                        part.ItemID,
                        part.amount
                    );

                if (!removed)
                {
                    RollbackIngredients(
                        inventory,
                        consumed,
                        i
                    );

                    CancelProcessing(
                        "No se pudieron consumir los ingredientes."
                    );

                    return;
                }
            }

            // -----------------------------------------------------
            // AÑADIR RESULTADOS
            // -----------------------------------------------------

            Dictionary<string, int>
                outputsAdded =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            for (
                int i = 0;
                i < recipe.outputs.Count;
                i++)
            {
                RecipePart output =
                    recipe.outputs[i];

                if (output == null ||
                    output.item == null)
                {
                    RollbackCraft(
                        inventory,
                        consumed,
                        outputsAdded
                    );

                    CancelProcessing(
                        "La receta tiene un resultado inválido."
                    );

                    return;
                }

                int amount =
                    output.amount *
                    multiplier;

                // AQUÍ SE AÑADE EL OUTPUT REAL
                // AL MISMO INVENTARIO.

                bool added =
                    inventory.TryAddItem(
                        output.item,
                        amount
                    );

                if (!added)
                {
                    RollbackCraft(
                        inventory,
                        consumed,
                        outputsAdded
                    );

                    CancelProcessing(
                        "No hay espacio suficiente " +
                        "para los resultados."
                    );

                    return;
                }

                string id =
                    output.item.ItemID;

                if (outputsAdded.ContainsKey(id))
                {
                    outputsAdded[id] += amount;
                }
                else
                {
                    outputsAdded.Add(
                        id,
                        amount
                    );
                }
            }

            // -----------------------------------------------------
            // ÉXITO
            // -----------------------------------------------------

            // =========================================================
            // REPRODUCIR SONIDO DE COMPLETADO (UNA SOLA VEZ)
            // =========================================================
            if (audioSourceMaquina != null && sonidoCompletado != null)
            {
                audioSourceMaquina.PlayOneShot(sonidoCompletado);
            }
            // =========================================================
            estadoActual =
                "<color=#69c475>" +
                "¡Fabricación completada!" +
                "</color>\n" +
                BuildOutputText(
                    recipe,
                    multiplier
                );

            recetaProcesando = null;
            cantidadProcesando = 0;

            estaProcesando = false;

            NotifyUI();
        }

        // =========================================================
        // ROLLBACK COMPLETO
        // =========================================================

        private void RollbackCraft(
            Inventory inventory,
            List<RecipePart> consumed,
            Dictionary<string, int> outputsAdded)
        {
            // Primero eliminamos cualquier resultado
            // que sí haya alcanzado a entrar.

            foreach (
                KeyValuePair<string, int> output
                in outputsAdded)
            {
                RemoveAmountById(
                    inventory,
                    output.Key,
                    output.Value
                );
            }

            // Luego devolvemos los ingredientes.

            for (
                int i = 0;
                i < consumed.Count;
                i++)
            {
                RecipePart part =
                    consumed[i];

                if (part == null ||
                    part.item == null)
                {
                    continue;
                }

                inventory.TryAddItem(
                    part.item,
                    part.amount
                );
            }
        }

        private void RollbackIngredients(
            Inventory inventory,
            List<RecipePart> consumed,
            int failedIndex)
        {
            for (
                int i = 0;
                i < failedIndex;
                i++)
            {
                RecipePart part =
                    consumed[i];

                if (part == null ||
                    part.item == null)
                {
                    continue;
                }

                inventory.TryAddItem(
                    part.item,
                    part.amount
                );
            }
        }

        // =========================================================
        // SUMAR CANTIDAD POR ID
        // =========================================================

        public int GetTotalAmountById(
            Inventory inventory,
            string itemId)
        {
            if (inventory == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            int total = 0;

            IReadOnlyList<ItemInstance> items =
                inventory.Items;

            for (
                int i = 0;
                i < items.Count;
                i++)
            {
                ItemInstance item =
                    items[i];

                if (item == null ||
                    item.Definition == null)
                {
                    continue;
                }

                if (string.Equals(
                        item.Definition.ItemID,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    total += item.Amount;
                }
            }

            return total;
        }

        // =========================================================
        // ELIMINAR POR ID
        // =========================================================

        private bool RemoveAmountById(
            Inventory inventory,
            string itemId,
            int amount)
        {
            if (inventory == null ||
                amount <= 0)
            {
                return false;
            }

            int remaining =
                amount;

            IReadOnlyList<ItemInstance>
                readOnlyItems =
                inventory.Items;

            // Copiamos referencias antes de modificar
            // la lista original.

            List<ItemInstance>
                candidates =
                new List<ItemInstance>();

            for (
                int i = 0;
                i < readOnlyItems.Count;
                i++)
            {
                ItemInstance item =
                    readOnlyItems[i];

                if (item == null ||
                    item.Definition == null)
                {
                    continue;
                }

                if (string.Equals(
                        item.Definition.ItemID,
                        itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(item);
                }
            }

            for (
                int i = 0;
                i < candidates.Count &&
                remaining > 0;
                i++)
            {
                ItemInstance item =
                    candidates[i];

                if (item == null)
                    continue;

                int take =
                    Mathf.Min(
                        item.Amount,
                        remaining
                    );

                bool removed =
                    inventory.TryRemoveAmount(
                        item,
                        take
                    );

                if (!removed)
                    return false;

                remaining -=
                    take;
            }

            return remaining <= 0;
        }

        // =========================================================
        // TEXTO DE RESULTADOS
        // =========================================================

        private string BuildOutputText(
            CraftingRecipe recipe,
            int multiplier)
        {
            List<string> result =
                new List<string>();

            if (recipe == null ||
                recipe.outputs == null)
            {
                return string.Empty;
            }

            for (
                int i = 0;
                i < recipe.outputs.Count;
                i++)
            {
                RecipePart output =
                    recipe.outputs[i];

                if (output == null ||
                    output.item == null)
                {
                    continue;
                }

                int amount =
                    output.amount *
                    multiplier;

                result.Add(
                    amount +
                    " × " +
                    output.item.ItemName +
                    " [" +
                    output.item.ItemID +
                    "]"
                );
            }

            return string.Join(
                "\n",
                result
            );
        }

        // =========================================================
        // ESTADO
        // =========================================================

        private void SetStatus(
            string message)
        {
            estadoActual =
                message;

            NotifyUI();
        }

        private void CancelProcessing(
            string message)
        {
            // Asegurarse de detener el sonido si se cancela a mitad de camino
            if (audioSourceMaquina != null && audioSourceMaquina.isPlaying)
            {
                audioSourceMaquina.Stop();
                audioSourceMaquina.loop = false;
            }
            
            recetaProcesando = null;
            cantidadProcesando = 0;

            progreso = 0f;
            estaProcesando = false;

            estadoActual =
                message;

            NotifyUI();
        }

        private void NotifyUI()
        {
            if (inventoryUI == null)
            {
                inventoryUI =
                    FindFirstObjectByType<InventoryUIController>();
            }

            if (inventoryUI != null)
                inventoryUI.RefreshStationUI();
        }
    }
}
