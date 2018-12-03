using UnityEngine;
using System.Collections;
using Unity.Entities;
using System.Collections.Generic;

[ClientOnlyComponent]
public class Spin : MonoBehaviour
{
    public float speed = 10f;
    public Vector3 rotationAxis;
}

[DisableAutoCreation]
public class SpinSystem : BaseComponentSystem
{
    ComponentGroup Group; 
    
    public SpinSystem(GameWorld gameWorld) : base(gameWorld) {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Spin));
    }

    protected override void OnUpdate()
    {
        var spinnerArray = Group.GetComponentArray<Spin>();
        float dt = m_world.frameDuration;
        for(int i = 0, c = spinnerArray.Length; i<c; i++)
        {
            var g = spinnerArray[i];
            g.gameObject.transform.Rotate(g.rotationAxis, g.speed * dt);
        }
    }
}
