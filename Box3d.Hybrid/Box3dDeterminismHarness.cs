using System.Text;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Runs the fixed <see cref="DeterminismScenario"/> and shows its state-hash signature
    /// on-screen — a turnkey way to check cross-platform determinism. Put it in an otherwise empty scene,
    /// build to the **Editor**, **Android**, and **WebGL**, run each, and compare the hashes shown. Matching
    /// hashes mean the physics reproduced bit-for-bit across those platforms; a mismatch (and the first
    /// checkpoint that differs) is single-precision float divergence between the native builds.
    ///
    /// <para>The signature is also logged (Debug.Log) so you can read it via <c>adb logcat</c> on Android
    /// or the browser console on WebGL, and can be copied to the clipboard.</para></summary>
    [AddComponentMenu("Box3d/Box3d Determinism Harness")]
    public class Box3dDeterminismHarness : MonoBehaviour
    {
#if ENABLE_IL2CPP
        private const string Backend = "IL2CPP";
#else
        private const string Backend = "Mono";
#endif

        private uint[] _hashes;
        private string _signature;
        private GUIStyle _style;

        /// <summary>The per-step hash stream from the last run.</summary>
        public uint[] Hashes => _hashes;

        /// <summary>The final world-state hash (0 until it has run).</summary>
        public uint Final => _hashes != null && _hashes.Length > 0 ? _hashes[_hashes.Length - 1] : 0u;

        /// <summary>The full one-line signature (platform + backend + checkpoints + final).</summary>
        public string Signature => _signature;

        private void Start() => Run();

        /// <summary>Runs the scenario and rebuilds the signature.</summary>
        public void Run()
        {
            _hashes = DeterminismScenario.Run();
            _signature = BuildSignature();
            Debug.Log(_signature, this);
        }

        private uint Checkpoint(int percent)
        {
            if (_hashes == null || _hashes.Length == 0) return 0u;
            int index = Mathf.Clamp(_hashes.Length * percent / 100 - 1, 0, _hashes.Length - 1);
            return _hashes[index];
        }

        private string BuildSignature()
        {
            var sb = new StringBuilder();
            sb.Append("[Box3dDeterminism] ");
            sb.Append($"platform={Application.platform} backend={Backend} ");
            sb.Append($"steps={DeterminismScenario.StepCount} | ");
            sb.Append($"25%=0x{Checkpoint(25):X8} 50%=0x{Checkpoint(50):X8} ");
            sb.Append($"75%=0x{Checkpoint(75):X8} final=0x{Final:X8}");
            return sb.ToString();
        }

        private void OnGUI()
        {
            if (_hashes == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Max(14, Screen.height / 36),
                    wordWrap = true,
                };
            }

            float margin = _style.fontSize;
            var area = new Rect(margin, margin, Screen.width - 2f * margin, Screen.height - 2f * margin);
            GUILayout.BeginArea(area);

            GUILayout.Label("box3d determinism harness", _style);
            GUILayout.Space(margin);
            GUILayout.Label($"platform : {Application.platform}", _style);
            GUILayout.Label($"backend  : {Backend}", _style);
            GUILayout.Label($"steps    : {DeterminismScenario.StepCount}", _style);
            GUILayout.Space(margin * 0.5f);
            GUILayout.Label($"25%   0x{Checkpoint(25):X8}", _style);
            GUILayout.Label($"50%   0x{Checkpoint(50):X8}", _style);
            GUILayout.Label($"75%   0x{Checkpoint(75):X8}", _style);
            GUILayout.Label($"FINAL 0x{Final:X8}", _style);
            GUILayout.Space(margin);

            if (GUILayout.Button("Run again", _style, GUILayout.Height(_style.fontSize * 2.5f))) Run();
            if (GUILayout.Button("Copy signature", _style, GUILayout.Height(_style.fontSize * 2.5f)))
            {
                GUIUtility.systemCopyBuffer = _signature;
            }

            GUILayout.EndArea();
        }
    }
}
