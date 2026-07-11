using Box3d;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>An on-screen physics stats overlay for the active <see cref="Box3dWorld"/> — step time,
    /// per-phase profile breakdown, and live body/contact/island counts, straight from box3d's own
    /// <c>GetProfile</c>/<c>GetCounters</c>. Drop it on any GameObject in a scene that has a Box3dWorld.
    /// The classic Box2D-testbed HUD: the fastest way to see what the simulation is doing and where the
    /// step time goes. Toggle with F3 (Input System) or the Visible field.</summary>
    [AddComponentMenu("Box3d/Box3d Stats HUD")]
    public class Box3dStatsHud : MonoBehaviour
    {
        private enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

        [SerializeField, Tooltip("Show the overlay. Call Toggle() from your own input to bind a key.")]
        private bool Visible = true;

        [SerializeField, Tooltip("Show the per-phase step-time breakdown (broadphase / narrowphase / solve / …).")]
        private bool ShowProfile = true;

        [SerializeField, Tooltip("Show live body / shape / contact / joint / island counts and memory.")]
        private bool ShowCounters = true;

        [SerializeField]
        private Corner Anchor = Corner.TopLeft;

        [SerializeField, Range(10, 28)]
        private int FontSize = 14;

        private Box3dWorld _world;
        private Profile _profile;
        private Counters _counters;
        private int _awakeBodies;
        private float _stepMs;
        private float _fps;
        private GUIStyle _style;
        private Texture2D _backdrop;

        private void LateUpdate()
        {
            if (!_world) _world = Box3dWorld.Instance;
            World world = _world ? _world.World : default;
            if (!world.IsValid) return;

            _profile = world.GetProfile();
            _counters = world.GetCounters();
            _awakeBodies = world.GetAwakeBodyCount();

            // Smooth the noisy per-step values so they're readable.
            _stepMs = Mathf.Lerp(_stepMs, _profile.Step, 0.1f);
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.1f);
        }

        /// <summary>Toggles the overlay — bind it to a key from your own input handling.</summary>
        public void Toggle() => Visible = !Visible;

        private void OnGUI()
        {
            if (!Visible || !_world) return;

            EnsureStyle();
            string text = BuildText();
            Vector2 size = _style.CalcSize(new GUIContent(text));
            var rect = new Rect(PlaceX(size.x), PlaceY(size.y), size.x, size.y);

            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(rect, _backdrop);
            GUI.color = prev;
            GUI.Label(rect, text, _style);
        }

        private string BuildText()
        {
            var sb = new System.Text.StringBuilder(512);
            sb.Append($"<b>box3d</b>  {_fps:F0} FPS   <b>step {_stepMs:F2} ms</b>\n");
            sb.Append($"awake {_awakeBodies}/{_counters.BodyCount} bodies\n");

            if (ShowCounters)
            {
                sb.Append($"\nshapes {_counters.ShapeCount}   contacts {_counters.ContactCount} " +
                          $"(awake {_counters.AwakeContactCount})\n");
                sb.Append($"joints {_counters.JointCount}   islands {_counters.IslandCount}   " +
                          $"tree h {_counters.TreeHeight}/{_counters.StaticTreeHeight}\n");
                sb.Append($"mem {FormatBytes(_counters.ByteCount)}   stack {FormatBytes(_counters.StackUsed)}\n");
            }

            if (ShowProfile)
            {
                sb.Append("\n<b>step breakdown (ms)</b>\n");
                Row(sb, "broadphase", _profile.Pairs + _profile.Refit);
                Row(sb, "narrowphase", _profile.Collide);
                Row(sb, "solve", _profile.Solve);
                Row(sb, "continuous", _profile.Bullets);
                Row(sb, "sleep", _profile.SleepIslands);
                Row(sb, "sensors", _profile.Sensors + _profile.SensorHits);
                Row(sb, "events", _profile.JointEvents + _profile.HitEvents);
            }

            return sb.ToString();
        }

        private static void Row(System.Text.StringBuilder sb, string name, float ms)
        {
            sb.Append($"  {name,-12} {ms,6:F2}\n");
        }

        private static string FormatBytes(int bytes)
        {
            if (bytes >= 1 << 20) return $"{bytes / (float)(1 << 20):F1} MB";
            if (bytes >= 1 << 10) return $"{bytes / (float)(1 << 10):F1} KB";
            return $"{bytes} B";
        }

        private void EnsureStyle()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(10, 12, 8, 10),
                    normal = { textColor = Color.white },
                };
                // Monospace so the columns line up.
                _style.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Menlo", "Courier New", "monospace" }, FontSize);
            }
            _style.fontSize = FontSize;
            if (_backdrop == null)
            {
                _backdrop = Texture2D.whiteTexture;
            }
        }

        private float PlaceX(float w) => Anchor == Corner.TopRight || Anchor == Corner.BottomRight
            ? Screen.width - w - 8f : 8f;

        private float PlaceY(float h) => Anchor == Corner.BottomLeft || Anchor == Corner.BottomRight
            ? Screen.height - h - 8f : 8f;
    }
}
