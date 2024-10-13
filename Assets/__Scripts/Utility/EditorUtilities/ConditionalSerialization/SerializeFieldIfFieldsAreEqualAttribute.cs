using System;
using System.Collections;
using System.Collections.Generic;

namespace MyEditorUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeFieldIfFieldsAreEqualAttribute : ConditionalSerializationAttribute
    {
        public string ConditionField_ { get; protected set; }
        public SerializeFieldIfFieldsAreEqualAttribute(string conditionField, string conditionField_, bool invertCondition = false)
        {
            ConditionField = conditionField;
            ConditionField_ = conditionField_;
            InvertCondition = invertCondition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IfFieldsAreEqualAttribute : SerializeFieldIfFieldsAreEqualAttribute
    {
        public IfFieldsAreEqualAttribute(string conditionField, string conditionField_, bool invertCondition = false) : base(conditionField, conditionField_, invertCondition) { }
    }
}