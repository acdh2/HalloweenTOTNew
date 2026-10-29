using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;

public class GamePlayBase : MonoBehaviour
{
    public Texture2D heartTexture;
    public CameraZoom cameraZoom;
    public GameObject message;
    // Optional images inside the message canvas, shown above and below the text.
    public RawImage messageImage;
    public RawImage messageImageBelow;

    private Texture messageImageDefault;
    private Texture messageImageBelowDefault;
    private float messageImageSpacing = 20f;
    private float fadeInDuration = 0.5f;
    private float displayDuration = 2.0f;
    private float fadeOutDuration = 0.5f;
    private float segmentPause = 1.0f;
    private float flickerMinDuration = 1.0f;
    private float flickerMaxDuration = 2.0f;

    protected bool gameIsRunning = true;
    protected int lives = 3;

    private float heartSize = 192f;
    private float heartSpacing = 8f;
    private float topMargin = 8f;
    private float rightMargin = 8f;
    private float heartFlickerDuration = 1.5f;
    private float lifeLostMessageDelay = 0.75f;

    private CanvasGroup messageCanvasGroup;

    // The heart that was just lost, drawn until it has finished flickering.
    private bool losingHeart = false;
    private bool losingHeartVisible = true;

    void Start()
    {
        messageCanvasGroup = message.GetComponent<CanvasGroup>();
        if (messageImage != null) messageImageDefault = messageImage.texture;
        if (messageImageBelow != null) messageImageBelowDefault = messageImageBelow.texture;
        StartCoroutine(MainGameLoop());
    }

    void OnDestroy()
    {
        gameIsRunning = false;
    }

    virtual protected IEnumerator MainGameLoop()
    {
        yield return null;
    }

    void OnGUI()
    {
        int heartsShown = lives + (losingHeart ? 1 : 0);
        if (heartTexture == null || heartsShown <= 0) return;

        // The hearts are drawn on top of the UI, so fade them out along with the message.
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 1f - messageCanvasGroup.alpha);

        float scaleFactor = Screen.height / 1080f;
        float currentSize = heartSize * scaleFactor;
        float currentSpacing = heartSpacing * scaleFactor;
        float currentTop = topMargin * scaleFactor;
        float currentRight = rightMargin * scaleFactor;

        for (int i = 0; i < heartsShown; i++)
        {
            if (i == lives && !losingHeartVisible) continue;
            float xPos = Screen.width - currentRight - ((i + 1) * (currentSize + currentSpacing));
            Rect rect = new Rect(xPos, currentTop, currentSize, currentSize);
            GUI.DrawTexture(rect, heartTexture);
        }

