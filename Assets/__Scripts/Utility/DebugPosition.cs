using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace NameSpace
{
    public sealed class DebugPosition : MonoBehaviour
    {
        [SerializeField] private float radius;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}