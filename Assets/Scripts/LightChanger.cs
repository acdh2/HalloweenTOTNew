using UnityEngine;

public class LightChanger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        int randomInt = Random.Range(1, 3);
        if (randomInt == 1)
        {
            gameObject.GetComponent<Light>().color = Color.red;
            Debug.Log("Light is red");
        }
        else if (randomInt == 2)
        {
            gameObject.GetComponent<Light>().color = Color.white;
            Debug.Log("Light is white");
        }
    }
}
