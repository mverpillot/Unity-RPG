using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(RequireTagAttribute))]
public class RequireTagDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RequireTagAttribute requireTag = (RequireTagAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);
        property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(GameObject), true);

        if (property.objectReferenceValue != null)
        {
            GameObject go = property.objectReferenceValue as GameObject;
            if (go != null && go.tag != requireTag.RequiredTag)
            {
                EditorGUI.HelpBox(position, $"GameObject must have tag '{requireTag.RequiredTag}'", MessageType.Error);
                property.objectReferenceValue = null;
            }
        }

        EditorGUI.EndProperty();
    }
}
