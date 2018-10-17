using System;

namespace UnityEngine.Recorder
{

    /// <summary>
    /// What is this: Provides the information needed to register Recorder classes with the RecorderInventory.
    /// Motivation  : Dynamically discover Recorder classes and provide a classification system and link between the recorder classes and their Settings classes.
    /// </summary>    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RecorderAttribute : Attribute
    {
        public Type settings;
        public string category;
        public string displayName;

        public RecorderAttribute(Type settingsType, string category, string displayName)
        {
            this.settings = settingsType;
            this.category = category;
            this.displayName = displayName;
        }
    }

    /// <summary>
    /// What is this: Indicate that a Input settings instance is scene specific and should not be shared accross scenes (not in a project wide asset)
    /// Motivation  : Some input settings target specific scenes, for example target a game object in the scene. Having the settings be stored in the 
    ///                 scene simplifies referencing.
    /// </summary>    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StoreInSceneAttribute : Attribute
    {
        public StoreInSceneAttribute()
        {
        }
    }

}
