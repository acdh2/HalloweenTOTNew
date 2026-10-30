using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlideShow : MonoBehaviour
{
    public Texture[] textures;
    public int secondenPerFrame = 2;

    private RawImage rawImage;
    private int currentIndex = 0;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        if (textures != null && textures.Length > 0)
        {
            StartCoroutine(PlaySlideShow());
        }
    }

    IEnumerator PlaySlideShow()
    {
        while (true)
        {
            rawImage.texture = textures[currentIndex];
            currentIndex = (currentIndex + 1) % textures.Length;
            yield return new WaitForSeconds(secondenPerFrame);
        }
    }
}