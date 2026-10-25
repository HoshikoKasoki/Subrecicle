using InventorySystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EmoteSceneBuilder
{
    private const string CanvasName = "Canvas - Emotes (Provisional)";

    [MenuItem("Tools/Subrecicle/Crear UI provisional de emotes")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        FirstPersonController movement = Object.FindFirstObjectByType<FirstPersonController>();
        if (movement == null)
            throw new MissingReferenceException("No se encontró FirstPersonController en la escena activa.");

        GameObject oldCanvas = FindSceneObject(CanvasName);
        if (oldCanvas != null)
        {
            Button[] existingButtons = FindEmoteButtons(oldCanvas.transform);
            ConnectController(movement, oldCanvas, existingButtons);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("UI existente conservada y sus seis emotes fueron reconectados en " + scene.path + ".");
            return;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dimmer = CreateImage(
            "Fondo tenue",
            canvasObject.transform,
            new Color(0.015f, 0.035f, 0.04f, 0.52f));
        Stretch(dimmer.GetComponent<RectTransform>());

        GameObject panel = CreateImage(
            "Panel Emotes",
            dimmer.transform,
            new Color(0.055f, 0.12f, 0.13f, 0.98f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 430f);
        panelRect.anchoredPosition = Vector2.zero;

        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.2f, 0.78f, 0.66f, 0.8f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        Text title = CreateText("Título", panel.transform, "EMOTES", 42, FontStyle.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -70f), new Vector2(-60f, 70f));
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.77f, 1f, 0.9f, 1f);

        Text subtitle = CreateText(
            "Subtítulo",
            panel.transform,
            "Selecciona un emote para activar la cámara orbital",
            21,
            FontStyle.Normal);
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -118f), new Vector2(-60f, 44f));
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = new Color(0.64f, 0.78f, 0.76f, 1f);

        Button[] emoteButtons = new Button[6];
        for (int i = 0; i < emoteButtons.Length; i++)
        {
            emoteButtons[i] = CreateButton(panel.transform, "Emote " + (i + 1), "EMOTE " + (i + 1));
            RectTransform buttonRect = emoteButtons[i].GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(200f, 76f);
            buttonRect.anchoredPosition = new Vector2(
                -220f + (i % 3) * 220f,
                35f - (i / 3) * 95f);
        }

        Text hint = CreateText(
            "Ayuda",
            panel.transform,
            "R / ESC  CERRAR   •   MUEVE EL RATÓN PARA ROTAR",
            18,
            FontStyle.Bold);
        SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 36f), new Vector2(-50f, 42f));
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.47f, 0.7f, 0.65f, 1f);

        ConnectController(movement, canvasObject, emoteButtons);

        canvasObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("UI provisional de emotes creada y conectada en " + scene.path + ".");
    }

    public static void BuildSampleSceneForBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        Build();
    }

    public static void ValidateSampleSceneForBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        FirstPersonController movement = Object.FindFirstObjectByType<FirstPersonController>();
        EmoteController controller = Object.FindFirstObjectByType<EmoteController>(FindObjectsInactive.Include);
        GameObject canvas = FindSceneObject(CanvasName);

        if (movement == null || controller == null || canvas == null)
            throw new MissingReferenceException("La escena no contiene el sistema completo de emotes.");

        SerializedObject serializedController = new SerializedObject(controller);
        string[] requiredReferences =
        {
            "movement", "playerCamera", "emoteCanvas", "emote1Button", "emote2Button", "emote3Button",
            "emote4Button", "emote5Button", "emote6Button", "inventoryUI", "animator"
        };

        foreach (string propertyName in requiredReferences)
        {
            SerializedProperty property = serializedController.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
                throw new MissingReferenceException("Falta la referencia de emotes: " + propertyName);
        }

        if (canvas.activeSelf)
            throw new UnityException("El Canvas de emotes debe comenzar cerrado.");

        Debug.Log("Validación de emotes correcta: Canvas, seis botones, jugador, cámara, inventario y Animator conectados.");
    }

    private static void ConnectController(
        FirstPersonController movement,
        GameObject canvasObject,
        Button[] emoteButtons)
    {
        if (emoteButtons == null || emoteButtons.Length != 6)
            throw new MissingReferenceException("El Canvas debe contener los botones Emote 1 a Emote 6.");

        EmoteController controller = movement.GetComponent<EmoteController>();
        if (controller == null)
            controller = movement.gameObject.AddComponent<EmoteController>();

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("movement").objectReferenceValue = movement;
        serializedController.FindProperty("playerCamera").objectReferenceValue = Camera.main == null
            ? null
            : Camera.main.transform;
        serializedController.FindProperty("emoteCanvas").objectReferenceValue = canvasObject;

        for (int i = 0; i < emoteButtons.Length; i++)
        {
            if (emoteButtons[i] == null)
                throw new MissingReferenceException("No se encontró el botón Emote " + (i + 1) + ".");

            serializedController.FindProperty("emote" + (i + 1) + "Button").objectReferenceValue = emoteButtons[i];
        }

        serializedController.FindProperty("inventoryUI").objectReferenceValue =
            movement.GetComponent<InventoryUIController>();
        serializedController.FindProperty("animator").objectReferenceValue =
            movement.GetComponentInChildren<Animator>();
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static Button[] FindEmoteButtons(Transform root)
    {
        Button[] result = new Button[6];
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < result.Length; i++)
            {
                if (button.name == "Emote " + (i + 1))
                    result[i] = button;
            }
        }

        return result;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name == objectName && candidate.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        child.GetComponent<Image>().color = color;
        return child;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        FontStyle fontStyle)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.transform.SetParent(parent, false);
        Text text = child.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = CreateImage(name, parent, new Color(0.12f, 0.48f, 0.41f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 1f, 0.91f, 1f);
        colors.pressedColor = new Color(0.52f, 0.8f, 0.7f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 1f, 0.82f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text buttonLabel = CreateText("Texto", buttonObject.transform, label, 28, FontStyle.Bold);
        Stretch(buttonLabel.rectTransform);
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        buttonLabel.color = Color.white;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
