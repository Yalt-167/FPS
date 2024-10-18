using System;
using System.Collections;
using System.Collections.Generic;

namespace MyEditorUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeFieldIfMatchConstantAttribute : ConditionalSerializationAttribute
    {
        public object ConstantValue { get; protected set; }
        public SerializeFieldIfMatchConstantAttribute(string conditionField, object constantValue, bool invertCondition = false)
        {
            ConditionField = conditionField;
            ConstantValue = constantValue;
            InvertCondition = invertCondition;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IfMatchConstantAttribute : SerializeFieldIfMatchConstantAttribute
    {
        public IfMatchConstantAttribute(string conditionField, object constantValue, bool invertCondition = false) : base(conditionField, constantValue, invertCondition) { }
    }
}