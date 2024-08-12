using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class PlayerSettings : MonoBehaviour
{
    public DamageLogSettings DamageLogSettings;
}

[Serializable]
public struct DamageLogSettings
{
    public bool DisplayOnRight;
    [Range(10, 400)] public int DisplayOffset;
    public bool DynamicLog;
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")] public IgnorableNonNullableType<Color>[] DamageLogColors;
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")] public IgnorableNonNullableType<FontStyles>[] DamageLogTextModifiers;
    [Range(0, 10)] public float DamageLogDuration;
    [Range(16, 36)] public int DamageLogSize;
    public bool GoingUp;
}

[Serializable]
public struct IgnorableNonNullableType<T>
{
    public bool Ignore;
    public T Content;

    public static implicit operator T(IgnorableNonNullableType<T> ignorableNonNullableType)
    {
        return ignorableNonNullableType.Content;
    }
}

//#3399FFFF
//#1A66CCFF
//#00C8C8FF
//#FFEB04FF
//#EBEBEBFF
//#649696FF
//#808080FF