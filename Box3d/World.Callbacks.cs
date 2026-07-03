using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Mathematics;
using UnityEngine;

namespace Box3d
{
    /// <summary>Decides if two shapes should collide. Both shapes have EnableCustomFiltering.
    /// THREAD SAFETY: invoked from worker threads during Step when the world is multithreaded —
    /// must be pure: no Unity API, no world mutation, no allocation.</summary>
    public delegate bool CustomFilterCallback(Shape shapeA, Shape shapeB);

    /// <summary>Inspects a contact before solving; return false to disable it this step. The shape
    /// has EnablePreSolveEvents. THREAD SAFETY: same rules as <see cref="CustomFilterCallback"/>.</summary>
    public delegate bool PreSolveCallback(Shape shapeA, Shape shapeB, float3 point, float3 normal);

    /// <summary>Mixes a material property (friction or restitution) of two touching shapes.
    /// THREAD SAFETY: same rules as <see cref="CustomFilterCallback"/>.</summary>
    public delegate float MaterialMixCallback(float valueA, ulong userMaterialIdA, float valueB, ulong userMaterialIdB);

    public unsafe partial struct World
    {
        // Filter/pre-solve callbacks are per-world: the native context pointer carries the world
        // index, and the managed delegate lives in a slot table. The material mixers get NO
        // context from box3d, so the wrapper can only hold one GLOBAL managed mixer of each kind —
        // setting one on any world replaces it for all worlds that registered one.

        private static readonly CustomFilterCallback[] CustomFilters = new CustomFilterCallback[UnsafeBindings.B3_MAX_WORLDS + 1];
        private static readonly PreSolveCallback[] PreSolves = new PreSolveCallback[UnsafeBindings.B3_MAX_WORLDS + 1];
        private static MaterialMixCallback _frictionMix;
        private static MaterialMixCallback _restitutionMix;

        private static readonly b3CustomFilterFcn CustomFilterDelegate = OnCustomFilter;
        private static readonly IntPtr CustomFilterPtr = Marshal.GetFunctionPointerForDelegate(CustomFilterDelegate);
        private static readonly b3PreSolveFcn PreSolveDelegate = OnPreSolve;
        private static readonly IntPtr PreSolvePtr = Marshal.GetFunctionPointerForDelegate(PreSolveDelegate);
        private static readonly b3FrictionCallback FrictionDelegate = OnFrictionMix;
        private static readonly IntPtr FrictionPtr = Marshal.GetFunctionPointerForDelegate(FrictionDelegate);
        private static readonly b3RestitutionCallback RestitutionDelegate = OnRestitutionMix;
        private static readonly IntPtr RestitutionPtr = Marshal.GetFunctionPointerForDelegate(RestitutionDelegate);

        [MonoPInvokeCallback(typeof(b3CustomFilterFcn))]
        private static NativeBool OnCustomFilter(ShapeId shapeIdA, ShapeId shapeIdB, void* context)
        {
            try
            {
                CustomFilterCallback callback = CustomFilters[(uint)context];
                if (callback == null) return true;
                return callback(new Shape { Id = shapeIdA }, new Shape { Id = shapeIdB });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return true; // default: collide
            }
        }

        [MonoPInvokeCallback(typeof(b3PreSolveFcn))]
        private static NativeBool OnPreSolve(ShapeId shapeIdA, ShapeId shapeIdB, float3 point, float3 normal, void* context)
        {
            try
            {
                PreSolveCallback callback = PreSolves[(uint)context];
                if (callback == null) return true;
                return callback(new Shape { Id = shapeIdA }, new Shape { Id = shapeIdB }, point, normal);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return true; // default: keep the contact
            }
        }

        [MonoPInvokeCallback(typeof(b3FrictionCallback))]
        private static float OnFrictionMix(float frictionA, ulong userMaterialIdA, float frictionB, ulong userMaterialIdB)
        {
            try
            {
                MaterialMixCallback callback = _frictionMix;
                return callback != null
                    ? callback(frictionA, userMaterialIdA, frictionB, userMaterialIdB)
                    : math.sqrt(frictionA * frictionB); // engine default
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return math.sqrt(frictionA * frictionB);
            }
        }

        [MonoPInvokeCallback(typeof(b3RestitutionCallback))]
        private static float OnRestitutionMix(float restitutionA, ulong userMaterialIdA, float restitutionB, ulong userMaterialIdB)
        {
            try
            {
                MaterialMixCallback callback = _restitutionMix;
                return callback != null
                    ? callback(restitutionA, userMaterialIdA, restitutionB, userMaterialIdB)
                    : math.max(restitutionA, restitutionB); // engine default
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return math.max(restitutionA, restitutionB);
            }
        }

        /// <summary>Registers a collision filter for shape pairs that enable custom filtering
        /// (<see cref="ShapeDef.EnableCustomFiltering"/>). Pass null to clear. See the delegate's
        /// thread-safety rules.</summary>
        public void SetCustomFilterCallback(CustomFilterCallback callback)
        {
            CustomFilters[Id.Index1] = callback;
            UnsafeBindings.b3World_SetCustomFilterCallback(Id,
                callback != null ? CustomFilterPtr : IntPtr.Zero, (void*)(uint)Id.Index1);
        }

        /// <summary>Registers a pre-solve inspector for shapes that enable pre-solve events
        /// (<see cref="ShapeDef.EnablePreSolveEvents"/>). Pass null to clear. See the delegate's
        /// thread-safety rules.</summary>
        public void SetPreSolveCallback(PreSolveCallback callback)
        {
            PreSolves[Id.Index1] = callback;
            UnsafeBindings.b3World_SetPreSolveCallback(Id,
                callback != null ? PreSolvePtr : IntPtr.Zero, (void*)(uint)Id.Index1);
        }

        /// <summary>Registers a friction mixing function (default: sqrt(a·b)). GLOBAL managed
        /// callback — the native API passes no context, so the last registered mixer serves every
        /// world. Pass null to restore the engine default.</summary>
        public void SetFrictionCallback(MaterialMixCallback callback)
        {
            _frictionMix = callback;
            UnsafeBindings.b3World_SetFrictionCallback(Id, callback != null ? FrictionPtr : IntPtr.Zero);
        }

        /// <summary>Registers a restitution mixing function (default: max(a, b)). GLOBAL managed
        /// callback — the native API passes no context, so the last registered mixer serves every
        /// world. Pass null to restore the engine default.</summary>
        public void SetRestitutionCallback(MaterialMixCallback callback)
        {
            _restitutionMix = callback;
            UnsafeBindings.b3World_SetRestitutionCallback(Id, callback != null ? RestitutionPtr : IntPtr.Zero);
        }

        internal void ClearCallbackSlots()
        {
            CustomFilters[Id.Index1] = null;
            PreSolves[Id.Index1] = null;
        }
    }
}
