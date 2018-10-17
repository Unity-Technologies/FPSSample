using UnityEngine;

[ServerOnlyComponent]
public class SpawnPoint : MonoBehaviour
{
    public int teamIndex;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position + transform.up, new Vector3(0.5f, 2.0f, 0.5f));
        Gizmos.DrawRay(transform.position + transform.up * 1.5f, transform.forward);
    }
#endif

}
