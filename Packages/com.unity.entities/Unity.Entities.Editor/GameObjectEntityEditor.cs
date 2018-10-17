
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor
{
    [CustomEditor(typeof(GameObjectEntity))]
    public class GameObjectEntityEditor : UnityEditor.Editor
    {
        [SerializeField] private SystemInclusionList inclusionList;

        private void OnEnable()
        {
            inclusionList = new SystemInclusionList();
        }

        public override void OnInspectorGUI()
        {
            var gameObjectEntity = (GameObjectEntity) target;
            if (gameObjectEntity.EntityManager == null || !gameObjectEntity.EntityManager.IsCreated || !gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
                return;

            inclusionList.OnGUI(World.Active, gameObjectEntity.Entity);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }
    }
}
