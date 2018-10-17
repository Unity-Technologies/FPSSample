using UnityEngine;
using System.Collections;
using Unity.Entities;
using System.Collections.Generic;

[ClientOnlyComponent]
public class Spin : SceneEntity
{
    public float speed = 10f;
    public Vector3 rotationAxis;

}

[DisableAutoCreation]
public class SpinSystem : BaseComponentSystem
{
    struct Spinners
    {
        public ComponentArray<Spin> spinners;
    }

    [Inject] 
    Spinners Group; 
    
    public SpinSystem(GameWorld gameWorld) : base(gameWorld) {}

    protected override void OnUpdate()
    {
        float dt = m_world.frameDuration;
        for(int i = 0, c = Group.spinners.Length; i<c; i++)
        {
            var g = Group.spinners[i];
            g.gameObject.transform.Rotate(g.rotationAxis, g.speed * dt);
        }
    }
}
