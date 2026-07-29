using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Mathematics;
using UnityEngine;
using LineList = System.Collections.Generic.List<Unity.Mathematics.float3>;

namespace Box3D
{
    /// <summary>What <see cref="World.DrawDebug"/> visualizes.</summary>
    [Flags]
    public enum DebugDrawFlags
    {
        None = 0,
        Shapes = 1 << 0,
        Joints = 1 << 1,
        JointExtras = 1 << 2,
        Bounds = 1 << 3,
        Mass = 1 << 4,
        Contacts = 1 << 5,
        ContactNormals = 1 << 6,
        ContactForces = 1 << 7,
        FrictionForces = 1 << 8,
        Islands = 1 << 9,
        GraphColors = 1 << 10,
        Default = Shapes | Joints,
    }

    public unsafe partial struct World
    {
        /// <summary>Draws the world's debug visualization with Debug.DrawLine — visible in the
        /// Scene view, and in the Game view ONLY with the Gizmos toggle enabled. Call once per
        /// frame from Update. Sphere/capsule/hull shapes draw as wireframes (mesh/heightfield/
        /// compound are skipped — use <see cref="DebugDrawFlags.Bounds"/> for those; shape
        /// wireframes also require the world to use the default debug-shape callbacks). Text
        /// drawing is not supported.</summary>
        public void DrawDebug(DebugDrawFlags flags = DebugDrawFlags.Default, float drawRadius = 100f)
        {
            b3DebugDraw draw = UnsafeBindings.b3DefaultDebugDraw();
            // Only interpret userShape pointers we created ourselves (GCHandles). Worlds with
            // user-supplied debug-shape callbacks keep the engine's empty default drawer.
            if (DebugDrawBridge.IsBridgeOwned(Id))
            {
                draw.DrawShapeFcn = DebugDrawBridge.ShapePtr;
            }
            draw.DrawSegmentFcn = DebugDrawBridge.SegmentPtr;
            draw.DrawTransformFcn = DebugDrawBridge.TransformPtr;
            draw.DrawPointFcn = DebugDrawBridge.PointPtr;
            draw.DrawSphereFcn = DebugDrawBridge.SpherePtr;
            draw.DrawCapsuleFcn = DebugDrawBridge.CapsulePtr;
            draw.DrawBoundsFcn = DebugDrawBridge.BoundsPtr;
            draw.DrawBoxFcn = DebugDrawBridge.BoxPtr;
            draw.drawingBounds = new B3Aabb
            {
                LowerBound = new float3(-drawRadius, -drawRadius, -drawRadius),
                UpperBound = new float3(drawRadius, drawRadius, drawRadius),
            };
            draw.drawShapes = (flags & DebugDrawFlags.Shapes) != 0;
            draw.drawJoints = (flags & DebugDrawFlags.Joints) != 0;
            draw.drawJointExtras = (flags & DebugDrawFlags.JointExtras) != 0;
            draw.drawBounds = (flags & DebugDrawFlags.Bounds) != 0;
            draw.drawMass = (flags & DebugDrawFlags.Mass) != 0;
            draw.drawContacts = (flags & DebugDrawFlags.Contacts) != 0;
            draw.drawContactNormals = (flags & DebugDrawFlags.ContactNormals) != 0;
            draw.drawContactForces = (flags & DebugDrawFlags.ContactForces) != 0;
            draw.drawFrictionForces = (flags & DebugDrawFlags.FrictionForces) != 0;
            draw.drawIslands = (flags & DebugDrawFlags.Islands) != 0;
            draw.drawGraphColors = (flags & DebugDrawFlags.GraphColors) != 0;

            DebugDrawBridge.DrawCallCount = 0;
            UnsafeBindings.b3World_Draw(Id, &draw, ulong.MaxValue);
        }
    }

