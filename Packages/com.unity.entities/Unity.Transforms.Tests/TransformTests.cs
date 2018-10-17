using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Entities.Tests
{
    [TestFixture]
    public class TransformTests : ECSTestsFixture
    {
        unsafe bool AssertCloseEnough(float4x4 a, float4x4 b)
        {
            float* ap = (float*) &a.c0.x;
            float* bp = (float*) &b.c0.x;
            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(expected:ap[i],actual:bp[i],delta:0.01f);
            }
            return true;
        }

        void Log(float4x4 a)
        {
            Debug.Log($"{a.c0.x:0.000} {a.c0.y:0.000} {a.c0.z:0.000} {a.c0.w:0.000}");
            Debug.Log($"{a.c1.x:0.000} {a.c1.y:0.000} {a.c1.z:0.000} {a.c1.w:0.000}");
            Debug.Log($"{a.c2.x:0.000} {a.c2.y:0.000} {a.c2.z:0.000} {a.c2.w:0.000}");
            Debug.Log($"{a.c3.x:0.000} {a.c3.y:0.000} {a.c3.z:0.000} {a.c3.w:0.000}");
        }
            
        [Test]
        public void TRS_ChildPosition()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var child = m_Manager.CreateEntity(typeof(Position));
            var attach = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            var childWorldPosition = m_Manager.GetComponentData<LocalToWorld>(child).Value.c3;
            Assert.AreEqual(expected:1.0f,actual:childWorldPosition.x,delta:0.01f);
            Assert.AreEqual(expected:2.0f,actual:childWorldPosition.y,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:childWorldPosition.z,delta:0.01f);
        }

        [Test]
        public void TRS_ParentAddedRemoved()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var child = m_Manager.CreateEntity(typeof(Position));
            var attach = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            Assert.IsTrue(m_Manager.HasComponent<Attached>(child));
            Assert.IsTrue(m_Manager.HasComponent<Parent>(child));
            Assert.IsFalse(m_Manager.Exists(attach));

            m_Manager.DestroyEntity(parent);
            m_Manager.DestroyEntity(child);

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            Assert.IsFalse(m_Manager.Exists(parent));
            Assert.IsFalse(m_Manager.Exists(child));
        }

        [Test]
        public void TRS_FreezeChild()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var child = m_Manager.CreateEntity(typeof(Position),typeof(Static));
            var attach = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(0.0f, 1.0f, 0.0f), math.up())});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            Assert.IsFalse(m_Manager.Exists(attach));
            Assert.IsTrue(m_Manager.HasComponent<Frozen>(child));

            var childWorldPosition = m_Manager.GetComponentData<LocalToWorld>(child).Value.c3;
            Assert.AreEqual(expected:1.0f,actual:childWorldPosition.x,delta:0.01f);
            Assert.AreEqual(expected:2.0f,actual:childWorldPosition.y,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:childWorldPosition.z,delta:0.01f);

            m_Manager.DestroyEntity(parent);
            m_Manager.DestroyEntity(child);

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            Assert.AreEqual(0, m_ManagerDebug.EntityCount);
        }

        [Test]
        public void TRS_ParentChangesChild()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var child = m_Manager.CreateEntity(typeof(Position));
            var attach = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(0.0f, 1.0f, 0.0f), math.up())});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            var childWorldPosition = m_Manager.GetComponentData<LocalToWorld>(child).Value.c3;
            Assert.AreEqual(expected:0.0f,actual:childWorldPosition.x,delta:0.01f);
            Assert.AreEqual(expected:3.0f,actual:childWorldPosition.y,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:childWorldPosition.z,delta:0.01f);
        }

        [Test]
        public void TRS_InnerDepth()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var parent2 = m_Manager.CreateEntity(typeof(Position));
            var child = m_Manager.CreateEntity(typeof(Position));
            var attach = m_Manager.CreateEntity(typeof(Attach));
            var attach2 = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(parent2, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});

            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = parent2});
            m_Manager.SetComponentData(attach2, new Attach {Parent = parent2, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            var parentDepth = m_Manager.GetSharedComponentData<Depth>(parent);
            var parent2Depth = m_Manager.GetSharedComponentData<Depth>(parent2);

            Assert.AreEqual(0,parentDepth.Value);
            Assert.AreEqual(1,parent2Depth.Value);

            var childWorldPosition = m_Manager.GetComponentData<LocalToWorld>(child).Value.c3;
            Assert.AreEqual(expected:2.0f,actual:childWorldPosition.x,delta:0.01f);
            Assert.AreEqual(expected:2.0f,actual:childWorldPosition.y,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:childWorldPosition.z,delta:0.01f);
        }

        [Test]
        public void TRS_LocalPositions()
        {
            var parent = m_Manager.CreateEntity(typeof(Position), typeof(Rotation));
            var parent2 = m_Manager.CreateEntity(typeof(Position));
            var child = m_Manager.CreateEntity(typeof(Position));
            var attach = m_Manager.CreateEntity(typeof(Attach));
            var attach2 = m_Manager.CreateEntity(typeof(Attach));

            m_Manager.SetComponentData(parent, new Position {Value = new float3(0, 2, 0)});
            m_Manager.SetComponentData(parent, new Rotation {Value = quaternion.lookRotation(new float3(1.0f, 0.0f, 0.0f), math.up())});
            m_Manager.SetComponentData(parent2, new Position {Value = new float3(0, 0, 1)});
            m_Manager.SetComponentData(child, new Position {Value = new float3(0, 0, 1)});

            m_Manager.SetComponentData(attach, new Attach {Parent = parent, Child = parent2});
            m_Manager.SetComponentData(attach2, new Attach {Parent = parent2, Child = child});

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            var parent2LocalPosition = m_Manager.GetComponentData<LocalToParent>(parent2).Value.c3;
            Assert.AreEqual(expected:0.0f,actual:parent2LocalPosition.x,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:parent2LocalPosition.y,delta:0.01f);
            Assert.AreEqual(expected:1.0f,actual:parent2LocalPosition.z,delta:0.01f);

            var childLocalPosition = m_Manager.GetComponentData<LocalToParent>(child).Value.c3;
            Assert.AreEqual(expected:0.0f,actual:childLocalPosition.x,delta:0.01f);
            Assert.AreEqual(expected:0.0f,actual:childLocalPosition.y,delta:0.01f);
            Assert.AreEqual(expected:1.0f,actual:childLocalPosition.z,delta:0.01f);
        }

        [Test]
        public void TRS_LocalPositionsHierarchy()
        {
            var pi = 3.14159265359f;
            var rotations = new quaternion[]
            {
                quaternion.eulerYZX(new float3(0.125f * pi, 0.0f, 0.0f)),
                quaternion.eulerYZX(new float3(0.5f * pi, 0.0f, 0.0f)),
                quaternion.eulerYZX(new float3(pi, 0.0f, 0.0f)),
            };
            var translations = new float3[]
            {
                new float3(0.0f, 0.0f, 1.0f),
                new float3(0.0f, 1.0f, 0.0f),
                new float3(1.0f, 0.0f, 0.0f),
                new float3(0.5f, 0.5f, 0.5f),
            };

            //  0: R:[0] T:[0]
            //  1:  - R:[1] T:[1]
            //  2:    - R:[2] T:[0]
            //  3:    - R:[2] T:[1]
            //  4:    - R:[2] T:[2]
            //  5:      - R:[1] T:[0]
            //  6:      - R:[1] T:[1]
            //  7:      - R:[1] T:[2]
            //  8:  - R:[2] T:[2]
            //  9:    - R:[1] T:[0]
            // 10:    - R:[1] T:[1]
            // 11:    - R:[1] T:[2]
            // 12:      - R:[0] T:[0]
            // 13:        - R:[0] T:[1]
            // 14:          - R:[0] T:[2]
            // 15:            - R:[0] T:[2]

            var rotationIndices = new int[] {0, 1, 2, 2, 2, 1, 1, 1, 2, 1, 1, 1, 0, 0, 0, 0};
            var translationIndices = new int[] {0, 1, 0, 1, 2, 0, 1, 2, 2, 0, 1, 2, 0, 1, 2, 2};
            var parentIndices = new int[] {-1, 0, 1, 1, 1, 4, 4, 4, 0, 8, 8, 8, 11, 12, 13, 14};

            var expectedLocalToParent = new float4x4[16];
            for (int i = 0; i < 16; i++)
            {
                var rotationIndex = rotationIndices[i];
                var translationIndex = translationIndices[i];
                var localToParent = new float4x4(rotations[rotationIndex], translations[translationIndex]);
                expectedLocalToParent[i] = localToParent;
            }

            var expectedLocalToWorld = new float4x4[16];

            expectedLocalToWorld[0] = expectedLocalToParent[0];
            for (int i = 1; i < 16; i++)
            {
                var parentIndex = parentIndices[i];
                expectedLocalToWorld[i] = math.mul(expectedLocalToWorld[parentIndex], expectedLocalToParent[i]);
            }

            var bodyArchetype = m_Manager.CreateArchetype(typeof(Position), typeof(Rotation));
            var attachArchetype = m_Manager.CreateArchetype(typeof(Attach));
            var bodyEntities = new NativeArray<Entity>(16, Allocator.TempJob);
            var attachEntities = new NativeArray<Entity>(15, Allocator.TempJob);

            m_Manager.CreateEntity(bodyArchetype, bodyEntities);
            m_Manager.CreateEntity(attachArchetype, attachEntities);

            for (int i = 0; i < 16; i++)
            {
                var rotationIndex = rotationIndices[i];
                var translationIndex = translationIndices[i];
                var rotation = new Rotation {Value = rotations[rotationIndex]};
                var position = new Position {Value = translations[translationIndex]};

                m_Manager.SetComponentData(bodyEntities[i], rotation);
                m_Manager.SetComponentData(bodyEntities[i], position);
            }

            for (int i = 1; i < 16; i++)
            {
                var parentIndex = parentIndices[i];
                m_Manager.SetComponentData(attachEntities[i - 1],
                    new Attach {Parent = bodyEntities[parentIndex], Child = bodyEntities[i]});
            }

            World.GetOrCreateManager<EndFrameTransformSystem>().Update();

            // Check all non-root LocalToParent
            for (int i = 1; i < 16; i++)
            {
                var entity = bodyEntities[i];
                var localToParent = m_Manager.GetComponentData<LocalToParent>(entity).Value;

                AssertCloseEnough(expectedLocalToParent[i], localToParent);
            }

            // Check all LocalToWorld
            for (int i = 0; i < 16; i++)
            {
                var entity = bodyEntities[i];
                var localToWorld = m_Manager.GetComponentData<LocalToWorld>(entity).Value;

                AssertCloseEnough(expectedLocalToWorld[i], localToWorld);
            }

            bodyEntities.Dispose();
            attachEntities.Dispose();
        }
    }
}
