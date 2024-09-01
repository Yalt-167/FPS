using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Utility
{
    public sealed class CoroutineStarter : MonoBehaviour
    {
        public static CoroutineStarter Instance;

        private void Awake()
        {
            Instance = this;
        }

        public void HandleCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}