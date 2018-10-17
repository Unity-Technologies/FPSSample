using UnityEngine;
using Unity.Entities;
using UnityEditor;
using UnityEngine.Profiling;

public class DestructibleProp : MonoBehaviour
{
	public float health = 10;

	public Vector3 splashDamageOffset;
	public SplashDamageSettings splashDamage;
	public GameObject[] collision;

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		if (Selection.activeGameObject != gameObject)
			return;
		
		if (splashDamage.radius > 0)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position + splashDamageOffset,splashDamage.radius);
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position + splashDamageOffset,splashDamage.falloffStartRadius);
		}
	}
#endif
}

[DisableAutoCreation]
[RequireComponent(typeof(HitCollisionOwner))]
public class UpdateDestructableProps : BaseComponentSystem<HitCollisionOwner,DestructibleProp,DestructablePropReplicatedState> 
{
	public UpdateDestructableProps(GameWorld world) : base(world) {}
	
	protected override void Update(Entity entity, HitCollisionOwner hitCollisionOwner, DestructibleProp prop, DestructablePropReplicatedState replicatedState)
	{
		if (prop.health <= 0)
			return;
	
		if (hitCollisionOwner.damageEvents.Count == 0)
			return;

		var instigator = Entity.Null;
		foreach (var damageEvent in hitCollisionOwner.damageEvents)
		{
			prop.health -= damageEvent.damage;

			if (damageEvent.instigator != Entity.Null)
				instigator = damageEvent.instigator;

			if (prop.health < 0)
				break;
		}
		hitCollisionOwner.damageEvents.Clear();
	
		if (prop.health <= 0)
		{
			hitCollisionOwner.collisionEnabled = false;

			foreach (var gameObject in prop.collision)
			{
				gameObject.SetActive(false);
			}
			
			replicatedState.destroyedTick = m_world.worldTime.tick;

			// Create splash damage
			if (prop.splashDamage.radius > 0)
			{
				var collisionMask = ~0;
				if (instigator != Entity.Null && EntityManager.HasComponent<CharacterPredictedState>(instigator))
				{
					var character = EntityManager.GetComponentObject<CharacterPredictedState>(instigator);
					collisionMask = ~(1 << character.teamId);
				}
				
				var splashCenter = prop.transform.position + prop.splashDamageOffset;
				SplashDamageRequest.Create(PostUpdateCommands, m_world.worldTime.tick, instigator, splashCenter, collisionMask, prop.splashDamage);
			}
		}
	}
} 