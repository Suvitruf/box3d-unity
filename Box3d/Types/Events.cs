using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    // Mirrors of the event structs in box3d types.h. Event arrays live in transient engine memory,
    // valid only until the next Step or world mutation — consume immediately, never store spans.
    // Shape/contact ids inside end-touch events may already be stale (validate before use).

    /// <summary>Mirrors native b3BodyMoveEvent (48 bytes). Delivered for every body that moved
    /// during the last step; the efficient way to sync Unity transforms.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyMoveEvent
    {
        public IntPtr UserData;
        public B3Transform Transform;
        public BodyId BodyId;
        public NativeBool FellAsleep;
    }

    /// <summary>Mirrors native b3ContactBeginTouchEvent (28 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactBeginTouchEvent
    {
        public ShapeId ShapeIdA;
        public ShapeId ShapeIdB;
        public ContactId ContactId;
    }

    /// <summary>Mirrors native b3ContactEndTouchEvent (28 bytes). The shapes/contact may already be
    /// destroyed — validate ids before use.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactEndTouchEvent
    {
        public ShapeId ShapeIdA;
        public ShapeId ShapeIdB;
        public ContactId ContactId;
    }

    /// <summary>Mirrors native b3ContactHitEvent (72 bytes). Fired when impact speed exceeds
    /// the world's hit event threshold and the shape opted in.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactHitEvent
    {
        public ShapeId ShapeIdA;
        public ShapeId ShapeIdB;
        public ContactId ContactId;
        public float3 Point;
        public float3 Normal;
        public float ApproachSpeed;
        public ulong UserMaterialIdA;
        public ulong UserMaterialIdB;
    }

    /// <summary>Mirrors native b3SensorBeginTouchEvent (16 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorBeginTouchEvent
    {
        public ShapeId SensorShapeId;
        public ShapeId VisitorShapeId;
    }

    /// <summary>Mirrors native b3SensorEndTouchEvent (16 bytes). The shapes may already be
    /// destroyed — validate ids before use.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SensorEndTouchEvent
    {
        public ShapeId SensorShapeId;
        public ShapeId VisitorShapeId;
    }

    /// <summary>Mirrors native b3JointEvent (16 bytes). Reported when an awake joint's force/torque
    /// exceeds its threshold.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JointEvent
    {
        public JointId JointId;
        public IntPtr UserData;
    }

    // Raw pointer+count containers returned by the native getters (transient memory).

    /// <summary>Mirrors native b3BodyEvents (16 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct BodyEventsRaw
    {
        public IntPtr MoveEvents;
        public int MoveCount;
    }

    /// <summary>Mirrors native b3ContactEvents (40 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContactEventsRaw
    {
        public IntPtr BeginEvents;
        public IntPtr EndEvents;
        public IntPtr HitEvents;
        public int BeginCount;
        public int EndCount;
        public int HitCount;
    }

    /// <summary>Mirrors native b3SensorEvents (24 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SensorEventsRaw
    {
        public IntPtr BeginEvents;
        public IntPtr EndEvents;
        public int BeginCount;
        public int EndCount;
    }

    /// <summary>Mirrors native b3JointEvents (16 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JointEventsRaw
    {
        public IntPtr JointEvents;
        public int Count;
    }

    // Public span bundles handed out by World. Ref structs: cannot be stored on the heap, which
    // enforces the consume-immediately rule at compile time.

    /// <summary>Contact events from the last step. Valid only until the next Step/world mutation.</summary>
    public readonly ref struct ContactEvents
    {
        public readonly ReadOnlySpan<ContactBeginTouchEvent> Begin;
        public readonly ReadOnlySpan<ContactEndTouchEvent> End;
        public readonly ReadOnlySpan<ContactHitEvent> Hit;

        internal ContactEvents(ReadOnlySpan<ContactBeginTouchEvent> begin,
            ReadOnlySpan<ContactEndTouchEvent> end, ReadOnlySpan<ContactHitEvent> hit)
        {
            Begin = begin;
            End = end;
            Hit = hit;
        }
    }

    /// <summary>Sensor events from the last step. Valid only until the next Step/world mutation.</summary>
    public readonly ref struct SensorEvents
    {
        public readonly ReadOnlySpan<SensorBeginTouchEvent> Begin;
        public readonly ReadOnlySpan<SensorEndTouchEvent> End;

        internal SensorEvents(ReadOnlySpan<SensorBeginTouchEvent> begin, ReadOnlySpan<SensorEndTouchEvent> end)
        {
            Begin = begin;
            End = end;
        }
    }
}
