#define USE_EXIT_WORKAROUND_FOGBUGZ_1062258
using System;
using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using UnityObject = UnityEngine.Object;
using System.IO;

namespace  UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : EditorWindow
    {
        ShortcutHandler m_ShortcutHandler;

        protected void SetupFramingShortcutHandler(VFXView view)
        {
            m_ShortcutHandler = new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
                {
                    {Event.KeyboardEvent("a"), view.FrameAll },
                    {Event.KeyboardEvent("f"), view.FrameSelection },
                    {Event.KeyboardEvent("o"), view.FrameOrigin },
                    {Event.KeyboardEvent("^#>"), view.FramePrev },
                    {Event.KeyboardEvent("^>"), view.FrameNext },
                    {Event.KeyboardEvent("#^r"), view.Resync},
                    {Event.KeyboardEvent("F7"), view.Compile},
                    {Event.KeyboardEvent("#d"), view.OutputToDot},
                    {Event.KeyboardEvent("^#d"), view.OutputToDotReduced},
                    {Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding},
                    {Event.KeyboardEvent("^r"), view.ReinitComponents},
                    {Event.KeyboardEvent("F5"), view.ReinitComponents},
                });
        }

        public static VFXViewWindow currentWindow;


        [MenuItem("Window/Visual Effects", false, 3010)]
        public static void AAToCreateSubmenuAtRightPlace()
        {
        }
        
        [MenuItem("Window/Visual Effects/Visual Effect Graph",false,3011)]
        public static void ShowWindow()
        {
            GetWindow<VFXViewWindow>();
        }

        public VFXView graphView
        {
            get; private set;
        }
        public void LoadAsset(VisualEffectAsset asset, VisualEffect effectToAttach)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            //Transitionning code
            if (resource == null)
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(AssetDatabase.GetAssetPath(asset));
            }

            LoadResource(resource, effectToAttach);
        }

        public void LoadResource(VisualEffectResource resource, VisualEffect effectToAttach = null)
        {
            if (graphView.controller == null || graphView.controller.model != resource)
            {
                bool differentAsset = resource != m_DisplayedResource;

                m_DisplayedResource = resource;
                graphView.controller = VFXViewController.GetController(resource, true);
                graphView.UpdateGlobalSelection();
                if (differentAsset)
                {
                    graphView.FrameNewController();
                }
            }
            if (effectToAttach != null && graphView.controller != null && graphView.controller.model != null && effectToAttach.visualEffectAsset == graphView.controller.model.asset)
                graphView.attachedComponent = effectToAttach;
        }

        protected VisualEffectResource GetCurrentResource()
        {
            var objs = Selection.objects;

            VisualEffectResource selectedResource = null;
            if (objs != null && objs.Length == 1)
            {
                if (objs[0] is VisualEffectAsset)
                {
                    VisualEffectAsset asset = objs[0] as VisualEffectAsset;
                    selectedResource = asset.GetResource();
                }
                else if (objs[0] is VisualEffectResource)
                {
                    selectedResource = objs[0] as VisualEffectResource;
                }
            }
            if (selectedResource == null)
            {
                int instanceID = Selection.activeInstanceID;

                if (instanceID != 0)
                {
                    string path = AssetDatabase.GetAssetPath(instanceID);
                    if (path.EndsWith(".vfx"))
                    {
                        selectedResource = VisualEffectResource.GetResourceAtPath(path);
                    }
                }
            }
            if (selectedResource == null && m_DisplayedResource != null)
            {
                selectedResource = m_DisplayedResource;
            }
            return selectedResource;
        }

        protected void OnEnable()
        {
            VFXManagerEditor.CheckVFXManager();

            graphView = new VFXView();
            graphView.StretchToParentSize();
            SetupFramingShortcutHandler(graphView);

            this.GetRootVisualContainer().Add(graphView);


            var currentAsset = GetCurrentResource();
            if (currentAsset != null)
            {
                LoadResource(currentAsset);
            }

            autoCompile = true;


            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            graphView.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);


            VisualElement rootVisualElement = this.GetRootVisualContainer();
            if (rootVisualElement.panel != null)
            {
                rootVisualElement.AddManipulator(m_ShortcutHandler);
            }

            currentWindow = this;

            /*if (m_ViewScale != Vector3.zero)
            {
                graphView.UpdateViewTransform(m_ViewPosition, m_ViewScale);
            }*/
#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit += Quitting_Workaround;
#endif
        }

#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
        private bool Quitting_Workaround()
        {
            if (graphView != null)
                graphView.controller = null;
            return true;
        }

#endif

        protected void OnDisable()
        {
#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit -= Quitting_Workaround;
#endif

            if (graphView != null)
            {
                graphView.UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
                graphView.UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
                graphView.controller = null;
            }
            currentWindow = null;
        }

        void OnFocus()
        {
            if (graphView != null)
                graphView.UpdateGlobalSelection();
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.AddManipulator(m_ShortcutHandler);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            VisualElement rootVisualElement = UIElementsEntryPoint.GetRootVisualContainer(this);
            rootVisualElement.RemoveManipulator(m_ShortcutHandler);
        }

        public bool autoCompile {get; set; }

        void Update()
        {
            if (graphView == null)
                return;
            VFXViewController controller = graphView.controller;
            var filename = "No Asset";
            if (controller != null)
            {
                controller.NotifyUpdate();
                if (controller.model != null)
                {
                    var graph = controller.graph;
                    if (graph != null)
                    {
                        filename = controller.name;

                        if (!graph.saved)
                        {
                            filename += "*";
                        }


                        graph.RecompileIfNeeded(!autoCompile);
                        controller.RecompileExpressionGraphIfNeeded();
                    }
                }
            }
            titleContent.text = filename;
        }

        [SerializeField]
        private VisualEffectResource m_DisplayedResource;
    }
}
