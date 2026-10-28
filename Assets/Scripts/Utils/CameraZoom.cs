using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Voert alleen camera-acties uit. Beslist zelf niets: GamePlay roept deze
// routines aan en wacht erop, zodat er altijd maar één ding tegelijk gebeurt.
public class CameraZoom : MonoBehaviour
{
    [Header("Doelpunten")]
    public Transform startPoint;
    public Transform windowTarget; // Hernoemd van targetPoint
    public Transform doorTarget;   // Nieuw doel voor deur routine

    [Header("Deur & Animatie")]
    public Animator doorAnimator;

    [Header("Head Zoeken")]
    [Tooltip("Offset ten opzichte van de positie van het Head object.")]
    public Vector3 headOffset = new Vector3(0f, 1f, -2f);
    public float fadeDuration = 1.0f;

    [Header("Timing")]
    public float tweenDuration = 1.0f;
    public float dwellTime = 3.0f;

    [Header("Pan Instellingen")]
    public float panAngle = 15f;

    [Header("Vloeiende Curve")]
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float fadeAlpha = 0f;

    void Start()
    {
        ResetToStart();
    }

    private void OnGUI()
    {
        if (fadeAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
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

    public IEnumerator GoToDoorAndOpen()
    {
        yield return MoveCameraRoutine(startPoint.position, startPoint.rotation, doorTarget.position, doorTarget.rotation);

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }
        AudioManager.PlaySound("DoorOpen");

        yield return new WaitForSeconds(2f);
    }

    // De regels gebruiken Camera.main.WorldToViewportPoint, dus ze moeten getest
    // worden vanuit het deurstandpunt. Dit zet de camera daar heel even neer en
    // weer terug binnen hetzelfde frame, zodat de speler er niets van ziet.
    public bool TestFromDoorView(RuleSet ruleset)
    {
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.position = doorTarget.position;
        transform.rotation = doorTarget.rotation;
        bool safe = ruleset.TestAll();

        transform.position = oldPos;
        transform.rotation = oldRot;
        return safe;
    }

    public IEnumerator KillerSequence()
    {
        Transform randomHead = FindRandomKillerViewPoint();
        if (randomHead != null)
        {
            transform.position = randomHead.position;
            transform.rotation = randomHead.rotation;
        }

        yield return new WaitForSeconds(1f);

        float elapsedFade = 0f;
        while (elapsedFade < fadeDuration)
        {
            elapsedFade += Time.deltaTime;
            fadeAlpha = Mathf.Clamp01(elapsedFade / fadeDuration);
            yield return null;
        }
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

        fadeAlpha = 0f;
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
            return heads[randomIndex];
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
