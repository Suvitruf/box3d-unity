using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Per-target SerializedObjects for Scene-view handles. With
    /// <see cref="CanEditMultipleObjects"/> Unity runs OnSceneGUI once per selected object, with
    /// <c>target</c> set to that object, but <c>Editor.serializedObject</c> spans the whole
    /// selection — a handle written through it would stamp one object's value onto every selected
    /// one. Handles read and write the current target's own SerializedObject from here instead.
    /// Dispose it in the editor's OnDisable.</summary>
    internal sealed class TargetSerializedObjects : IDisposable
    {
        private sealed class Entry
        {
            public SerializedObject Object;
            public readonly Dictionary<string, SerializedProperty> Properties = new Dictionary<string, SerializedProperty>();
        }

        private readonly Dictionary<Object, Entry> _entries = new Dictionary<Object, Entry>();

        /// <summary>The target's SerializedObject, refreshed from the object. Call it once per
        /// OnSceneGUI before reading properties and apply handle edits through it.</summary>
        public SerializedObject Update(Object target)
        {
            Entry entry = Get(target);
            entry.Object.Update();
            return entry.Object;
        }

        /// <summary>A property of the target's SerializedObject, cached: OnSceneGUI runs on every
        /// scene-view event and FindProperty re-walks the property path each call.</summary>
        public SerializedProperty Property(Object target, string name)
        {
            Entry entry = Get(target);
            if (!entry.Properties.TryGetValue(name, out SerializedProperty property))
            {
                property = entry.Object.FindProperty(name);
                entry.Properties[name] = property;
            }
            return property;
        }

        public void Dispose()
        {
            foreach (Entry entry in _entries.Values) entry.Object.Dispose();
            _entries.Clear();
        }

        private Entry Get(Object target)
        {
            if (!_entries.TryGetValue(target, out Entry entry))
            {
                entry = new Entry { Object = new SerializedObject(target) };
                _entries[target] = entry;
            }
            return entry;
        }
    }
}
