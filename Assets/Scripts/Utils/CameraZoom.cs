using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float cooldownTime = 1.5f;      

    [Header("Pan Instellingen")]
    public float panAngle = 15f;

    [Header("Vloeiende Curve")]
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isBusy = false;
    private float fadeAlpha = 0f;

    public bool IsBusy
    {
        get { return isBusy; }
    }

    void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }
    }

    private void OnGUI()
    {
        if (fadeAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }

    public void TriggerZoom()
    {
        if (isBusy) return;
        StartCoroutine(ZoomRoutine());
    }

    public void TriggerDoorRoutine()
    {
        if (isBusy) return;
        StartCoroutine(DoorRoutine());
    }

    private IEnumerator ZoomRoutine()
    {
        isBusy = true;

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
        yield return new WaitForSeconds(cooldownTime);

        isBusy = false;
    }

    private IEnumerator DoorRoutine()
    {
        isBusy = true;

        // Stap 1: Camera gaat naar doorTarget
        yield return MoveCameraRoutine(startPoint.position, startPoint.rotation, doorTarget.position, doorTarget.rotation);

        // Stap 2: Activeer "OpenDoor" trigger
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenDoor");
        }

        // Stap 3: Wacht 2 seconden
        yield return new WaitForSeconds(2f);

        // Stap 4: Zoek een random "Head" object in de hiërarchie en pas positie aan
        Transform randomHead = FindRandomKillerViewPoint();
        if (randomHead != null)
        {
            transform.position = randomHead.position;
            transform.rotation = randomHead.rotation;
        }

        yield return new WaitForSeconds(1f);

        // Stap 5: Fade naar zwart
        float elapsedFade = 0f;
        while (elapsedFade < fadeDuration)
        {
            elapsedFade += Time.deltaTime;
            fadeAlpha = Mathf.Clamp01(elapsedFade / fadeDuration);
            yield return null;
        }

        // Stap 6: Spring terug, reset animatie en beëindig fade abrupt
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Reset");
        }

        fadeAlpha = 0f;
        isBusy = false;
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
            int randomIndex = Random.Range(0, heads.Count);
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