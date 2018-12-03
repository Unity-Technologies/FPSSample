#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.IO;

namespace PocketHammer
{
	public class TCResourceHelper {

		public static T LoadAsset<T>(string path) where T : UnityEngine.Object
		{
			string rootPath = FindLocalPluginRoot();
			string completePath = rootPath + "/" + path;
			T a = (T)AssetDatabase.LoadAssetAtPath(completePath, typeof(T));	
			return a;
		}

		private static string FindLocalPluginRoot() {

			string path = "unknown";
			if(FindFirstPathWithLeafFolder(Application.dataPath, "TerrainCombiner", ref path))
			{
				path = path.Replace(Application.dataPath,"Assets");
			}

			return path;
		}

		private static bool FindFirstPathWithLeafFolder(string startFolder, string folderName, ref string path)
		{
			string[] folderEntries = Directory.GetDirectories(startFolder);

			for (int n = 0, len = folderEntries.Length; n < len; ++n)
			{
				string leafFolderName = Path.GetFileName(folderEntries[n]);

				if (leafFolderName == folderName)
				{
					path = folderEntries[n];
					return true;
				}
				else
				{
					bool found = FindFirstPathWithLeafFolder(folderEntries[n], folderName, ref path);
					if (found)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}

#endif