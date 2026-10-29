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

    public static Transform root => GetRoot();

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
        return IsVisible(obj);
    }

    // Visible if the object is active and it or one of its children has an
    // enabled renderer. We assume everything that matters is in camera view.
    private static bool IsVisible(Transform obj)
    {
        if (obj == null || !obj.gameObject.activeInHierarchy) return false;

        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
        {
            if (rend.enabled) return true;
        }
        return false;
    }

    public static int Count(string name)
    {
        int count = 0;
        if (root == null) return 0;
        CountRecursive(root, name, ref count);
        return count;
    }

    public static bool Exists(string name)
    {
        return Count(name) > 0;
    }

    public static bool CountIsEven(string name)
    {
        return Count(name) % 2 == 0;
    }

    private static void CountRecursive(Transform parent, string name, ref int count)
    {
        if (parent.name == name && IsVisible(parent))
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