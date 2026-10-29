using System.Collections;
using UnityEngine;

// Lets the sky flash now and then, like a thunderstorm far away.
// The sky is the background color of the camera, so Clear Flags must be Solid Color.
[RequireComponent(typeof(Camera))]
public class Lightning : MonoBehaviour
{
    [Header("Timing")]
    public float minInterval = 8f;
    public float maxInterval = 20f;

    [Header("Flash")]
    public Color flashColor = new Color(0.75f, 0.8f, 0.9f);
    [Range(0f, 1f)] public float maxStrength = 0.35f;
    public float fadeDuration = 0.6f;

    private Camera cam;
    private Color baseColor;
    private float strength = 0f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        baseColor = cam.backgroundColor;
    }

    void OnEnable()
    {
        StartCoroutine(Storm());
    }

    void OnDisable()
    {
        SetStrength(0f);
    }

    private IEnumerator Storm()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            yield return Strike();
        }
    }

    // One to three short flickers, after the last one the sky slowly goes dark again.
    private IEnumerator Strike()
    {
        int flickers = Random.Range(1, 4);
        for (int i = 0; i < flickers; i++)
        {
            float flash = maxStrength * Random.Range(0.5f, 1f);
            SetStrength(flash);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
            if (i < flickers - 1)
            {
                SetStrength(flash * 0.2f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
        }

        float start = strength;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            SetStrength(start * fade * fade);
            yield return null;
        }
        SetStrength(0f);
    }

    private void SetStrength(float value)
    {
        strength = value;
        cam.backgroundColor = Color.Lerp(baseColor, flashColor, strength);
    }
}
