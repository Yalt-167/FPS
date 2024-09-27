using System;
using System.Collections;
using System.Collections.Generic;

using MapHandling;

using UnityEngine;

namespace SceneHandling
{
    public sealed class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] private MapData currentMapData;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDrawGizmos()
        {
            if (currentMapData == null) { return; }
                
            currentMapData.DebugBounds();
        }
    }
}