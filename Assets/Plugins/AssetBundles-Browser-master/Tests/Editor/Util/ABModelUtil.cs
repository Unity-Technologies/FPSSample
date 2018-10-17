using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using AssetBundleBrowser.AssetBundleModel;

namespace Assets.AssetBundles_Browser.Editor.Tests.Util
{
    class ABModelUtil
    {
        /// <summary>
        /// Empty texutre for testing purposes
        /// </summary>
        public static Texture2D FakeTexture2D
        {
            get { return new Texture2D(16, 16); }
        }

        /// <summary>
        /// This is the Models root folder object.
        /// </summary>
        public static BundleFolderConcreteInfo Root
        {
            get
            {
                FieldInfo rootFieldInfo = typeof(Model).GetField("s_RootLevelBundles",
                BindingFlags.NonPublic | BindingFlags.Static);
                BundleFolderConcreteInfo concreteFolder = rootFieldInfo.GetValue(null) as BundleFolderConcreteInfo;
                return concreteFolder;
            }
        }

        public static List<BundleInfo> BundlesToUpdate
        {
            get
            {
                FieldInfo info = typeof(Model).GetField("s_BundlesToUpdate", BindingFlags.NonPublic | BindingFlags.Static);
                List<BundleInfo> bundleInfo = info.GetValue(null) as List<BundleInfo>;
                return bundleInfo;

            }
        }

        public static IList MoveData
        {
            get
            {
                FieldInfo info = typeof(Model).GetField("s_MoveData", BindingFlags.NonPublic | BindingFlags.Static);
                var moveData = info.GetValue(null) as IList;
                return moveData;
            }
        }
    }
}
