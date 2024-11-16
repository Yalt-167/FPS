using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MyUtilities
{
    public sealed class CoroutineStarter : MonoBehaviour
    {
        public static CoroutineStarter Instance;

        private void Awake()
        {
            Instance = this;
        }

        public static void HandleCoroutine(IEnumerator coroutine)
        {
            Instance.StartCoroutine(coroutine);
        }
    }
}