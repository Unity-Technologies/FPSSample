using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

[DisableAutoCreation]
public abstract class BaseComponentSystem : ComponentSystem
{
    protected BaseComponentSystem(GameWorld world)
    {
        m_world = world;
    }

    readonly protected GameWorld m_world;
}

[DisableAutoCreation]
 public abstract class BaseComponentSystem<T1> : BaseComponentSystem
 	where T1 : MonoBehaviour
 {
 	ComponentGroup Group;
 	protected ComponentType[] ExtraComponentRequirements;
	string name;

 	public BaseComponentSystem(GameWorld world) : base(world) {}

    protected override void OnCreateManager()
 	{
 		base.OnCreateManager();
		name = GetType().Name;
 		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
 		list.AddRange(new ComponentType[] { typeof(T1) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
 		Group = GetComponentGroup(list.ToArray());
 	}
 
 	protected override void OnUpdate()
 	{
		Profiler.BeginSample(name);

 		var entityArray = Group.GetEntityArray();
 		var dataArray = Group.GetComponentArray<T1>();
 
 		for (var i = 0; i < entityArray.Length; i++)
 		{
 			Update(entityArray[i], dataArray[i]);
 		}
		 
		Profiler.EndSample();
 	}
 	
 	protected abstract void Update(Entity entity,T1 data);
 }


[DisableAutoCreation]
public abstract class BaseComponentSystem<T1,T2> : BaseComponentSystem
	where T1 : MonoBehaviour
	where T2 : MonoBehaviour
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name; 
	
	public BaseComponentSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] {typeof(T1), typeof(T2)});
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentArray<T1>();
		var dataArray2 = Group.GetComponentArray<T2>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2);
}


[DisableAutoCreation]
public abstract class BaseComponentSystem<T1,T2,T3> : BaseComponentSystem
	where T1 : MonoBehaviour
	where T2 : MonoBehaviour
	where T3 : MonoBehaviour
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentArray<T1>();
		var dataArray2 = Group.GetComponentArray<T2>();
		var dataArray3 = Group.GetComponentArray<T3>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3);
}

[DisableAutoCreation]
public abstract class BaseComponentDataSystem<T1> : BaseComponentSystem
	where T1 : struct,IComponentData
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray = Group.GetComponentDataArray<T1>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data);
}

[DisableAutoCreation]
public abstract class BaseComponentDataSystem<T1,T2> : BaseComponentSystem
	where T1 : struct,IComponentData
	where T2 : struct,IComponentData
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	private string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		name = GetType().Name;
		base.OnCreateManager();
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentDataArray<T1>();
		var dataArray2 = Group.GetComponentDataArray<T2>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2);
}

[DisableAutoCreation]
public abstract class BaseComponentDataSystem<T1,T2,T3> : BaseComponentSystem
	where T1 : struct,IComponentData
	where T2 : struct,IComponentData
	where T3 : struct,IComponentData
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentDataArray<T1>();
		var dataArray2 = Group.GetComponentDataArray<T2>();
		var dataArray3 = Group.GetComponentDataArray<T3>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3);
}


[DisableAutoCreation]
public abstract class BaseComponentDataSystem<T1,T2,T3,T4> : BaseComponentSystem
	where T1 : struct,IComponentData
	where T2 : struct,IComponentData
	where T3 : struct,IComponentData
	where T4 : struct,IComponentData
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentDataArray<T1>();
		var dataArray2 = Group.GetComponentDataArray<T2>();
		var dataArray3 = Group.GetComponentDataArray<T3>();
		var dataArray4 = Group.GetComponentDataArray<T4>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i], dataArray4[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3,T4 data4);
}

[DisableAutoCreation]
public abstract class BaseComponentDataSystem<T1,T2,T3,T4, T5> : BaseComponentSystem
	where T1 : struct,IComponentData
	where T2 : struct,IComponentData
	where T3 : struct,IComponentData
	where T4 : struct,IComponentData
	where T5 : struct,IComponentData
{
	ComponentGroup Group;
	protected ComponentType[] ExtraComponentRequirements;
	string name;
	
	public BaseComponentDataSystem(GameWorld world) : base(world) {}
	
	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		var list = new List<ComponentType>(6);
		if(ExtraComponentRequirements != null)		
			list.AddRange(ExtraComponentRequirements);
		list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) } );
		list.Add(ComponentType.Subtractive<DespawningEntity>());
		Group = GetComponentGroup(list.ToArray());
	}

	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var entityArray = Group.GetEntityArray();
		var dataArray1 = Group.GetComponentDataArray<T1>();
		var dataArray2 = Group.GetComponentDataArray<T2>();
		var dataArray3 = Group.GetComponentDataArray<T3>();
		var dataArray4 = Group.GetComponentDataArray<T4>();
		var dataArray5 = Group.GetComponentDataArray<T5>();

		for (var i = 0; i < entityArray.Length; i++)
		{
			Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i], dataArray4[i], dataArray5[i]);
		}
		
		Profiler.EndSample();
	}
	
	protected abstract void Update(Entity entity,T1 data1,T2 data2,T3 data3,T4 data4, T5 data5);
}

