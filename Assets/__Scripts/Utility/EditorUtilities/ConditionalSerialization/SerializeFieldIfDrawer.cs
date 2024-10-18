
using UnityEditor;
using UnityEngine;


namespace MyEditorUtilities
{
    [CustomPropertyDrawer(typeof(SerializeFieldIfAttribute))]
    [CustomPropertyDrawer(typeof(IfAttribute))]

    [CustomPropertyDrawer(typeof(SerializeFieldIfFieldsAreEqualAttribute))]
    [CustomPropertyDrawer(typeof(IfFieldsAreEqualAttribute))]

    [CustomPropertyDrawer(typeof(SerializeFieldIfMatchConstantAttribute))]
    [CustomPropertyDrawer(typeof(IfMatchConstantAttribute))]
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

            SerializedProperty conditionField = property.serializedObject.FindProperty(ifAttribute.ConditionField) ?? throw new System.Exception("The condition field doesn t exist");

            switch (ifAttribute)
            {
                case SerializeFieldIfFieldsAreEqualAttribute asSerializeFieldIfAreEqualAttribute:

                    SerializedProperty conditionField_ = property.serializedObject.FindProperty(asSerializeFieldIfAreEqualAttribute.ConditionField_) ?? throw new System.Exception("The second condition field doesn t exist");

                    if (conditionField.propertyType == SerializedPropertyType.Enum)
                    {
                        if (conditionField.serializedObject.targetObject.GetType().GetField(conditionField.propertyPath).FieldType == conditionField_.serializedObject.targetObject.GetType().GetField(conditionField_.propertyPath).FieldType)
                        {
                            return asSerializeFieldIfAreEqualAttribute.InvertCondition ? conditionField.enumValueIndex != conditionField_.enumValueIndex : conditionField.enumValueIndex == conditionField_.enumValueIndex;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return asSerializeFieldIfAreEqualAttribute.InvertCondition ? !GetRelevantValue(conditionField).Equals(GetRelevantValue(conditionField_)) : GetRelevantValue(conditionField).Equals(GetRelevantValue(conditionField_));
                    }

                case SerializeFieldIfMatchConstantAttribute asSerializeFieldIfMatchConstantAttribute:

                    if (conditionField.propertyType == SerializedPropertyType.Enum)
                    {
                        if (conditionField.serializedObject.targetObject.GetType().GetField(conditionField.propertyPath).FieldType == asSerializeFieldIfMatchConstantAttribute.ConstantValue.GetType())
                        {
                            return asSerializeFieldIfMatchConstantAttribute.InvertCondition ? conditionField.enumValueIndex != System.Convert.ToInt32(asSerializeFieldIfMatchConstantAttribute.ConstantValue) : conditionField.enumValueIndex == System.Convert.ToInt32(asSerializeFieldIfMatchConstantAttribute.ConstantValue);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return asSerializeFieldIfMatchConstantAttribute.InvertCondition ? !asSerializeFieldIfMatchConstantAttribute.ConstantValue.Equals(GetRelevantValue(conditionField)) : asSerializeFieldIfMatchConstantAttribute.ConstantValue.Equals(GetRelevantValue(conditionField));
                    }
                        

                case SerializeFieldIfAttribute:
                    _ = conditionField.propertyType == SerializedPropertyType.Boolean ? (object)null : throw new System.Exception("The condition field is not a boolean");

                    return ifAttribute.InvertCondition ? !conditionField.boolValue : conditionField.boolValue;

                default:
                    return false;
            }
        }

        private object GetRelevantValue(SerializedProperty conditionField)
        {
            switch (conditionField.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return conditionField.intValue;

                case SerializedPropertyType.Boolean:
                    return conditionField.boolValue;

                case SerializedPropertyType.Float:
                    return conditionField.floatValue;

                case SerializedPropertyType.String:
                    return conditionField.stringValue;

                case SerializedPropertyType.Color:
                    return conditionField.colorValue;

                case SerializedPropertyType.ObjectReference:
                    return conditionField.objectReferenceValue;

                case SerializedPropertyType.Enum:
                    return conditionField.enumValueIndex;

                case SerializedPropertyType.Vector2:
                    return conditionField.vector2Value;

                case SerializedPropertyType.Vector3:
                    return conditionField.vector3Value;

                case SerializedPropertyType.Vector4:
                    return conditionField.vector4Value;

                case SerializedPropertyType.Rect:
                    return conditionField.rectValue;

                case SerializedPropertyType.Bounds:
                    return conditionField.boundsValue;

                case SerializedPropertyType.Quaternion:
                    return conditionField.quaternionValue;

                case SerializedPropertyType.AnimationCurve:
                    return conditionField.animationCurveValue;

                case SerializedPropertyType.LayerMask:
                    return conditionField.intValue;  // LayerMask is stored as int

                case SerializedPropertyType.ArraySize:
                    return conditionField.intValue;  // Array size is stored as int

                case SerializedPropertyType.Character:
                    return (char)conditionField.intValue;

                case SerializedPropertyType.ExposedReference:
                    return conditionField.exposedReferenceValue;

                case SerializedPropertyType.RectInt:
                    return conditionField.rectIntValue;

                case SerializedPropertyType.BoundsInt:
                    return conditionField.boundsIntValue;

                case SerializedPropertyType.Vector2Int:
                    return conditionField.vector2IntValue;

                case SerializedPropertyType.Vector3Int:
                    return conditionField.vector3IntValue;

                default:
                    Debug.LogWarning("Unsupported SerializedPropertyType: " + conditionField.propertyType);
                    return null;
            }
        }

    }
}



