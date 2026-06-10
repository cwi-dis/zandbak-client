using System.Linq;
using Orchestrator.ScriptableObjects;
using Orchestrator.Attributes;
using UnityEditor;
using UnityEngine;

namespace Orchestrator.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PrefabNameSelectionAttribute))]
    public class PrefabNameSelectionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = (PrefabNameSelectionAttribute)attribute;
            var registryProperty = property.serializedObject.FindProperty(attr.RegistryFieldName);

            if (registryProperty == null || registryProperty.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var registry = registryProperty.objectReferenceValue as SharedObjectPrefabRegistry;
            if (registry == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var names = registry.registryEntries.Select(e => e.prefabName).ToArray();
            if (names.Length == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            int currentIndex = System.Array.IndexOf(names, property.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, names);
            property.stringValue = names[newIndex];
        }
    }
}
