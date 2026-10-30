using UnityEngine;

public class JumpScareFaceTrigger : MonoBehaviour
{
    public SpawnRandomBodyWithMaterial spawnRandomBodyWithMaterial;
    public void SwitchToJumpscareFace()
    {
        StartCoroutine(spawnRandomBodyWithMaterial?.HandleJumpScare());
    }
}
