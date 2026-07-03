using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Box3d
{
    /// <summary>Installs Box3d's global log/assert hooks so engine diagnostics reach the Unity
    /// console. Runs automatically on play; call <see cref="Install"/> manually from edit-mode
    /// code (tests, tools) if needed.</summary>
    public static class Box3dRuntime
    {
        private delegate void LogDelegate(IntPtr message);
        private delegate int AssertDelegate(IntPtr condition, IntPtr fileName, int lineNumber);

        // Keep delegates rooted for the lifetime of the process — the native side stores the
        // function pointers.
        private static readonly LogDelegate LogHandler = OnLog;
        private static readonly AssertDelegate AssertHandler = OnAssert;

        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            if (Box3dApi.IsDoublePrecision)
            {
                Debug.LogError("[Box3d] Native library was built with BOX3D_DOUBLE_PRECISION — " +
                               "this wrapper requires a single-precision build. Struct layouts will not match.");
            }

            UnsafeBindings.b3SetLogFcn(Marshal.GetFunctionPointerForDelegate(LogHandler));
            UnsafeBindings.b3SetAssertFcn(Marshal.GetFunctionPointerForDelegate(AssertHandler));
        }

        [MonoPInvokeCallback(typeof(LogDelegate))]
        private static void OnLog(IntPtr message)
        {
            Debug.Log($"[Box3d] {Marshal.PtrToStringAnsi(message)}");
        }

        [MonoPInvokeCallback(typeof(AssertDelegate))]
        private static int OnAssert(IntPtr condition, IntPtr fileName, int lineNumber)
        {
            Debug.LogError($"[Box3d] Assertion failed: {Marshal.PtrToStringAnsi(condition)} " +
                           $"at {Marshal.PtrToStringAnsi(fileName)}:{lineNumber}");
            return 0;
        }
    }
}
