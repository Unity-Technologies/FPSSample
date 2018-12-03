using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

#if UNITY_EDITOR

namespace PocketHammer
{
	[InitializeOnLoad]
	public class TCWorker {

		public delegate void Task();

		private class TaskData {
			public TerrainCombiner terrainCombiner;
			public Terrain terrain;
			public TerrainData terrainData;
			public int heightmapResolution;
			public int frameCount;
		}

		private static Queue<TaskData> pendingTasks = new Queue<TaskData>();
		private static TaskData currentTask = null;

		private static Queue<Task> TaskQueue = new Queue<Task>();
		private static object _queueLock = new object();

		static TCWorker()
		{
			EditorApplication.update += Update;
		}

		public static void RequestUpdate(TerrainCombiner combiner) {

			foreach(TaskData task in pendingTasks) {
				if(task.terrainCombiner == combiner) {

					// TODO: Update request
					return;
				}
			}

			TaskData newTask = new TaskData();
			newTask.terrainCombiner = combiner;
			newTask.terrain = combiner.GetComponent<Terrain>();
			newTask.terrainData = newTask.terrain.terrainData;
			newTask.heightmapResolution = newTask.terrainData.heightmapResolution;
			newTask.frameCount = 0;
			pendingTasks.Enqueue(newTask);
		}

		private static void Update() {
			if(currentTask != null) {

				lock (_queueLock)
				{
					int count = 0;
					int maxDuration = 200; 
					System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

					while(TaskQueue.Count > 0 && (stopwatch.ElapsedMilliseconds < maxDuration)) {
						TaskQueue.Dequeue()();
						count ++;
					}

//					int duration = (int)(stopwatch.ElapsedMilliseconds);
//					Debug.Log("handled " + count + " tasks in " + duration);
					if(TaskQueue.Count == 0) {
						currentTask = null;
					}
				}
			}
			else
			{
				// Should we start new th
				if(pendingTasks.Count > 0)
				{
					currentTask = pendingTasks.Dequeue();

					TerrainCombiner terrainCombiner = currentTask.terrainCombiner;

                    Debug.Log("TerrainCombiner starting update tasks");

					// Cache updated instantly so we get data for all parts in one frame
					UpdateCaches(terrainCombiner);


					
					lock (_queueLock)
					{
						// Start combine
						TaskQueue.Enqueue(delegate() { TCHeightmapHelper.StartCombine(terrainCombiner, terrainCombiner.groundLevelFraction); });
						TaskQueue.Enqueue(delegate() { TCMaterialHelper.StartCombine(terrainCombiner); });
	
						// Sample texture
						TaskQueue.Enqueue(delegate() { TCHeightmapHelper.SampleTexture(currentTask.terrainCombiner, currentTask.heightmapResolution); });
						for(int nLayer=0;nLayer<currentTask.terrainData.alphamapLayers;nLayer++) {
							int i = nLayer;
							TaskQueue.Enqueue(delegate() { TCMaterialHelper.SampleTexture(currentTask.terrainCombiner, i); });
						}

						// Apply data
						TaskQueue.Enqueue(delegate() { TCHeightmapHelper.ApplyData(currentTask.terrainCombiner); });
						TaskQueue.Enqueue(delegate() { TCMaterialHelper.ApplyData(currentTask.terrainCombiner); });
					}
				}
			}
		}

		private static void UpdateCaches(TerrainCombiner terrainCombiner) {

			// Update source caches
			for(int i=0;i<terrainCombiner.Instances.Length;i++) {
				TerrainCombinerSource source = terrainCombiner.Instances[i].source;
				if(source != null && source.CacheDirty) {

					TCHeightmapHelper.UpdateSourceCache(source);
					TCMaterialHelper.UpdateSourceCache(source);

					source.CacheDirty = false;
				}
			}

			// Update target caches
			if(terrainCombiner.CacheDirty) {

				TCHeightmapHelper.UpdateCombinerCache(terrainCombiner);
				TCMaterialHelper.UpdateCombinerCache(terrainCombiner);

				terrainCombiner.CacheDirty = false;
			}

		} 
	}
}

#endif
