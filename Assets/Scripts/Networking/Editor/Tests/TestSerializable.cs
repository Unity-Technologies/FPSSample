using System;
using NUnit.Framework;
using UnityEngine;


namespace NetcodeTests
{
    public abstract class TestSerializable
    {
        public abstract void Deserialize(ref NetworkReader reader);

        public abstract void Serialize(ref NetworkWriter writer);

        public abstract void AssertReplicatedCorrectly(TestSerializable clientEntity, bool isPredicting);

    }
}
