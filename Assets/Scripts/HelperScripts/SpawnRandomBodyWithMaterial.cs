using UnityEngine;
using System.Collections.Generic;

public class SpawnRandomBodyWithMaterial : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialSet
    {
        public Material skin;
        public Material jumpscareSkin;
    }

    [System.Serializable]
    public struct BodyOption
    {
        public GameObject body;
        public List<MaterialSet> materials;
    }

    public List<BodyOption> bodies;

    System.Action OnJumpScare = null;

    void Start()
    {
        if (bodies == null || bodies.Count == 0) return;

        int bodyIndex = Random.Range(0, bodies.Count);
        BodyOption bodyOption = bodies[bodyIndex];
        GameObject bodyPrefab = bodyOption.body;
        if (bodyPrefab == null) return;
        if (bodyOption.materials == null || bodyOption.materials.Count == 0) return;

        int materialIndex = Random.Range(0, bodyOption.materials.Count);
        MaterialSet materialSet = bodyOption.materials[materialIndex];
        Material material = materialSet.skin;
        Material jumpScareMaterial = materialSet.jumpscareSkin;

        gameObject.name = bodyPrefab.name;

        GameObject newBody = Instantiate(bodyPrefab, transform);
        newBody.name = bodyPrefab.name;

        Renderer[] renderers = newBody.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }

        OnJumpScare = () =>
        {
            Renderer[] renderers = newBody.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = jumpScareMaterial;
            }
        };
    }

    public System.Collections.IEnumerator HandleJumpScare()
    {
        yield return new WaitForSeconds(0.4f);
        OnJumpScare?.Invoke();
    }

}
