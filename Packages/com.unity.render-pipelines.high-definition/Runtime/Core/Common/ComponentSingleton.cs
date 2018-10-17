namespace UnityEngine.Experimental.Rendering
{
    // Use this class to get a static instance of a component
    // Mainly used to have a default instance
    public static class ComponentSingleton<TType>
        where TType : Component
    {
        static TType s_Instance = null;
        public static TType instance
        {
            get
            {
                if (s_Instance == null)
                {
                    GameObject go = new GameObject("Default " + typeof(TType)) { hideFlags = HideFlags.HideAndDontSave };
                    go.SetActive(false);
                    s_Instance = go.AddComponent<TType>();
                }

                return s_Instance;
            }
        }
    }
}
