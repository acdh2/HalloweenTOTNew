using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ClickEventTrigger : MonoBehaviour
{
    public UnityEvent onMouseDownEvent;
    public float minInteractionDistance = 0f;
    public float maxInteractionDistance = 5f;
    private Light spookyLight;
    private bool isHovered = false;

    void Start()
    {
        spookyLight = GetComponentInChildren<Light>();
        if (spookyLight != null)
        {
            spookyLight.intensity = 0f;
        }
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        
        // The rule bar at the bottom of the screen lies on top of the scene.
        isHovered = !RuleBrowser.PointerOverBar
            && Physics.Raycast(ray, out hit, maxInteractionDistance)
            && hit.transform == transform
            && hit.distance >= minInteractionDistance;

        if (spookyLight != null)
        {
            float targetIntensity = isHovered ? 0.3f : 0f;
            float speed = isHovered ? 2f : 15f;
            
            spookyLight.intensity = Mathf.MoveTowards(spookyLight.intensity, targetIntensity, Time.deltaTime * speed);
        }

        if (isHovered && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            onMouseDownEvent?.Invoke();
        }
    }
}