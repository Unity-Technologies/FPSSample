using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class SkeletonImporter : AssetPostprocessor
{
    uint m_Version = 3;
    public override uint GetVersion() {return m_Version;}

    List<TwistConfig> m_TwistConfigs = new List<TwistConfig>();
    List<FanConfig> m_FanConfigs = new List<FanConfig>();
    List<TranslateScaleConfig> m_TranslateScaleConfigs = new List<TranslateScaleConfig>();

    enum ComponentType
    {
        TwistComponent,
        FanComponent,
        TranslateScaleComponent,
    }

    [Serializable]
    public class ComponentConfig
    {
        public List<TwistConfig> twistConfig = new List<TwistConfig>();
        public List<FanConfig> fanConfig = new List<FanConfig>();
        public List<TranslateScaleConfig> translateScaleConfig = new List<TranslateScaleConfig>();
    }
    
    
    [Serializable]
    public class TypeData
    {
        public string type = "";
        public TypeData() {}
        public TypeData(string type)
        {
            this.type = type;
        }
    }

    [Serializable]
    public class TwistConfig
    {
        public string driver;
        public string aimAxis = "";
        public List<String> twistJoints = new List<String>();
        public List<float> twistFactors = new List<float>();

        public TwistConfig() {}
        public TwistConfig(string driver, string aimAxis, List<String> twistJoints, List<float> twistFactors)
        {
            this.driver = driver;
            this.aimAxis = aimAxis;
            this.twistJoints = twistJoints;
            this.twistFactors = twistFactors;
        }
    }
    
    [Serializable]
    public class FanConfig
    {
        public string driverA;
        public string driverB;
        public string driven;

        public FanConfig() {}
        public FanConfig(string driverA, string driverB, string driven)
        {
            this.driverA = driverA;
            this.driverB = driverB;
            this.driven = driven;
        }
    }

    
    [Serializable]
    public class TranslateScaleConfig
    {
        public string driver;
        public string aimAxis = "";
        public List<String> stretchBones = new List<String>();
        public List<float> stretchFactors = new List<float>();
        public List<float> scaleFactors = new List<float>();

        
        public TranslateScaleConfig() {}
        public TranslateScaleConfig(string driver, string aimAxis, List<String> stretchBones, List<float> stretchFactors,  List<float> scaleFactors)
        {
            this.driver = driver;
            this.aimAxis = aimAxis;
            this.stretchBones = stretchBones;
            this.stretchFactors = stretchFactors;
            this.scaleFactors = scaleFactors;
        }
    }   
    
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, System.Object[] values)
    {
        string stringValue;
        for (int i = 0; i < propNames.Length; i++)
        {
            if (propNames[i] == "sampleGame_compData")
            {
                stringValue = values[i] as System.String;
                if (!String.IsNullOrEmpty(stringValue))
                {
                    ComponentConfig componentConfig = new ComponentConfig();
                    componentConfig = JsonUtility.FromJson<ComponentConfig>(stringValue);

                    foreach (var twistConfig in componentConfig.twistConfig)
                    {
                        m_TwistConfigs.Add(twistConfig); 
                    }
                    foreach (var fanConfig in componentConfig.fanConfig)
                    {
                            m_FanConfigs.Add(fanConfig);                    
                    }
                    foreach (var translateScaleConfig in componentConfig.translateScaleConfig)
                    {
                        m_TranslateScaleConfigs.Add(translateScaleConfig);
                    }                    
                }               
            }
        }
    }

    void OnPostprocessModel(GameObject root)
    {
        // Attempt to find skeleton root
        var skeletonRoot = root.transform.Find("Skeleton");
        
        // TODO: (sunek) Be able to setup skeleton within the editor and get rid of this?
        if (skeletonRoot == null)
        {
            var animator = root.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    skeletonRoot = animator.GetBoneTransform(HumanBodyBones.Hips).parent;   
                }
            }            
        }
        
        if (skeletonRoot == null)
            return;
        
        if (skeletonRoot.GetComponent<MeshFilter>() || skeletonRoot.GetComponent<SkinnedMeshRenderer>() || skeletonRoot.GetComponent<Collider>())
        {
            return;
        }

        var skeletonComponent = root.AddComponent<Skeleton>();
        skeletonComponent.StoreBoneData(skeletonRoot);
        AddTwistComponents(root, m_TwistConfigs);
        AddFanComponents(root, m_FanConfigs);
        AddTranslateScaleComponents(root, m_TranslateScaleConfigs);
    }

   static void AddTwistComponents(GameObject root, List<TwistConfig> twistConfigs)
    {
         if (twistConfigs.Count == 0)
            return;

        var twistComponent = root.AddComponent<Twist>();

        for (var i = 0; i < twistConfigs.Count; i++)
        {
            var twistConfig = twistConfigs[i];
            var twistChain = new Twist.TwistChain();
            twistChain.driver = FindInChildren(root.transform, twistConfig.driver);

            if (!twistChain.driver)
                continue;

            // TODO: Support variable axis
//            switch (twistConfig.aimAxis)
//            {
//                case "X":
//                    twistComponent.aimAxis = Twist.AimAxis.X;
//                    break;
//                case "Y":
//                    twistComponent.aimAxis = Twist.AimAxis.Y;
//                    break;
//                case "Z":
//                    twistComponent.aimAxis = Twist.AimAxis.Z;
//                    break;
//                default:
//                    twistComponent.aimAxis = Twist.AimAxis.X;
//                    break;
//            }
                        
            twistChain.twistJoints = new List<Twist.TwistJoint>();
            for (var j = 0; j < twistConfig.twistJoints.Count; j++)
            {
                var twistJoint = new Twist.TwistJoint();
                twistJoint.joint = FindInChildren(root.transform, twistConfig.twistJoints[j]);
                twistJoint.factor = twistConfig.twistFactors[j];
                twistChain.twistJoints.Add(twistJoint);
            }

            if (twistChain.HasValidData())
                twistComponent.twistChains.Add(twistChain);
        }

        twistComponent.SetBindpose();
    }
    
    static void AddFanComponents(GameObject root, List<FanConfig> fanConfigs)
    {
        if (fanConfigs.Count == 0)
            return;

        var fanComponent = root.AddComponent<Fan>();

        for (var i = 0; i < fanConfigs.Count; i++)
        {
            var fanData = new Fan.FanData();            
            fanData.driven = FindInChildren(root.transform, fanConfigs[i].driven);
            fanData.driverA = FindInChildren(root.transform, fanConfigs[i].driverA);
            fanData.driverB = FindInChildren(root.transform, fanConfigs[i].driverB);

            if (fanData.HasValidData())
            {
                fanComponent.fanDatas.Add(fanData);
            }
        }
    }

    static void AddTranslateScaleComponents(GameObject root, List<TranslateScaleConfig> translateScaleConfigs)
    {
        if (translateScaleConfigs.Count == 0)
            return;

        var component = root.AddComponent<TranslateScale>();

        for (var i = 0; i < translateScaleConfigs.Count; i++)
        {
            var config = translateScaleConfigs[i];
            var chain = new TranslateScale.TranslateScaleChain();
            chain.driver = FindInChildren(root.transform, config.driver);
            
            if (!chain.driver)
                continue;
            
            chain.drivenJoints = new List<TranslateScale.Driven>();
            for (var j = 0; j < config.stretchBones.Count; j++)
            {
                var driven = new TranslateScale.Driven();
                driven.joint = FindInChildren(root.transform,config.stretchBones[j]);
                driven.scaleFactor = config.scaleFactors[j];
                driven.strectchFactor = config.stretchFactors[j];
                chain.drivenJoints.Add(driven);
            }

            if (chain.HasValidData())
                component.chains.Add(chain);
        }

        component.SetBindpose();
    }
    
    static Transform FindInChildren(Transform parent, string name)
    {
        var result = parent.Find(name);
        if (result != null)
            return result;
        foreach (Transform child in parent)
        {
            result = FindInChildren(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
