using System;
using System.Collections;
using System.Collections.Generic;

namespace MyEditorUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeFieldIfAttribute : ConditionalSerializationAttribute
    {
        public SerializeFieldIfAttribute(string conditionField, bool invertCondition = false)
        {
            ConditionField = conditionField;
            InvertCondition = invertCondition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IfAttribute : SerializeFieldIfAttribute
    {
        public IfAttribute(string conditionField, bool invertCondition = false) : base(conditionField, invertCondition) { }
    }
}