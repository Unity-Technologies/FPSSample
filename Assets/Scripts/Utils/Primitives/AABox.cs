

using System;
using static Unity.Mathematics.math;
namespace Primitives
{
	using Unity.Mathematics;

	[Serializable]
	public struct AABox
	{
		public float3 center;
		public float3 size;
	}
}

