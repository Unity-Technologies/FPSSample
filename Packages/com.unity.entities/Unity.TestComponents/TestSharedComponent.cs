namespace Unity.Entities.Tests
{
    [System.Serializable]
    public struct TestShared : ISharedComponentData
    {
        public int Value;

        public TestShared(int value) { Value = value; }
    }

    class TestSharedComponent : SharedComponentDataWrapper<TestShared>
    {
        
    }
}
