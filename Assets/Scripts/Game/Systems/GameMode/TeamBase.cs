using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ServerOnlyComponent]
[RequireComponent(typeof(BoxCollider))]
public class TeamBase : MonoBehaviour
{
    public int teamIndex;
    [System.NonSerialized] public BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var boxCollider = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        if (gameObject == UnityEditor.Selection.activeGameObject)
        {
            // If we are directly selected (and not just our parent is selected)
            // draw with negative size to get an 'inside out' cube we can see from the inside
            Gizmos.color = new Color(0.5f, 1.0f, 1.5f, 0.8f);
            Gizmos.DrawCube(boxCollider.center, -boxCollider.size);
        }
        Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 0.1f);
        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
    }
#endif
}
