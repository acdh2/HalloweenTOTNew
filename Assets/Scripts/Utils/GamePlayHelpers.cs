using UnityEngine;

public class GamePlayHelpers : MonoBehaviour
{
    private static Transform _root;

    private static Transform GetRoot() {
        if (_root == null) {
            GameObject rootObject = GameObject.Find("Scene");
            if (rootObject != null) _root = rootObject.transform;
        }
        if (_root == null) Debug.LogError("Root object cannot be found!");
        return _root;
    }

    private static Transform root => GetRoot();

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public static Transform Find(string name) 
    {
        if (root == null) return null;
        return FindRecursive(root, name);
    }

    public static bool IsVisible(string name) 
    {
        Transform obj = Find(name);
        if (obj == null || Camera.main == null) return false;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && obj.gameObject.activeSelf && rend.enabled)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(obj.position);
            return screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;
        }
        return false;
    }

    public static float GetX(string name) 
    {
        Transform obj = Find(name);
        if (obj == null || Camera.main == null) return 0f;

        Vector3 screenPoint = Camera.main.WorldToViewportPoint(obj.position);
        return (screenPoint.x * 2f) - 1f;
    }

    public static float GetY(string name) 
    {
        Transform obj = Find(name);
        if (obj == null || Camera.main == null) return 0f;

        Vector3 screenPoint = Camera.main.WorldToViewportPoint(obj.position);
        return (screenPoint.y * 2f) - 1f;
    }

    public static int Count(string name)
    {
        int count = 0;
        if (root == null) return 0;
        CountRecursive(root, name, ref count);
        return count;
    }

    private static void CountRecursive(Transform parent, string name, ref int count)
    {
        if (parent.name == name)
        {
            count++;
        }
        foreach (Transform child in parent)
        {
            CountRecursive(child, name, ref count);
        }
    }

    public static void MakeVisible(Transform obj, bool visible)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(visible);
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null) rend.enabled = visible;
    }

    public static void MakeOneVisible(params Transform[] objects)
    {
        if (objects == null || objects.Length == 0) return;
        int randomIndex = Random.Range(0, objects.Length);
        for (int i = 0; i < objects.Length; i++)
        {
            MakeVisible(objects[i], i == randomIndex);
        }
    }

    public static void MakeAllInvisible(params Transform[] objects)
    {
        if (objects == null) return;
        foreach (Transform obj in objects)
        {
            MakeVisible(obj, false);
        }
    }    

    public static bool RandomBool() 
    {
        return UnityEngine.Random.value > 0.5f;
    }

}