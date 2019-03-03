using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Utils
{
    public class PointCacheAsset : ScriptableObject
    {
        public int PointCount;
        public Texture2D[] surfaces;
    }
}
