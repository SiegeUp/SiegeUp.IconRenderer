using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameObjectsListAttribute))]
public class GameObjectsListDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        try
        {
            var prefabRefProp = property.FindPropertyRelative("prefabRef");
            if (prefabRefProp.objectReferenceValue is GameObject gameObject && gameObject)
                label.text = gameObject.name;
        }
        catch
        {
        }

        EditorGUI.PropertyField(position, property, label, property.isExpanded);
    }
}