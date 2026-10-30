using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Only performs camera actions. Decides nothing itself: GamePlay calls these
// routines and waits for them, so only one thing ever happens at a time.
public class CameraZoom : MonoBehaviour
{
    [Header("Targets")]
    public Transform startPoint;
    public Transform windowTarget; // Renamed from targetPoint
    public Transform doorTarget;   // New target for the door routine

    [Header("Door & Animation")]
    public Animator doorAnimator;

    [Header("Head Search")]
    [Tooltip("Offset relative to the position of the Head object.")]
    public Vector3 headOffset = new Vector3(0f, 1f, -2f);

    [Header("Hit Effect")]
    [Tooltip("How long the flash, vignette and shake take to die out.")]
    public float hitDuration = 0.9f;
    [Range(0f, 1f)] public float hitFlashAlpha = 0.6f;
    [Range(0f, 1f)] public float hitVignetteAlpha = 0.9f;
    public float shakeStrength = 0.08f;
    public float shakeAngle = 4f;
    public float shakeFrequency = 25f;

    [Header("Timing")]
    public float tweenDuration = 1.0f;
    public float dwellTime = 3.0f;
    [Tooltip("Seconds after the killer view appears before the hit lands.")]
    public float killerHitDelay = 0.5f;

    [Header("Pan Settings")]
    public float panAngle = 15f;

    [Header("Smooth Curve")]
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float flashAlpha = 0f;
    private float vignetteAlpha = 0f;
    private Texture2D vignetteTexture;

    void Start()
    {
        vignetteTexture = CreateVignetteTexture(128);
        ResetToStart();
    }

    void OnDestroy()
    {
        Destroy(vignetteTexture);
    }

    private void OnGUI()
    {
        // Lower depth is drawn on top: the hit effect covers the hearts of GamePlayBase.
        GUI.depth = -1;
        Rect screen = new Rect(0, 0, Screen.width, Screen.height);
        Color oldColor = GUI.color;
        if (flashAlpha > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, flashAlpha);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
        }
        if (vignetteAlpha > 0f)
        {
            GUI.color = new Color(0.55f, 0f, 0f, vignetteAlpha);
            GUI.DrawTexture(screen, vignetteTexture);
        }
        GUI.color = oldColor;
    }

    public IEnumerator PeekThroughWindow()
    {
        yield return MoveCameraRoutine(startPoint.position, startPoint.rotation, windowTarget.position, windowTarget.rotation);

        float dwellElapsed = 0f;
        Quaternion baseTargetRot = windowTarget.rotation;
        Vector3 targetPos = windowTarget.position;

        while (dwellElapsed < dwellTime)
        {
            dwellElapsed += Time.deltaTime;
            float t = dwellElapsed / dwellTime;
            float angleOffset = Mathf.Sin(t * Mathf.PI * 1f) * panAngle;

            transform.position = targetPos;
            transform.rotation = baseTargetRot * Quaternion.Euler(0f, angleOffset, 0f);

            yield return null;
        }

        yield return MoveCameraRoutine(targetPos, baseTargetRot, startPoint.position, startPoint.rotation);
    }

    // waitAfterOpen: how long to look at the opened door before continuing.
    public IEnumerator GoToDoorAndOpen(float waitAfterOpen)
    {
        yield return MoveCameraRoutine(startPoint.position, startPoint.rotation, doorTarget.position, doorTarget.rotation);

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }
        AudioManager.PlaySound("DoorOpen");

        yield return new WaitForSeconds(waitAfterOpen);
    }

    public IEnumerator KillerSequence()
    {
        Transform randomHead = FindRandomKillerViewPoint();
        if (randomHead != null)
        {
            transform.position = randomHead.position;
            transform.rotation = randomHead.rotation;
        }

        yield return new WaitForSeconds(killerHitDelay);

        yield return HitEffect();
    }

    // The hit lands at full strength in a single frame and then dies out:
    // the flash fast, the red edges and the camera shake a bit slower.
    private IEnumerator HitEffect()
    {
        Vector3 basePos = transform.position;
        Quaternion baseRot = transform.rotation;
        float seed = Random.Range(0f, 100f);

        float elapsed = 0f;
        while (elapsed < hitDuration)
        {
            float t = elapsed / hitDuration;
            float fade = 1f - t;
            flashAlpha = hitFlashAlpha * fade * fade * fade * fade;
            vignetteAlpha = hitVignetteAlpha * Mathf.Sqrt(fade);

            // Perlin noise instead of Random, so the shake jitters without teleporting.
            float shake = fade * fade;
            float time = elapsed * shakeFrequency;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(seed, time) - 0.5f, Mathf.PerlinNoise(seed + 10f, time) - 0.5f, 0f) * 2f;
            float roll = (Mathf.PerlinNoise(seed + 20f, time) - 0.5f) * 2f;
            transform.position = basePos + baseRot * offset * shakeStrength * shake;
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, roll * shakeAngle * shake);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = basePos;
        transform.rotation = baseRot;
        flashAlpha = 0f;
        vignetteAlpha = 0f;
    }

    // White with a transparent center that fades to opaque at the edges, tinted by GUI.color.
    private static Texture2D CreateVignetteTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 fromCenter = new Vector2(x, y) / (size - 1) * 2f - Vector2.one;
                float edge = Mathf.InverseLerp(0.45f, 1.35f, fromCenter.magnitude);
                pixels[y * size + x] = new Color(1f, 1f, 1f, edge * edge);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    public void CloseDoor()
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Reset");
        }
    }

    public void ResetToStart()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }

        flashAlpha = 0f;
        vignetteAlpha = 0f;
    }

    private Transform FindRandomKillerViewPoint()
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        List<Transform> heads = new List<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t.name == "KillerViewPoint")
            {
                heads.Add(t);
            }
        }

        if (heads.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, heads.Count);
            Transform head = heads[randomIndex];
            JumpScareFaceTrigger jumpScareFaceTrigger = head.gameObject.GetComponent<JumpScareFaceTrigger>();
            jumpScareFaceTrigger?.SwitchToJumpscareFace();
            return head;
        }

        return null;
    }

    private IEnumerator MoveCameraRoutine(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
    {
        float elapsed = 0f;

        while (elapsed < tweenDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / tweenDuration);
            float smoothT = zoomCurve.Evaluate(rawT);

            transform.position = Vector3.Lerp(fromPos, toPos, smoothT);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, smoothT);

            yield return null;
        }

        transform.position = toPos;
        transform.rotation = toRot;
    }
}
