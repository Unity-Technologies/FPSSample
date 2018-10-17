using UnityEngine;

namespace AssetBundleBrowser
{
    /// <summary>
    /// This class maintains a record of a loaded asset bundle, allowing us
    /// to associate the full path of an asset bundle with the actual bundle,
    /// so that we can:
    /// 
    /// 1. distinguish between bundle variants, which, when loaded
    /// resolve to the same name. 
    /// 
    /// 2. Differentiate between the same asset bundles built for different platforms.
    /// 
    /// ex:
    ///
    /// Two asset bundle variants:
    /// 
    /// - variant one: mycylinder.one
    /// - variant two: mycylinder.two
    /// 
    /// Will Resolve to "mycylinder" when loaded.
    /// 
    /// Likewise, 
    /// 
    /// - iOS: AssetBundles/iOS/myBundle
    /// - Android: AssetBundle/Android/myBundle
    /// 
    /// Will both resolve to "mybundle" when loaded.
    /// 
    /// </summary>
    internal class AssetBundleRecord
    {
        /// <summary>
        /// Full path of the asset bundle.
        /// </summary>
        internal string path { get; private set; }

        /// <summary>
        /// Reference to the loaded asset bundle associated with the path.
        /// </summary>
        internal AssetBundle bundle { get; private set; }

        internal AssetBundleRecord(string path, AssetBundle bundle)
        {
            if (string.IsNullOrEmpty(path) ||
                null == bundle)
            {
                string msg = string.Format("AssetBundleRecord encountered invalid parameters path={0}, bundle={1}",
                    path,
                    bundle);
                
                throw new System.ArgumentException(msg);
            }

            this.path = path;
            this.bundle = bundle;
        }
    }
}
