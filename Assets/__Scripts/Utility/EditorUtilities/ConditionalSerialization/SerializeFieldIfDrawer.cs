

using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace MyEditorUtilities
{
    [CustomPropertyDrawer(typeof(IfAttribute))]
    [CustomPropertyDrawer(typeof(SerializeFieldIfAttribute))]
    public class SerializeFieldIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ShouldBeSerialized(property)) { return; }

            EditorGUI.PropertyField(position, property, label, true);
        }

        /// <summary>
        /// Necessary when there are other fields to serialize because otherwise it would skip the space need for the serializeation of this variable before serializing the next field even when this one is not serialized (this probably makes no sense but idc im solo so far
        /// </summary>
        /// <param name="property"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShouldBeSerialized(property) ? EditorGUI.GetPropertyHeight(property, label) : 0f; // doesn t account for the padding between each variable
        }

        private bool ShouldBeSerialized(SerializedProperty property)
        {
            ConditionalSerializationAttribute ifAttribute = (ConditionalSerializationAttribute)attribute;

            SerializedProperty conditionField = property.serializedObject.FindProperty(ifAttribute.ConditionField);

            _ = conditionField ?? throw new System.Exception("The condition field doesn t exist");

            _ = conditionField.propertyType == SerializedPropertyType.Boolean ? (object)null : throw new System.Exception("The condition field is not a boolean");

            return ifAttribute.InvertCondition ? !conditionField.boolValue : conditionField.boolValue;
        } 
    }
}



