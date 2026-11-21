using UnityEngine;

namespace Box3D.Hybrid
{
    // Shared by shape components and ropes. Cache the GameObject once and only touch native
    // shape filters when the Unity layer/mask changes or synchronization is first enabled.
    internal struct CollisionLayerState
    {
        private readonly GameObject _gameObject;
        private int _layer;
        private ulong _mask;

        internal CollisionLayerState(GameObject gameObject)
        {
            _gameObject = gameObject;
            _layer = -1;
            _mask = 0;
        }

        internal bool Refresh(ulong[] layerMasks, bool force)
        {
            int layer = _gameObject.layer;
            ulong mask = layerMasks[layer];
            if (!force && layer == _layer && mask == _mask) return false;
            _layer = layer;
            _mask = mask;
            return true;
        }

        internal void Apply(Shape shape)
        {
            if (!shape.IsValid) return;
            CollisionFilter filter = shape.GetFilter();
            ulong category = 1UL << _layer;
            if (filter.CategoryBits == category && filter.MaskBits == _mask) return;
            filter.CategoryBits = category;
            filter.MaskBits = _mask;
            shape.SetFilter(filter, invokeContacts: true);
        }
    }
}
