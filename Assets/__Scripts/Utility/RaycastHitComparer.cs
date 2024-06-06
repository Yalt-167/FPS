using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastHitComparer : IComparer<RaycastHit>
{
    public int Compare(RaycastHit hit1, RaycastHit hit2)
    {
        return hit1.distance.CompareTo(hit2.distance);
    }
}
