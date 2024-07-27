using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Interpolation
{
    public static float ExponentialLerp(float t, float exponentiationFactor = 5.0f)
    {
        return 1.0f - Mathf.Exp(-exponentiationFactor * t);
    }
}