[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class InitializeComponentSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	struct SystemState : IComponentData {}
	ComponentGroup IncomingGroup;
	string name;
	
	public InitializeComponentSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		IncomingGroup = GetComponentGroup(typeof(T),ComponentType.Subtractive<SystemState>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.GetEntityArray();
		if (incomingEntityArray.Length > 0)
		{
			var incomingComponentArray = IncomingGroup.GetComponentArray<T>();
			for (var i = 0; i < incomingComponentArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				PostUpdateCommands.AddComponent(entity,new SystemState());

				Initialize(entity, incomingComponentArray[i]);
			}
		}
		
		Profiler.EndSample();
	}

	protected abstract void Initialize(Entity entity, T component);
}


[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class InitializeComponentDataSystem<T,K> : BaseComponentSystem
	where T : struct, IComponentData
	where K : struct, IComponentData
{
	
	ComponentGroup IncomingGroup;
	string name;
	
	public InitializeComponentDataSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		IncomingGroup = GetComponentGroup(typeof(T),ComponentType.Subtractive<K>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.GetEntityArray();
		if (incomingEntityArray.Length > 0)
		{
			var incomingComponentDataArray = IncomingGroup.GetComponentDataArray<T>();
			for (var i = 0; i < incomingComponentDataArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				PostUpdateCommands.AddComponent(entity,new K());

				Initialize(entity, incomingComponentDataArray[i]);
			}
		}
		
		Profiler.EndSample();
	}

	protected abstract void Initialize(Entity entity, T component);
}



[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class DeinitializeComponentSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	ComponentGroup OutgoingGroup;
	string name;

	public DeinitializeComponentSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		OutgoingGroup = GetComponentGroup(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var outgoingComponentArray = OutgoingGroup.GetComponentArray<T>();
		var outgoingEntityArray = OutgoingGroup.GetEntityArray();
		for (var i = 0; i < outgoingComponentArray.Length; i++)
		{
			Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
		}
		
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(Entity entity, T component);
}


[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class DeinitializeComponentDataSystem<T> : BaseComponentSystem
	where T : struct, IComponentData
{
	ComponentGroup OutgoingGroup;
	string name;

	public DeinitializeComponentDataSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		OutgoingGroup = GetComponentGroup(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var outgoingComponentArray = OutgoingGroup.GetComponentDataArray<T>();
		var outgoingEntityArray = OutgoingGroup.GetEntityArray();
		for (var i = 0; i < outgoingComponentArray.Length; i++)
		{
			Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
		}
		
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(Entity entity, T component);
}

[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class InitializeComponentGroupSystem<T,S> : BaseComponentSystem
	where T : MonoBehaviour
	where S : struct, IComponentData
{
	ComponentGroup IncomingGroup;
	string name;

	public InitializeComponentGroupSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		IncomingGroup = GetComponentGroup(typeof(T),ComponentType.Subtractive<S>());
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		var incomingEntityArray = IncomingGroup.GetEntityArray();
		if (incomingEntityArray.Length > 0)
		{
			for (var i = 0; i < incomingEntityArray.Length; i++)
			{
				var entity = incomingEntityArray[i];
				PostUpdateCommands.AddComponent(entity,new S());
			}
			Initialize(ref IncomingGroup);
		}
		Profiler.EndSample();
	}

	protected abstract void Initialize(ref ComponentGroup group);
}



[DisableAutoCreation]
[AlwaysUpdateSystem]
public abstract class DeinitializeComponentGroupSystem<T> : BaseComponentSystem
	where T : MonoBehaviour
{
	ComponentGroup OutgoingGroup;
	string name;

	public DeinitializeComponentGroupSystem(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		name = GetType().Name;
		OutgoingGroup = GetComponentGroup(typeof(T), typeof(DespawningEntity));
	}
    
	protected override void OnUpdate()
	{
		Profiler.BeginSample(name);

		if (OutgoingGroup.CalculateLength() > 0)
			Deinitialize(ref OutgoingGroup);
		
		Profiler.EndSample();
	}

	protected abstract void Deinitialize(ref ComponentGroup group);
}
