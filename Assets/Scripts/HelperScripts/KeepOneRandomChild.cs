using UnityEngine;

public class KeepOneRandomChild : MonoBehaviour
{
    void Start()
    {
        int childCount = transform.childCount;
        if (childCount <= 1) return;

        int randomIndex = Random.Range(0, childCount);

        for (int i = childCount - 1; i >= 0; i--)
        {
            if (i != randomIndex)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}