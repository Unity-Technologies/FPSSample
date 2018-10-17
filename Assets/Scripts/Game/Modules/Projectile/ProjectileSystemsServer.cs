using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

[DisableAutoCreation]
public class HandleServerProjectileRequests : BaseComponentSystem
{
	public struct Reqests
	{
		[ReadOnly] public EntityArray entities;
		[ReadOnly] public ComponentDataArray<ProjectileRequest> requests;
	}

	[Inject] 
	public Reqests Group;

	public HandleServerProjectileRequests(GameWorld world, BundledResourceManager resourceSystem) : base(world)
	{
		m_resourceSystem = resourceSystem;
    
		m_settings = Resources.Load<ProjectileModuleSettings>("ProjectileModuleSettings");
	}

	protected override void OnDestroyManager()
	{
		base.OnDestroyManager();
		Resources.UnloadAsset(m_settings);
	}

	protected override void OnUpdate()
	{
		// Copy requests as spawning will invalidate Group 
		var requests = new ProjectileRequest[Group.requests.Length];
		for (var i = 0; i < Group.requests.Length; i++)
		{
			requests[i] = Group.requests[i];
			PostUpdateCommands.DestroyEntity(Group.entities[i]);
		}

		// Handle requests
		foreach (var request in requests)
		{
			var projectileEntity = m_settings.projectileFactory.Create(EntityManager);
			
			var projectileData = EntityManager.GetComponentData<ProjectileData>(projectileEntity);

			projectileData.Initialize(request);
			projectileData.LoadSettings(m_resourceSystem);
			
			PostUpdateCommands.SetComponent(projectileEntity, projectileData);
			PostUpdateCommands.AddComponent(projectileEntity, new ServerEntity());
		}
	}

	BundledResourceManager m_resourceSystem;
	ProjectileModuleSettings m_settings;
}
