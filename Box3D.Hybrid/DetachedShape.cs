#if UNITY_EDITOR
using System;

namespace Box3D.Hybrid
{
    /// <summary>Owns one preview shape and its referenced geometry, independently of the scene component.</summary>
    internal sealed class DetachedShape : IDisposable
    {
        internal Shape Shape;
        internal TriangleMesh Mesh;
        internal HeightField Field;

        internal DetachedShape(Shape shape) => Shape = shape;

        public void Dispose()
        {
            Shape.Destroy();
            Mesh.Destroy();
            Field.Destroy();
        }
    }
}
#endif
