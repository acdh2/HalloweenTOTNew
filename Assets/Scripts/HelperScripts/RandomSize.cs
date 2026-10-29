using UnityEngine;

public class RandomSize : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float scale = Random.Range(0.5f, 1f);
        transform.localScale = Vector3.one * scale;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
