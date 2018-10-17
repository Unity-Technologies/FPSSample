using UnityEngine;

namespace Unity.Entities.Editor
{
//    Commented out to avoid cluttering Create Asset menu
//    [CreateAssetMenu(fileName = "Styles.asset", menuName = "GUI Style Asset", order = 600)]
    public class GUIStyleAsset : ScriptableObject
    {

        public GUIStyle[] styles;
    }
}