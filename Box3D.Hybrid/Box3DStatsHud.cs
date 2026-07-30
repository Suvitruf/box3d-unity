using Box3D;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>An on-screen physics stats overlay for the active <see cref="Box3DWorld"/> — step time,
    /// per-phase profile breakdown, and live body/contact/island counts, straight from box3d's own
    /// <c>GetProfile</c>/<c>GetCounters</c>. Drop it on any GameObject in a scene that has a Box3DWorld.
    /// The classic Box2D-testbed HUD: the fastest way to see what the simulation is doing and where the
    /// step time goes. Toggle with F3 (Input System) or the Visible field.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DStatsHud.png")]
    [AddComponentMenu("Box3D/Diagnostics/Stats HUD")]
    public class Box3DStatsHud : MonoBehaviour
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

        private Box3DWorld _world;
        private bool _worldSearched;
        private Profile _profile;
        private Counters _counters;
        private int _awakeBodies;
        private float _stepMs;
        private float _fps;
        private GUIStyle _style;
        private Texture2D _backdrop;
        // Built once per frame here; OnGUI runs (at least) twice per frame and would otherwise
        // rebuild the string and a GUIContent per pass.
        private readonly GUIContent _content = new GUIContent();

        private void LateUpdate()
        {
            // One-shot acquisition: Box3DWorld.Instance scene-searches and AUTO-CREATES a world
            // when none exists — retrying every frame would make a diagnostics overlay respawn
            // physics after the world it was watching died.
            if (!_world)
            {
                if (_worldSearched) return;
                _worldSearched = true;
                _world = Box3DWorld.Instance;
            }
            World world = _world ? _world.World : default;
            if (!world.IsValid) return;

            _profile = world.GetProfile();
            _counters = world.GetCounters();
            _awakeBodies = world.GetAwakeBodyCount();

            // Smooth the noisy per-step values so they're readable.
            _stepMs = Mathf.Lerp(_stepMs, _profile.Step, 0.1f);
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.1f);

            if (Visible) _content.text = BuildText();
        }

        /// <summary>Toggles the overlay — bind it to a key from your own input handling.</summary>
        public void Toggle() => Visible = !Visible;

        private void OnGUI()
        {
            if (!Visible || !_world || string.IsNullOrEmpty(_content.text)) return;

            EnsureStyle();
            Vector2 size = _style.CalcSize(_content);
            var rect = new Rect(PlaceX(size.x), PlaceY(size.y), size.x, size.y);

            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(rect, _backdrop);
            GUI.color = prev;
            GUI.Label(rect, _content, _style);
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
