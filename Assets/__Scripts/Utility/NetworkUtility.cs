using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MyUtilities
{
    public static class NetworkUtility
    {
        public static void CallWhenNetworkSpawned(Unity.Netcode.NetworkBehaviour networkBehaviour, Action delegate_)
        {
            CoroutineStarter.HandleCoroutine(CallWhenNetworkSpawnedInternal(networkBehaviour,delegate_));
        }

        private static IEnumerator CallWhenNetworkSpawnedInternal(Unity.Netcode.NetworkBehaviour networkBehaviour, Action delegate_)
        {
            yield return new WaitUntil(() => networkBehaviour.IsSpawned);

            delegate_();
        }

    }
}