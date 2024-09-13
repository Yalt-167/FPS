using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyUtility
{
    public static class Utility
    {
        public static int ModuloThatWorksWithNegatives(int num, int modulo)
        {
            var return_ = num % modulo;
            return return_ < 0 ? return_ + modulo : return_;
        }

        public static Vector3 ReflectVector(Vector3 vectorToReflect, Vector3 normalVector)
        {
            var normalizedNormal = normalVector.normalized;

            var dotProduct = Vector3.Dot(vectorToReflect, normalizedNormal);

            return vectorToReflect - 2 * dotProduct * normalizedNormal;
        }
    }
}