using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    /// <summary>
    /// Make sure to ASSIGN it to the vector you want<br/>
    /// otherwise it will NOT take effect due to Vector3 being a struct (value type)
    /// </summary>
    /// <param name="currentVector"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Vector3 Mask(this Vector3 currentVector, Vector3 other)
    {
        return currentVector.Mask(other.x, other.y, other.z);
    }

    /// <summary>
    /// Make sure to ASSIGN it to the vector you want<br/>
    /// otherwise it will NOT take effect due to Vector3 being a struct (value type)
    /// </summary>
    /// <param name="currentVector"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Mask(this Vector3 currentVector, float x, float y, float z)
    {
        return new(currentVector.x * x, currentVector.y * y, currentVector.z * z);
    }

    /// <summary>
    /// Make sure to ASSIGN it to the vector you want<br/>
    /// otherwise it will NOT take effect due to Vector3 being a struct (value type)
    /// </summary>
    /// <param name="currentVector"></param>
    /// <param name="keepX"></param>
    /// <param name="keepY"></param>
    /// <param name="keepZ"></param>
    /// <returns></returns>
    public static Vector3 Mask(this Vector3 currentVector, bool keepX, bool keepY, bool keepZ)
    {
        return currentVector.Mask(
            new Vector3(
                keepX ? 1 : 0,
                keepY ? 1 : 0,
                keepZ ? 1 : 0
            )
        );
    }

    public static Rect ToRect(this Vector4 currentVector)
    {
        return new Rect(currentVector.x, currentVector.y, currentVector.z, currentVector.w);
    }
}