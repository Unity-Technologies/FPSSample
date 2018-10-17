using UnityEditor;
using UnityEngine;

using Unity.Entities.Properties;
using Unity.Properties;

namespace Unity.Entities.Editor
{
    [CustomEditor(typeof(EntitySelectionProxy))]
    public class EntitySelectionProxyEditor : UnityEditor.Editor
    {
        private EntityIMGUIVisitor visitor;

        [SerializeField] private SystemInclusionList inclusionList;
        
        void OnEnable()
        {
            visitor = new EntityIMGUIVisitor();
            inclusionList = new SystemInclusionList();
        }

        public override void OnInspectorGUI()
        {
            var targetProxy = (EntitySelectionProxy) target;
            if (!targetProxy.Exists)
                return;
            var container = targetProxy.Container;
            (targetProxy.Container.PropertyBag as StructPropertyBag<EntityContainer>)?.Visit(ref container, visitor);

            GUI.enabled = true;
            
            inclusionList.OnGUI(targetProxy.World, targetProxy.Entity);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }
    }
}