    /// <summary>Trampolines translating box3d debug-draw callbacks into Debug.DrawLine calls.
    /// Main thread only (b3World_Draw is called by the user). Shape interiors use box3d's
    /// debug-shape system: the engine asks us to create a "user shape" per shape (we build a
    /// local-space wireframe line list, handed back as a GCHandle), then passes it to the draw
    /// callback with the body transform. Every trampoline catches — a managed exception must
    /// never unwind through native frames.</summary>
    internal static unsafe class DebugDrawBridge
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void* CreateDebugShapeDelegate(b3DebugShape* debugShape, void* userContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DestroyDebugShapeDelegate(void* userShape, void* userContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate NativeBool DrawShapeDelegate(void* userShape, B3WorldTransform transform, uint color, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SegmentDelegate(B3Pos p1, B3Pos p2, uint color, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void TransformDelegate(B3WorldTransform transform, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PointDelegate(B3Pos p, float size, uint color, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SphereDelegate(B3Pos p, float radius, uint color, float alpha, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CapsuleDelegate(B3Pos p1, B3Pos p2, float radius, uint color, float alpha, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BoundsDelegate(B3Aabb aabb, uint color, void* context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BoxDelegate(float3 extents, B3WorldTransform transform, uint color, void* context);

        private static readonly CreateDebugShapeDelegate CreateShapeInstance = CreateDebugShape;
        private static readonly DestroyDebugShapeDelegate DestroyShapeInstance = DestroyDebugShape;
        private static readonly DrawShapeDelegate ShapeInstance = DrawShape;
        private static readonly SegmentDelegate SegmentInstance = DrawSegment;
        private static readonly TransformDelegate TransformInstance = DrawTransform;
        private static readonly PointDelegate PointInstance = DrawPoint;
        private static readonly SphereDelegate SphereInstance = DrawSphere;
        private static readonly CapsuleDelegate CapsuleInstance = DrawCapsule;
        private static readonly BoundsDelegate BoundsInstance = DrawBounds;
        private static readonly BoxDelegate BoxInstance = DrawBox;

        internal static readonly IntPtr CreateShapePtr = Marshal.GetFunctionPointerForDelegate(CreateShapeInstance);
        internal static readonly IntPtr DestroyShapePtr = Marshal.GetFunctionPointerForDelegate(DestroyShapeInstance);
        internal static readonly IntPtr ShapePtr = Marshal.GetFunctionPointerForDelegate(ShapeInstance);
        internal static readonly IntPtr SegmentPtr = Marshal.GetFunctionPointerForDelegate(SegmentInstance);
        internal static readonly IntPtr TransformPtr = Marshal.GetFunctionPointerForDelegate(TransformInstance);
        internal static readonly IntPtr PointPtr = Marshal.GetFunctionPointerForDelegate(PointInstance);
        internal static readonly IntPtr SpherePtr = Marshal.GetFunctionPointerForDelegate(SphereInstance);
        internal static readonly IntPtr CapsulePtr = Marshal.GetFunctionPointerForDelegate(CapsuleInstance);
        internal static readonly IntPtr BoundsPtr = Marshal.GetFunctionPointerForDelegate(BoundsInstance);
        internal static readonly IntPtr BoxPtr = Marshal.GetFunctionPointerForDelegate(BoxInstance);

        // Worlds whose debug-shape callbacks were wired by World.Create (vs user-supplied).
        private static readonly bool[] BridgeOwned = new bool[UnsafeBindings.B3_MAX_WORLDS + 1];

        /// <summary>Callbacks invoked by the last DrawDebug call — lets tests verify the bridge fires.</summary>
        internal static int DrawCallCount;

        internal static void SetBridgeOwned(WorldId id, bool owned)
        {
            BridgeOwned[id.Index1] = owned;
        }

        internal static bool IsBridgeOwned(WorldId id)
        {
            return BridgeOwned[id.Index1];
        }

        private static Color ToColor(uint hex)
        {
            return new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f);
        }

        private static void Line(float3 a, float3 b, Color color)
        {
            Debug.DrawLine(a, b, color);
        }

        // --- debug-shape system: local-space wireframes built once per shape, drawn per frame ---

        [MonoPInvokeCallback(typeof(CreateDebugShapeDelegate))]
        private static void* CreateDebugShape(b3DebugShape* debugShape, void* userContext)
        {
            try
            {
                float3[] lines = BuildWireframe(debugShape);
                if (lines == null) return null;
                return (void*)GCHandle.ToIntPtr(GCHandle.Alloc(lines));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        [MonoPInvokeCallback(typeof(DestroyDebugShapeDelegate))]
        private static void DestroyDebugShape(void* userShape, void* userContext)
        {
            try
            {
                if (userShape == null) return;
                GCHandle.FromIntPtr((IntPtr)userShape).Free();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(DrawShapeDelegate))]
        private static NativeBool DrawShape(void* userShape, B3WorldTransform worldTransform, uint color, void* context)
        {
            try
            {
                DrawCallCount++;
                B3Transform transform = worldTransform.ToB3Transform();
                var lines = (float3[])GCHandle.FromIntPtr((IntPtr)userShape).Target;
                Color unityColor = ToColor(color);
                for (int i = 0; i < lines.Length; i += 2)
                {
                    Line(transform.Position + math.mul(transform.Rotation, lines[i]),
                         transform.Position + math.mul(transform.Rotation, lines[i + 1]), unityColor);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            return true;
        }

        private static float3[] BuildWireframe(b3DebugShape* debugShape)
        {
            // Type check BEFORE allocating: native re-asks every draw while the result is null,
            // so skipped types (mesh/heightfield/compound) must not allocate per frame.
            ShapeType type = debugShape->type;
            if (type != ShapeType.Sphere && type != ShapeType.Capsule && type != ShapeType.Hull)
            {
                return null; // use DebugDrawFlags.Bounds to visualize these
            }

            var lines = new LineList(128);
            switch (type)
            {
                case ShapeType.Sphere:
                {
                    Sphere* sphere = debugShape->sphere;
                    AddCircle(lines, sphere->Center, sphere->Radius, new float3(1f, 0f, 0f), new float3(0f, 1f, 0f));
                    AddCircle(lines, sphere->Center, sphere->Radius, new float3(0f, 1f, 0f), new float3(0f, 0f, 1f));
                    AddCircle(lines, sphere->Center, sphere->Radius, new float3(0f, 0f, 1f), new float3(1f, 0f, 0f));
                    break;
                }
                case ShapeType.Capsule:
                {
                    Capsule* capsule = debugShape->capsule;
                    float3 axis = math.normalizesafe(capsule->Center2 - capsule->Center1, new float3(0f, 1f, 0f));
                    float3 side = math.normalizesafe(math.cross(axis, new float3(0.371f, 0.827f, 0.421f)), new float3(1f, 0f, 0f));
                    float3 forward = math.cross(axis, side);
                    float radius = capsule->Radius;
                    AddCircle(lines, capsule->Center1, radius, side, forward);
                    AddCircle(lines, capsule->Center2, radius, side, forward);
                    AddLine(lines, capsule->Center1 + side * radius, capsule->Center2 + side * radius);
                    AddLine(lines, capsule->Center1 - side * radius, capsule->Center2 - side * radius);
                    AddLine(lines, capsule->Center1 + forward * radius, capsule->Center2 + forward * radius);
                    AddLine(lines, capsule->Center1 - forward * radius, capsule->Center2 - forward * radius);
                    break;
                }
                default:
                {
                    // Walk the half-edge structure: one line per edge pair (skip the twin).
                    HullData* hull = debugShape->hull;
                    byte* basePtr = (byte*)hull;
                    float3* points = (float3*)(basePtr + hull->PointOffset);
                    byte* edges = basePtr + hull->EdgeOffset; // b3HullHalfEdge = {next, twin, origin, face} bytes
                    for (int i = 0; i < hull->EdgeCount; i++)
                    {
                        byte twin = edges[i * 4 + 1];
                        if (i >= twin) continue;
                        AddLine(lines, points[edges[i * 4 + 2]], points[edges[twin * 4 + 2]]);
                    }
                    break;
                }
            }
            return lines.ToArray();
        }

        private static void AddLine(LineList lines, float3 a, float3 b)
        {
            lines.Add(a);
            lines.Add(b);
        }

        private static void AddCircle(LineList lines, float3 center, float radius, float3 axisA, float3 axisB)
        {
            const int segments = 16;
            float3 previous = center + axisA * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (2f * math.PI / segments);
                float3 next = center + (axisA * math.cos(angle) + axisB * math.sin(angle)) * radius;
                AddLine(lines, previous, next);
                previous = next;
            }
        }

        // --- gizmo-level draw callbacks ---

        [MonoPInvokeCallback(typeof(SegmentDelegate))]
        private static void DrawSegment(B3Pos wp1, B3Pos wp2, uint color, void* context)
        {
            try
            {
                DrawCallCount++;
                Line((float3)wp1, (float3)wp2, ToColor(color));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(TransformDelegate))]
        private static void DrawTransform(B3WorldTransform worldTransform, void* context)
        {
            try
            {
                DrawCallCount++;
                B3Transform transform = worldTransform.ToB3Transform();
                const float axisLength = 0.3f;
                float3 p = transform.Position;
                Line(p, p + math.mul(transform.Rotation, new float3(axisLength, 0f, 0f)), Color.red);
                Line(p, p + math.mul(transform.Rotation, new float3(0f, axisLength, 0f)), Color.green);
                Line(p, p + math.mul(transform.Rotation, new float3(0f, 0f, axisLength)), Color.blue);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(PointDelegate))]
        private static void DrawPoint(B3Pos wp, float size, uint color, void* context)
        {
            try
            {
                DrawCallCount++;
                float3 p = (float3)wp;
                Color unityColor = ToColor(color);
                float h = size * 0.02f;
                Line(p - new float3(h, 0f, 0f), p + new float3(h, 0f, 0f), unityColor);
                Line(p - new float3(0f, h, 0f), p + new float3(0f, h, 0f), unityColor);
                Line(p - new float3(0f, 0f, h), p + new float3(0f, 0f, h), unityColor);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(SphereDelegate))]
        private static void DrawSphere(B3Pos wp, float radius, uint color, float alpha, void* context)
        {
            try
            {
                DrawCallCount++;
                float3 p = (float3)wp;
                Color unityColor = ToColor(color);
                DrawCircleLines(p, radius, new float3(1f, 0f, 0f), new float3(0f, 1f, 0f), unityColor);
                DrawCircleLines(p, radius, new float3(0f, 1f, 0f), new float3(0f, 0f, 1f), unityColor);
                DrawCircleLines(p, radius, new float3(0f, 0f, 1f), new float3(1f, 0f, 0f), unityColor);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(CapsuleDelegate))]
        private static void DrawCapsule(B3Pos wp1, B3Pos wp2, float radius, uint color, float alpha, void* context)
        {
            try
            {
                DrawCallCount++;
                float3 p1 = (float3)wp1;
                float3 p2 = (float3)wp2;
                Color unityColor = ToColor(color);
                float3 axis = math.normalizesafe(p2 - p1, new float3(0f, 1f, 0f));
                float3 side = math.normalizesafe(math.cross(axis, new float3(0.371f, 0.827f, 0.421f)), new float3(1f, 0f, 0f));
                float3 forward = math.cross(axis, side);

                DrawCircleLines(p1, radius, side, forward, unityColor);
                DrawCircleLines(p2, radius, side, forward, unityColor);
                Line(p1 + side * radius, p2 + side * radius, unityColor);
                Line(p1 - side * radius, p2 - side * radius, unityColor);
                Line(p1 + forward * radius, p2 + forward * radius, unityColor);
                Line(p1 - forward * radius, p2 - forward * radius, unityColor);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(BoundsDelegate))]
        private static void DrawBounds(B3Aabb aabb, uint color, void* context)
        {
            try
            {
                DrawCallCount++;
                DrawEdges(Corners(aabb), ToColor(color));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MonoPInvokeCallback(typeof(BoxDelegate))]
        private static void DrawBox(float3 extents, B3WorldTransform worldTransform, uint color, void* context)
        {
            try
            {
                DrawCallCount++;
                B3Transform transform = worldTransform.ToB3Transform();
                var corners = new float3[8];
                for (int i = 0; i < 8; i++)
                {
                    float3 local = new float3(
                        (i & 1) == 0 ? -extents.x : extents.x,
                        (i & 2) == 0 ? -extents.y : extents.y,
                        (i & 4) == 0 ? -extents.z : extents.z);
                    corners[i] = transform.Position + math.mul(transform.Rotation, local);
                }
                DrawEdges(corners, ToColor(color));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static float3[] Corners(B3Aabb aabb)
        {
            var corners = new float3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = new float3(
                    (i & 1) == 0 ? aabb.LowerBound.x : aabb.UpperBound.x,
                    (i & 2) == 0 ? aabb.LowerBound.y : aabb.UpperBound.y,
                    (i & 4) == 0 ? aabb.LowerBound.z : aabb.UpperBound.z);
            }
            return corners;
        }

        private static void DrawEdges(float3[] corners, Color color)
        {
            // Connect corners differing in exactly one bit (12 box edges).
            for (int i = 0; i < 8; i++)
            {
                for (int bit = 1; bit <= 4; bit <<= 1)
                {
                    int j = i | bit;
                    if (j != i) Line(corners[i], corners[j], color);
                }
            }
        }

        private static void DrawCircleLines(float3 center, float radius, float3 axisA, float3 axisB, Color color)
        {
            const int segments = 16;
            float3 previous = center + axisA * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (2f * math.PI / segments);
                float3 next = center + (axisA * math.cos(angle) + axisB * math.sin(angle)) * radius;
                Line(previous, next, color);
                previous = next;
            }
        }
    }
}
