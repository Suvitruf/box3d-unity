using System;
using System.Text;

namespace Box3d
{
    /// <summary>A capture of a world's simulation for deterministic replay and validation. Record with
    /// <see cref="World.StartRecording"/> / <see cref="World.StopRecording"/>, then
    /// <see cref="ValidateReplay"/> to confirm the sim reproduces bit-identical state (the core check
    /// for lockstep/rollback netcode), <see cref="SaveToFile"/> it, or play it back with a ReplayPlayer.
    ///
    /// <para>Owns native memory — call <see cref="Destroy"/> when done. The bytes from
    /// <see cref="GetData"/> are valid only until then.</para></summary>
    public struct Recording
    {
        private IntPtr _handle; // b3Recording*

        /// <summary>Whether this wraps a live native recording.</summary>
        public bool IsCreated => _handle != IntPtr.Zero;

        /// <summary>Allocates an empty recording. <paramref name="byteCapacity"/> caps the buffer — it
        /// won't grow past it, so size it for the run length (a few MB per few thousand steps).</summary>
        public static unsafe Recording Create(int byteCapacity = 8 << 20)
        {
            return new Recording { _handle = (IntPtr)UnsafeBindings.b3CreateRecording(byteCapacity) };
        }

        /// <summary>Loads a recording previously written with <see cref="SaveToFile"/>.</summary>
        public static unsafe Recording LoadFromFile(string path)
        {
            byte[] p = NullTerminated(path);
            fixed (byte* pp = p)
            {
                return new Recording { _handle = (IntPtr)UnsafeBindings.b3LoadRecordingFromFile((sbyte*)pp) };
            }
        }

        /// <summary>Frees the native recording.</summary>
        public unsafe void Destroy()
        {
            if (_handle == IntPtr.Zero) return;
            UnsafeBindings.b3DestroyRecording((b3Recording*)_handle);
            _handle = IntPtr.Zero;
        }

        /// <summary>Serialized size in bytes.</summary>
        public unsafe int Size => _handle != IntPtr.Zero ? UnsafeBindings.b3Recording_GetSize((b3Recording*)_handle) : 0;

        /// <summary>The raw serialized bytes — valid only until <see cref="Destroy"/>. Copy them if you
        /// need to keep them.</summary>
        public unsafe ReadOnlySpan<byte> GetData()
        {
            if (_handle == IntPtr.Zero) return default;
            return new ReadOnlySpan<byte>(UnsafeBindings.b3Recording_GetData((b3Recording*)_handle), Size);
        }

        /// <summary>Writes the recording to a file.</summary>
        public unsafe bool SaveToFile(string path)
        {
            if (_handle == IntPtr.Zero) return false;
            byte[] p = NullTerminated(path);
            fixed (byte* pp = p)
            {
                return UnsafeBindings.b3SaveRecordingToFile((b3Recording*)_handle, (sbyte*)pp);
            }
        }

        /// <summary>Replays the recording and verifies every embedded state hash reproduces identically —
        /// box3d's determinism check. Pass a different <paramref name="workerCount"/> than it was recorded
        /// at to confirm cross-thread determinism. Returns true if the simulation is deterministic.</summary>
        public unsafe bool ValidateReplay(int workerCount = 1)
        {
            if (_handle == IntPtr.Zero) return false;
            byte* data = UnsafeBindings.b3Recording_GetData((b3Recording*)_handle);
            return UnsafeBindings.b3ValidateReplay(data, Size, workerCount);
        }

        // The native handle, for World.StartRecording.
        internal IntPtr Handle => _handle;

        private static byte[] NullTerminated(string s)
        {
            int n = Encoding.UTF8.GetByteCount(s);
            var buf = new byte[n + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
            return buf; // trailing 0 already present (buffer is n+1)
        }
    }
}
