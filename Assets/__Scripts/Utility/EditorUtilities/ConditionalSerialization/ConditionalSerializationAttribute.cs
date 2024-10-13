
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
}
