using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using AssetBundleBrowser;

namespace AssetBundleBrowserTests
{
    public class AssetBundleRecordTests
    {
        [TestCase]
        public void TestAssetBundleRecordConstructor()
        {
            this.VerifyConstructorException(null, null);
            this.VerifyConstructorException(string.Empty, null);
            this.VerifyConstructorException("bundleName.one", null);

            // Need an actual asset bundle for further tests, omitting since projects will differ.
        }

        private void VerifyConstructorException(string path, AssetBundle bundle)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {

                new AssetBundleRecord(path, bundle);
            });
        }
    }
}
