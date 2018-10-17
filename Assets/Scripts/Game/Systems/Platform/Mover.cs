using Unity.Entities;
using UnityEngine;

[ExecuteAlways]
public class Mover : MonoBehaviour	 
{
	public GameObject target;
	public Vector3 relativeEndPoint = Vector3.up*10;
	public float waitDuration = 4;
	public float moveSpeed = 3;

#if UNITY_EDITOR

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawIcon(transform.position,"Elevator.tif");

	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawLine(transform.position, transform.position+ relativeEndPoint);
		if (target != null)
			target.transform.position = transform.position;

		if (target != null)
		{
			target.transform.position = transform.position;
		}
	}

#endif	
}


[DisableAutoCreation]
public class MoverUpdate : BaseComponentSystem<Mover>       
{
	public MoverUpdate(GameWorld world) : base(world) {}

	protected override void Update(Entity entity, Mover mover)
	{
		// TODO (mogensh) Platforms disabled until we get stable interplation delay that can be used when predicting on platform
		return;
		
		var time = m_world.worldTime; 
		if (mover.target == null)
			return;
		
		var moveDistance = mover.relativeEndPoint.magnitude;
		var moveDir = mover.relativeEndPoint.normalized;
		var baseMoveDuration = moveDistance / mover.moveSpeed;
		var totalDuration = mover.waitDuration*2 + baseMoveDuration*2;

		
		var totalTickDuration = Mathf.FloorToInt(totalDuration / time.tickInterval);
		var tickTime = time.tick % totalTickDuration;

		var totalTime = tickTime * time.tickInterval + time.tickDuration;

		var moveForwardStart = mover.waitDuration;
		var moveForwardEnd = moveForwardStart + baseMoveDuration;
		var moveBackwardsStart = moveForwardStart + baseMoveDuration + mover.waitDuration;

		var moveDuration = 0.0f;
		if (totalTime > moveForwardStart)
		{
			if (totalTime < moveForwardEnd)
			{
				moveDuration = totalTime - moveForwardStart;
			}
			else if (totalTime <= moveBackwardsStart)
			{
				moveDuration = baseMoveDuration;
			}
			else
			{
				moveDuration = baseMoveDuration - (totalTime - moveBackwardsStart);
			}
		}
	
		mover.target.transform.position = mover.transform.position + moveDuration*mover.moveSpeed*moveDir;
	}

}
