using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3D.Tests
{
    /// <summary>Covers the standalone <see cref="DynamicTree"/> wrapper: proxy lifecycle, the three
    /// buffer-fill queries, filtering, introspection and validation. Also guards the ABI of the
    /// tree structs — the wrapper allocates <c>b3DynamicTree</c> by value, so a size mismatch would
    /// corrupt memory.</summary>
    public class DynamicTreeTests
    {
        private static B3Aabb Box(float3 center, float half) => new B3Aabb
        {
            LowerBound = center - half,
            UpperBound = center + half,
        };

        [Test]
        public void StructSizes_MatchNativeLayout()
        {
            Assert.AreEqual(80, UnsafeUtility.SizeOf<b3DynamicTree>(), "b3DynamicTree size vs native");
            Assert.AreEqual(28, UnsafeUtility.SizeOf<b3RayCastInput>(), "b3RayCastInput size vs native");
            Assert.AreEqual(40, UnsafeUtility.SizeOf<b3BoxCastInput>(), "b3BoxCastInput size vs native");
            Assert.AreEqual(8, UnsafeUtility.SizeOf<b3TreeStats>(), "b3TreeStats size vs native");
            Assert.AreEqual(16, UnsafeUtility.SizeOf<DynamicTreeHit>(), "DynamicTreeHit size");
        }

        [Test]
        public void Create_Dispose_Lifecycle()
        {
            var tree = new DynamicTree();
            Assert.IsTrue(tree.IsCreated);
            Assert.AreEqual(0, tree.ProxyCount);

            tree.Dispose();
            Assert.IsFalse(tree.IsCreated);
            Assert.DoesNotThrow(() => tree.Dispose()); // double-dispose is safe
        }

        [Test]
        public void CreateProxy_IncrementsCount_ReturnsDistinctIds()
        {
            using var tree = new DynamicTree();
            int a = tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 1);
            int b = tree.CreateProxy(Box(new float3(10, 0, 0), 1f), userData: 2);
            int c = tree.CreateProxy(Box(new float3(0, 10, 0), 1f), userData: 3);

            Assert.AreEqual(3, tree.ProxyCount);
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void Query_Overlap_FindsProxyAndUserData()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 42);
            tree.CreateProxy(Box(new float3(20, 0, 0), 1f), userData: 99);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            int n = tree.Query(Box(new float3(0, 0, 0), 0.5f), hits, out TreeStats stats);

            Assert.AreEqual(1, n);
            Assert.AreEqual(42ul, hits[0].UserData);
            Assert.IsTrue(stats.NodeVisits > 0 || stats.LeafVisits > 0, "query should visit tree nodes");
        }

        [Test]
        public void Query_Miss_ReturnsZero()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 1);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            Assert.AreEqual(0, tree.Query(Box(new float3(100, 100, 100), 1f), hits));
        }

        [Test]
        public void Query_BufferFull_StopsEarly()
        {
            using var tree = new DynamicTree();
            for (int i = 0; i < 3; i++)
                tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: (ulong)i); // three overlapping proxies

            Span<DynamicTreeHit> one = stackalloc DynamicTreeHit[1];
            Assert.AreEqual(1, tree.Query(Box(new float3(0, 0, 0), 2f), one), "should stop when buffer is full");
        }

        [Test]
        public void MoveProxy_UpdatesLocation()
        {
            using var tree = new DynamicTree();
            int p = tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 7);

            tree.MoveProxy(p, Box(new float3(50, 0, 0), 1f));

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            Assert.AreEqual(0, tree.Query(Box(new float3(0, 0, 0), 1f), hits), "old location empty after move");
            Assert.AreEqual(1, tree.Query(Box(new float3(50, 0, 0), 1f), hits), "found at new location");
        }

        [Test]
        public void DestroyProxy_RemovesIt()
        {
            using var tree = new DynamicTree();
            int p = tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 5);
            Assert.AreEqual(1, tree.ProxyCount);

            tree.DestroyProxy(p);
            Assert.AreEqual(0, tree.ProxyCount);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            Assert.AreEqual(0, tree.Query(Box(new float3(0, 0, 0), 1f), hits));
        }

        [Test]
        public void RayCast_CrossingProxy_Collected_MissReturnsZero()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(5, 0, 0), 1f), userData: 11);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            int hit = tree.RayCast(new float3(0, 0, 0), new float3(10, 0, 0), hits); // along +x through the box
            Assert.AreEqual(1, hit);
            Assert.AreEqual(11ul, hits[0].UserData);

            int miss = tree.RayCast(new float3(0, 0, 0), new float3(0, 10, 0), hits); // upward, misses x=5
            Assert.AreEqual(0, miss);
        }

        [Test]
        public void BoxCast_Sweep_Collected()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(5, 0, 0), 1f), userData: 13);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            int n = tree.BoxCast(Box(new float3(0, 0, 0), 0.5f), new float3(10, 0, 0), hits);
            Assert.AreEqual(1, n);
            Assert.AreEqual(13ul, hits[0].UserData);
        }

        [Test]
        public void CategoryMask_Filters()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 1, categoryBits: 0b10);

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            B3Aabb q = Box(new float3(0, 0, 0), 1f);

            Assert.AreEqual(0, tree.Query(q, hits, maskBits: 0b01), "non-matching mask → miss");
            Assert.AreEqual(1, tree.Query(q, hits, maskBits: 0b10), "matching mask → hit");
            Assert.AreEqual(0, tree.Query(q, hits, maskBits: 0b11, requireAllBits: true),
                "requireAllBits: category must contain every mask bit");
        }

        [Test]
        public void SetCategoryBits_RoundTrips()
        {
            using var tree = new DynamicTree();
            int p = tree.CreateProxy(Box(new float3(0, 0, 0), 1f), userData: 1, categoryBits: 0b01);
            tree.SetCategoryBits(p, 0b100);
            Assert.AreEqual(0b100ul, tree.GetCategoryBits(p));

            Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[8];
            Assert.AreEqual(1, tree.Query(Box(new float3(0, 0, 0), 1f), hits, maskBits: 0b100));
        }

        [Test]
        public void RootBounds_ContainsAllProxies()
        {
            using var tree = new DynamicTree();
            tree.CreateProxy(Box(new float3(-5, 0, 0), 1f), userData: 1);
            tree.CreateProxy(Box(new float3(5, 3, 0), 1f), userData: 2);

            B3Aabb root = tree.RootBounds;
            Assert.LessOrEqual(root.LowerBound.x, -6f);
            Assert.GreaterOrEqual(root.UpperBound.x, 6f);
            Assert.GreaterOrEqual(root.UpperBound.y, 4f);
        }

        [Test]
        public void Rebuild_KeepsTreeValid()
        {
            using var tree = new DynamicTree(4);
            var ids = new int[16];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = tree.CreateProxy(Box(new float3(i * 2f, 0, 0), 0.5f), userData: (ulong)i);
            for (int i = 0; i < ids.Length; i += 2)
                tree.MoveProxy(ids[i], Box(new float3(i * 2f, 5f, 0), 0.5f));

            Assert.GreaterOrEqual(tree.Rebuild(fullBuild: true), 0);
            Assert.DoesNotThrow(() => tree.Validate());
            Assert.DoesNotThrow(() => tree.ValidateNoEnlarged());

            Assert.IsTrue(tree.ByteCount > 0);
            Assert.GreaterOrEqual(tree.Height, 0);
            Assert.AreEqual(16, tree.ProxyCount);
        }
    }
}
