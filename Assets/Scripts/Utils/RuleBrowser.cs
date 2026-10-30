using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A bar at the bottom of the screen showing one active rule at a time, with a
// button on either side to browse through them. It builds its own canvas, so it
// lies below the message, which covers it while fading in. It is only shown
// while the camera is at its start point, empty until the first rule.
public class RuleBrowser : MonoBehaviour
{
    [Header("Buttons")]
    public Sprite previousSprite;
    public Sprite nextSprite;
    [Tooltip("Mirrors the next sprite horizontally, so both buttons can use the same arrow.")]
    public bool mirrorNextSprite = true;

    [Header("Layout")]
    [Tooltip("Font size relative to the message text.")]
    public float fontScale = 0.8f;
    [Tooltip("Sizes in units of the canvas reference resolution (800 x 600).")]
    public float height = 64f;
    public float buttonSize = 48f;
    public float padding = 8f;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);

    // True while the mouse is over the visible bar, so clicks on the buttons
    // don't also reach the door or window behind it.
    public static bool PointerOverBar { get; private set; }

    private CameraZoom cameraZoom;
    private CanvasGroup messageCanvasGroup;
    private CanvasGroup canvasGroup;
    private RectTransform bar;
    private TextMeshProUGUI text;
    private Button previousButton;
    private Button nextButton;

    private List<(int number, string description)> rules = new List<(int, string)>();
    private int index = 0;

    // message is the UI object of GamePlayBase: its CanvasGroup tells whether
    // a message is showing and its text provides the font.
    public void Init(CameraZoom cameraZoom, GameObject message)
    {
        this.cameraZoom = cameraZoom;
        messageCanvasGroup = message.GetComponent<CanvasGroup>();
        // The message has nothing to click, but its full screen background
        // would catch the clicks meant for the buttons, even while invisible.
        messageCanvasGroup.blocksRaycasts = false;
        Build(message.GetComponentInChildren<TextMeshProUGUI>(true), message.GetComponent<CanvasScaler>());
        Refresh();
    }

    void OnDestroy()
    {
        PointerOverBar = false;
    }

    // Takes over the active rules. With a focusNumber the bar jumps to that
    // rule, otherwise it stays on the rule it showed, or the one that took its place.
    public void SetRules(RuleSet ruleset, int focusNumber = -1)
    {
        int shownNumber = rules.Count > 0 ? rules[index].number : -1;
        rules = ruleset.GetNumberedRules();

        int target = focusNumber > 0 ? focusNumber : shownNumber;
        int found = rules.FindIndex(r => r.number == target);
        index = found >= 0 ? found : Mathf.Clamp(index, 0, Mathf.Max(0, rules.Count - 1));
        Refresh();
    }

    private void Browse(int step)
    {
        index = Mathf.Clamp(index + step, 0, Mathf.Max(0, rules.Count - 1));
        Refresh();
    }

    private void Refresh()
    {
        if (text == null) return;
        text.text = rules.Count > 0 ? $"Rule #{rules[index].number} - {rules[index].description}" : "";
        previousButton.interactable = index > 0;
        nextButton.interactable = index < rules.Count - 1;
    }

    void Update()
    {
        if (canvasGroup == null) return;

        // The message covers the bar, so it only has to stop the buttons from working meanwhile.
        bool show = cameraZoom != null && cameraZoom.IsAtStart;
        bool usable = show && messageCanvasGroup.alpha <= 0f;
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = usable;
        canvasGroup.blocksRaycasts = usable;

        PointerOverBar = canvasGroup.blocksRaycasts && UnityEngine.InputSystem.Mouse.current != null
            && RectTransformUtility.RectangleContainsScreenPoint(bar, UnityEngine.InputSystem.Mouse.current.position.ReadValue());
    }

    private void Build(TextMeshProUGUI messageText, CanvasScaler messageScaler)
    {
        GameObject canvasObject = new GameObject("RuleBrowser", typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Below the message canvas, so a message always covers the bar.
        canvas.sortingOrder = -1;

        // Scales the same way as the message canvas.
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = messageScaler != null ? messageScaler.referenceResolution : new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = messageScaler != null ? messageScaler.matchWidthOrHeight : 0f;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // The full width of the screen, a fixed height, stuck to the bottom.
        bar = CreateRect("Bar", canvasObject.transform);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, height);
        bar.anchoredPosition = Vector2.zero;
        Image background = bar.gameObject.AddComponent<Image>();
        background.color = backgroundColor;

        previousButton = CreateButton("Previous", bar, previousSprite, "<", 0f, messageText);
        nextButton = CreateButton("Next", bar, nextSprite, ">", 1f, messageText);
        if (mirrorNextSprite && nextSprite != null) nextButton.transform.localScale = new Vector3(-1f, 1f, 1f);
        previousButton.onClick.AddListener(() => Browse(-1));
        nextButton.onClick.AddListener(() => Browse(1));

        // Between the buttons. Only a display, it can't be edited or clicked.
        RectTransform textRect = CreateRect("Rule", bar);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        float side = buttonSize + 2f * padding;
        textRect.offsetMin = new Vector2(side, padding);
        textRect.offsetMax = new Vector2(-side, -padding);
        text = CreateText(textRect, messageText, fontScale);
    }

    // anchorX 0 puts the button at the left edge, 1 at the right edge.
    private Button CreateButton(string name, RectTransform parent, Sprite sprite, string fallbackLabel, float anchorX, TextMeshProUGUI messageText)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = new Vector2(anchorX, 0.5f);
        rect.anchorMax = new Vector2(anchorX, 0.5f);
        // Pivot in the center, so mirroring the button keeps it in place.
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);
        float offset = padding + buttonSize / 2f;
        rect.anchoredPosition = new Vector2(anchorX == 0f ? offset : -offset, 0f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        // Without a sprite the button shows an arrow on a transparent background.
        if (sprite == null)
        {
            image.color = new Color(1f, 1f, 1f, 0f);
            RectTransform labelRect = CreateRect("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI label = CreateText(labelRect, messageText, 1f);
            label.text = fallbackLabel;
            button.targetGraphic = label;
        }

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
        button.colors = colors;
        return button;
    }

    private static TextMeshProUGUI CreateText(RectTransform rect, TextMeshProUGUI messageText, float scale)
    {
        TextMeshProUGUI result = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            result.font = messageText.font;
            result.fontSharedMaterial = messageText.fontSharedMaterial;
            result.fontStyle = messageText.fontStyle;
            result.color = messageText.color;
            result.fontSize = messageText.fontSize * scale;
        }
        result.alignment = TextAlignmentOptions.Center;
        result.textWrappingMode = TextWrappingModes.Normal;
        result.raycastTarget = false;
        return result;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }
}
