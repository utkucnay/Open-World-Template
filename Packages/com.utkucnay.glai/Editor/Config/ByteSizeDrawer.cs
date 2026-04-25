using Glai.Core;
using UnityEditor;
using UnityEngine;

namespace Glai.Config.Editor
{
    [CustomPropertyDrawer(typeof(ByteSize))]
    public class ByteSizeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative(nameof(ByteSize.Value));
            SerializedProperty unitProperty = property.FindPropertyRelative(nameof(ByteSize.Unit));

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            float unitWidth = 54f;
            float spacing = 4f;
            Rect valueRect = new Rect(contentRect.x, contentRect.y, contentRect.width - unitWidth - spacing, contentRect.height);
            Rect unitRect = new Rect(valueRect.xMax + spacing, contentRect.y, unitWidth, contentRect.height);

            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
            EditorGUI.PropertyField(unitRect, unitProperty, GUIContent.none);
        }
    }
}
