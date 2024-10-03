
using System;

using UnityEngine;


namespace MyEditorUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ConditionalSerializationAttribute : PropertyAttribute
    {
        public string ConditionField { get; protected set; }
        public bool InvertCondition { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IfAttribute : ConditionalSerializationAttribute
    {
        public IfAttribute(string conditionField, bool invertCondition = false)
        {
            ConditionField = conditionField;
            InvertCondition = invertCondition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeFieldIfAttribute : ConditionalSerializationAttribute
    {
        public SerializeFieldIfAttribute(string conditionField, bool invertCondition = false)
        {
            ConditionField = conditionField;
            InvertCondition = invertCondition;
        }
    }
}
