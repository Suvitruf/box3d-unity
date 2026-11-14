using Box3D.Hybrid;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Tests
{
    public class PreviewOwnershipTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void OverlappingPreviews_DisposeIndependently(bool terrain)
        {
            int baseline = UnsafeBindings.b3GetByteCount();
            var go = new GameObject("Preview geometry");
            go.SetActive(false);
            Mesh mesh = null;
            TerrainData data = null;
            World first = World.Create(WorldDef.Default);
            World second = World.Create(WorldDef.Default);
            DetachedShape a = null;
            DetachedShape b = null;
            try
            {
                Box3DShape component;
                if (terrain)
                {
                    data = new TerrainData { heightmapResolution = 33, size = new Vector3(4f, 1f, 4f) };
                    Terrain source = go.AddComponent<Terrain>();
                    source.terrainData = data;
                    var field = go.AddComponent<Box3DTerrainShape>();
                    field.SetTerrain(source);
                    component = field;
                }
                else
                {
                    mesh = new Mesh
                    {
                        vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 4f), new Vector3(4f, 0f, 0f) },
                        triangles = new[] { 0, 1, 2 },
                    };
                    var meshShape = go.AddComponent<Box3DMeshShape>();
                    meshShape.SetMesh(mesh);
                    component = meshShape;
                }
                a = component.CreateDetachedShape(first.CreateBody(BodyDef.Default));
                b = component.CreateDetachedShape(second.CreateBody(BodyDef.Default));
                Assert.IsTrue(a.Shape.IsValid);
                Assert.IsTrue(b.Shape.IsValid);
                int beforeDispose = UnsafeBindings.b3GetByteCount();
                first.Destroy();
                a.Dispose();
                // Disposing geometry must not depend on the source component still being alive.
                Object.DestroyImmediate(go);
                Assert.IsTrue(b.Shape.IsValid);
                second.CastRayClosest(new float3(0.5f, 2f, 0.5f), new float3(0f, -4f, 0f), QueryFilter.Default, out RayResult hit);
                Assert.IsTrue(hit.Hit);
                Assert.Less(UnsafeBindings.b3GetByteCount(), beforeDispose);
                second.Step(1f / 60f);
            }
            finally
            {
                first.Destroy();
                second.Destroy();
                a?.Dispose();
                b?.Dispose();
                if (go) Object.DestroyImmediate(go);
                if (mesh) Object.DestroyImmediate(mesh);
                if (data) Object.DestroyImmediate(data);
            }
            Assert.AreEqual(baseline, UnsafeBindings.b3GetByteCount());
        }
    }
}
