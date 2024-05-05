using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSettings : MonoBehaviour
{
    public HitMarkerSettings HitMarkerSettings;
}
[Serializable]
public struct HitMarkerSettings
{
    public bool DisplayOnRight;
    [Range(0, 400)] public int DisplayOffset;
    public bool DynamicMarker;
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Legs(Shielded) -> Head -> Body -> Legs -> Object(Weakpoint) -> Object")] public Color?[] CustomColors;
    [Range(0, 10)] public float HitMarkerDuration;
}