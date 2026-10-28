using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;

public class GamePlayBase : MonoBehaviour
{
    public Texture2D heartTexture;
    public CameraZoom cameraZoom;
    public GameObject message;

    private float fadeInDuration = 0.5f;
    private float displayDuration = 2.0f;
    private float fadeOutDuration = 0.5f;

    protected bool gameIsRunning = true;
    protected int lives = 3;

    private float heartSize = 192f;
    private float heartSpacing = 8f;
    private float topMargin = 8f;
    private float rightMargin = 8f;

    void Start()
    {
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
        if (heartTexture == null || lives <= 0) return;

        float scaleFactor = Screen.height / 1080f;
        float currentSize = heartSize * scaleFactor;
        float currentSpacing = heartSpacing * scaleFactor;
        float currentTop = topMargin * scaleFactor;
        float currentRight = rightMargin * scaleFactor;

        for (int i = 0; i < lives; i++)
        {
            float xPos = Screen.width - currentRight - ((i + 1) * (currentSize + currentSpacing));
            Rect rect = new Rect(xPos, currentTop, currentSize, currentSize);
            GUI.DrawTexture(rect, heartTexture);
        }
    }

    // Met stay = true blijft het bericht in beeld staan (bv. voor game over).
    protected IEnumerator ShowMessage(string messageText, bool stay = false)
    {
        CanvasGroup canvasGroup = message.GetComponent<CanvasGroup>();
        TMPro.TextMeshProUGUI text = message.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        text.text = messageText;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1.0f;

        if (stay) yield break;

        yield return new WaitForSeconds(displayDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
