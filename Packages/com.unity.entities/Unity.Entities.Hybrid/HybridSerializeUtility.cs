using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Entities.Serialization
{
    public class SerializeUtilityHybrid
    {
        public static void Serialize(EntityManager manager, BinaryWriter writer, out GameObject sharedData)
        {
            int[] sharedComponentIndices;
            SerializeUtility.SerializeWorld(manager, writer, out sharedComponentIndices);
            sharedData = SerializeSharedComponents(manager, sharedComponentIndices);
        }

        public static void Deserialize(EntityManager manager, BinaryReader reader, GameObject sharedData)
        {
            int sharedComponentCount = DeserializeSharedComponents(manager, sharedData, "");
            var transaction = manager.BeginExclusiveEntityTransaction();
            SerializeUtility.DeserializeWorld(transaction, reader);
            ReleaseSharedComponents(transaction, sharedComponentCount);
            manager.EndExclusiveEntityTransaction();
        }

        public static void ReleaseSharedComponents(ExclusiveEntityTransaction transaction, int sharedComponentCount)
        {
            // Chunks have now taken over ownership of the shared components (reference counts have been added)
            // so remove the ref that was added on deserialization
            for (int i = 0; i < sharedComponentCount; ++i)
            {
                transaction.SharedComponentDataManager.RemoveReference(i+1);
            }
        }

        public static GameObject SerializeSharedComponents(EntityManager manager, int[] sharedComponentIndices)
        {
            if (sharedComponentIndices.Length == 0)
                return null;

            var go = new GameObject("SharedComponents");
            go.SetActive(false);

            for (int i = 0; i != sharedComponentIndices.Length; i++)
            {
                var sharedData = manager.m_SharedComponentManager.GetSharedComponentDataBoxed(sharedComponentIndices[i]);

                var typeName = sharedData.GetType().FullName + "Component";
                var componentType = sharedData.GetType().Assembly.GetType(typeName);
                if (componentType == null)
                    throw new System.ArgumentException($"SharedComponentDataWrapper<{sharedData.GetType().FullName}> must be named '{typeName}'");

                var com = go.AddComponent(componentType) as ComponentDataWrapperBase;
                #if UNITY_EDITOR
                if (!EditorUtility.IsPersistent(MonoScript.FromMonoBehaviour(com)))
                {
                    throw new System.ArgumentException($"SharedComponentDataWrapper<{sharedData.GetType().FullName}> must be defined in a file with the same name as the wrapper class");
                }
                #endif
                com.UpdateSerializedData(manager, sharedComponentIndices[i]);
            }

            return go;
        }

        public static int DeserializeSharedComponents(EntityManager manager, GameObject gameobject, string debugSceneName)
        {
            if (gameobject == null)
                return 0;

            manager.m_SharedComponentManager.PrepareForDeserialize();

            var sharedData = gameobject.GetComponents<ComponentDataWrapperBase>();
            for (int i = 0; i != sharedData.Length; i++)
            {
                int index = sharedData[i].InsertSharedComponent(manager);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index != i + 1)
                {
                    var newComponent = sharedData[i];
                    var existingComponent = manager.m_SharedComponentManager.GetSharedComponentDataBoxed(index);
                    throw new System.ArgumentException($"Shared Component {i} was inserted but got index {index} at load time than at build time when loading {debugSceneName}..\n{newComponent} vs {existingComponent}");
                }
#endif
            }

            return sharedData.Length;
        }
    }
}
