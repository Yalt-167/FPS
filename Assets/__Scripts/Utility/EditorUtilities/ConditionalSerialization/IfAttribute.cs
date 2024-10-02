
using System;

using UnityEngine;


namespace MyEditorUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IfAttribute : PropertyAttribute
    {
        public string ConditionField { get; private set; }
        public bool InvertCondition { get; private set; }

        public IfAttribute(string conditionField, bool invertCondition = false)
        {
            ConditionField = conditionField;
            InvertCondition = invertCondition;
        }
    }
}
