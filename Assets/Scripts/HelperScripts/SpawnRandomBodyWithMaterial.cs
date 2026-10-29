using UnityEngine;
using System.Collections.Generic;

public class SpawnRandomBodyWithMaterial : MonoBehaviour
{
    public List<GameObject> bodies;
    public List<Material> materials;

    void Start()
    {
        if (materials == null || materials.Count == 0) return;
        if (bodies == null || bodies.Count == 0) return;

        int bodyIndex = Random.Range(0, bodies.Count);
        GameObject bodyPrefab = bodies[bodyIndex];

        int materialIndex = Random.Range(0, materials.Count);
        Material material = materials[materialIndex];

        gameObject.name = bodyPrefab.name;

        GameObject newBody = Instantiate(bodyPrefab, transform);
        newBody.name = bodyPrefab.name;

        Renderer[] renderers = newBody.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }
    }
}