        GUI.color = oldColor;
    }

    // Removes a life: its heart flickers like a broken lamp, goes out, and
    // after a short pause the caller can show its message.
    protected IEnumerator LoseLife()
    {
        lives--;
        yield return FlickerLostHeart();
        yield return new WaitForSeconds(lifeLostMessageDelay);
    }

    private IEnumerator FlickerLostHeart()
    {
        losingHeart = true;
        float elapsed = 0f;
        while (elapsed < heartFlickerDuration)
        {
            losingHeartVisible = !losingHeartVisible;
            float wait = losingHeartVisible ? Random.Range(0.05f, 0.25f) : Random.Range(0.03f, 0.12f);
            yield return new WaitForSeconds(wait);
            elapsed += wait;
        }
        losingHeart = false;
        losingHeartVisible = true;
    }

    // With stay = true the message stays on screen (e.g. for game over).
    // A duration > 0 overrides displayDuration.
    // Each | in the text is a pause: the next part flickers in segmentPause seconds later.
    // The hidden parts are already in the text but transparent, so the layout doesn't shift.
    // The images are shown centered above and below the text.
    protected IEnumerator ShowMessage(string messageText, bool stay = false, float duration = -1f, Texture image = null, Texture imageBelow = null)
    {
        CanvasGroup canvasGroup = message.GetComponent<CanvasGroup>();
        TMPro.TextMeshProUGUI text = message.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        string[] segments = messageText.Split('|');
        text.text = RevealSegments(segments, 1);
        // The hidden segments are in the text too, so the bounds don't change while they appear.
        text.ForceMeshUpdate();
        ShowMessageImage(text, messageImage, image != null ? image : messageImageDefault, true);
        ShowMessageImage(text, messageImageBelow, imageBelow != null ? imageBelow : messageImageBelowDefault, false);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1.0f;

        for (int i = 1; i < segments.Length; i++)
        {
            yield return new WaitForSeconds(segmentPause);
            yield return FlickerIn(text, segments, i);
        }

        if (stay) yield break;

        yield return new WaitForSeconds(duration > 0f ? duration : displayDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    // Puts the image just above the top line or just below the bottom line of the text.
    // Its height comes from the inspector, the width follows the aspect ratio of the texture.
    // Without an image the RawImage is hidden.
    private void ShowMessageImage(TMPro.TextMeshProUGUI text, RawImage target, Texture image, bool above)
    {
        if (target == null) return;
        target.gameObject.SetActive(image != null);
        if (image == null) return;

        target.texture = image;
        RectTransform imageRect = target.rectTransform;
        imageRect.pivot = new Vector2(0.5f, above ? 0f : 1f);
        imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.y * image.width / image.height, imageRect.sizeDelta.y);

        float edgeY = above ? text.textBounds.max.y : text.textBounds.min.y;
        Vector3 textEdge = text.transform.TransformPoint(new Vector3(0f, edgeY, 0f));
        float edge = imageRect.parent.InverseTransformPoint(textEdge).y;

        // Horizontally centered on the screen, whatever the anchors in the inspector are.
        RectTransform canvasRect = (RectTransform)target.canvas.rootCanvas.transform;
        Vector3 screenCenter = canvasRect.TransformPoint(canvasRect.rect.center);
        float center = imageRect.parent.InverseTransformPoint(screenCenter).x;

        float offset = above ? messageImageSpacing : -messageImageSpacing;
        imageRect.localPosition = new Vector3(center, edge + offset, 0f);
    }

    // Fades segment index in like a lantern: every letter flickers on its own
    // and settles down over a random 1-2 seconds, after which it stays on.
    private IEnumerator FlickerIn(TMPro.TextMeshProUGUI text, string[] segments, int index)
    {
        int letterCount = segments[index].Length;
        float[] durations = new float[letterCount];
        float[] nextChanges = new float[letterCount];
        float[] alphas = new float[letterCount];
        float totalDuration = 0f;
        for (int i = 0; i < letterCount; i++)
        {
            durations[i] = Random.Range(flickerMinDuration, flickerMaxDuration);
            totalDuration = Mathf.Max(totalDuration, durations[i]);
        }

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < letterCount; i++)
            {
                if (elapsed >= durations[i])
                {
                    alphas[i] = 1f;
                }
                else if (elapsed >= nextChanges[i])
                {
                    // Brightness climbs over time, the random dips get smaller near the end.
                    float t = elapsed / durations[i];
                    alphas[i] = Mathf.Clamp01(t + Random.Range(-0.6f, 0.3f) * (1f - t));
                    nextChanges[i] = elapsed + Random.Range(0.03f, 0.12f);
                }
            }
            text.text = RevealSegments(segments, index, alphas);
            yield return null;
        }
        text.text = RevealSegments(segments, index + 1);
    }

    // Shows the first count segments, the next one with an alpha per letter
    // (transparent without letterAlphas) and the rest transparent, so all
    // text stays in place.
    private static string RevealSegments(string[] segments, int count, float[] letterAlphas = null)
    {
        StringBuilder result = new StringBuilder(string.Join("", segments, 0, count));
        if (count >= segments.Length) return result.ToString();

        int hiddenFrom = count;
        if (letterAlphas != null)
        {
            string next = segments[count];
            for (int i = 0; i < next.Length; i++)
            {
                int alphaByte = Mathf.RoundToInt(Mathf.Clamp01(letterAlphas[i]) * 255f);
                result.Append($"<alpha=#{alphaByte:X2}>").Append(next[i]);
            }
            hiddenFrom = count + 1;
        }

        if (hiddenFrom < segments.Length)
            result.Append("<alpha=#00>").Append(string.Join("", segments, hiddenFrom, segments.Length - hiddenFrom));
        return result.ToString();
    }
}
