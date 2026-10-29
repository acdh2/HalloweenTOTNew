using UnityEngine;

using System.Collections.Generic;

public class KeepRandomChildrenRange : MonoBehaviour
{
    public int minKeep = 1;
    public int maxKeep = 7;

    void Start()
    {
        int childCount = transform.childCount;
        if (childCount <= 0) return;

        int countToKeep = Mathf.Clamp(Random.Range(minKeep, maxKeep + 1), 0, childCount);
        if (childCount <= countToKeep) return;

        List<int> keepIndices = new List<int>();
        while (keepIndices.Count < countToKeep)
        {
            int randomIndex = Random.Range(0, childCount);
            if (!keepIndices.Contains(randomIndex))
            {
                keepIndices.Add(randomIndex);
            }
        }

        for (int i = childCount - 1; i >= 0; i--)
        {
            if (!keepIndices.Contains(i))
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}