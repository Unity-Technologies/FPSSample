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
public class UpdateDestructableProps : BaseComponentSystem
{
	ComponentGroup Group;
	
	public UpdateDestructableProps(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(HitCollisionOwnerData), typeof(DestructibleProp),
			typeof(DestructablePropReplicatedData));
	}

	protected override void OnUpdate()
	{
		var entityArray = Group.GetEntityArray();
		var hitCollArray = Group.GetComponentDataArray<HitCollisionOwnerData>();
		var propArray = Group.GetComponentArray<DestructibleProp>();
		var replicatedDataArray = Group.GetComponentDataArray<DestructablePropReplicatedData>();

		for (int i = 0; i < entityArray.Length; i++)
		{
			var prop = propArray[i];
			
			if (prop.health <= 0)
				continue;

			var entity = entityArray[i];
			var damageBuffer = EntityManager.GetBuffer<DamageEvent>(entity);
			
			if (damageBuffer.Length == 0)
				continue;

			var instigator = Entity.Null;
			for(int j=0;j<damageBuffer.Length;j++)
			{
				var damageEvent = damageBuffer[j];
				prop.health -= damageEvent.damage;

				if (damageEvent.instigator != Entity.Null)
					instigator = damageEvent.instigator;

				if (prop.health < 0)
					break;
			}
			damageBuffer.Clear();

			if (prop.health <= 0)
			{
				var replicatedState = replicatedDataArray[i];

				var hitCollOwner = hitCollArray[i];
				hitCollOwner.collisionEnabled = 0;
				EntityManager.SetComponentData(entity,hitCollOwner);

				foreach (var gameObject in prop.collision)
				{
					gameObject.SetActive(false);
				}

				replicatedState.destroyedTick = m_world.worldTime.tick;

				// Create splash damage
				if (prop.splashDamage.radius > 0)
				{
					var collisionMask = ~0;
					if (instigator != Entity.Null && EntityManager.HasComponent<Character>(instigator))
					{
						var character = EntityManager.GetComponentObject<Character>(instigator);
						collisionMask = ~(1 << character.teamId);
					}

					var splashCenter = prop.transform.position + prop.splashDamageOffset;
					SplashDamageRequest.Create(PostUpdateCommands, m_world.worldTime.tick, instigator, splashCenter,
						collisionMask, prop.splashDamage);
				}
				
				EntityManager.SetComponentData(entity,replicatedState);
			}
		}
		
	}
} 