using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    public static int ModuloThatWorksWithNegatives(int num, int modulo)
    {
        var return_ = num % modulo;
        return return_ < 0 ? return_ + modulo : return_;
    }
}

public class RaycastHitComparer : IComparer<RaycastHit>
{
    public int Compare(RaycastHit hit1, RaycastHit hit2)
    {
        return hit1.distance.CompareTo(hit2.distance);
    }
}