using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace MapHandling
{
    [CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/MapData")]
    public sealed class MapData : ScriptableObject
    {
        public MapBounds MapBounds;

        public int DebugPlaneLineIncrement = 10;
        public int DebugPlaneLineExtents = 100;
        public int DebugDist = 100; // how far the plane with be rendered


        private void OnValidate()
        {
            MapBounds.HighestOrigin = Vector3.up * MapBounds.HighestPoint;
            MapBounds.LowestOrigin = Vector3.up * MapBounds.LowestPoint;

            MapBounds.NorthMostOrigin = Vector3.forward * MapBounds.NorthMostPoint;
            MapBounds.SouthMostOrigin = Vector3.forward * MapBounds.SouthMostPoint;

            MapBounds.EastMostOrigin = Vector3.right * MapBounds.EastMostPoint;
            MapBounds.WestMostOrigin = Vector3.right * MapBounds.WestMostPoint;
        }


        public bool IsOutOfBound(Transform transform)
        {
            return IsOutOfBound(transform.position);
        }

        public bool IsOutOfBound(Vector3 position)
        {
            if (position.y > MapBounds.HighestPoint) { return true; }

            if (position.y < MapBounds.LowestPoint) { return true; }


            if (position.x > MapBounds.EastMostPoint) { return true; }

            if (position.x < MapBounds.WestMostPoint) { return true; }


            if (position.z > MapBounds.NorthMostPoint) { return true; }

            if (position.z < MapBounds.SouthMostPoint) { return true; }


            return false;
        }

        public void DebugBounds()
        {
            if (MapBounds.DebugHighestPoint || MapBounds.DebugLowestPoint)
            {
                DebugBound(Vector3.up, new Vector3[2] { Vector3.forward, Vector3.right }, new bool[2] { MapBounds.DebugHighestPoint, MapBounds.DebugLowestPoint }, new Vector3[2] { MapBounds.HighestOrigin, MapBounds.LowestOrigin }, Color.green);
            }

            if (MapBounds.DebugNorthMostPoint || MapBounds.DebugSouthMostPoint)
            {
                DebugBound(Vector3.forward, new Vector3[2] { Vector3.up, Vector3.right }, new bool[2] { MapBounds.DebugNorthMostPoint, MapBounds.DebugSouthMostPoint }, new Vector3[2] { MapBounds.NorthMostOrigin, MapBounds.SouthMostOrigin }, Color.blue);
            }

            if (MapBounds.DebugEastMostPoint || MapBounds.DebugWestMostPoint)
            {
                DebugBound(Vector3.right, new Vector3[2] { Vector3.up, Vector3.forward }, new bool[2] { MapBounds.DebugEastMostPoint, MapBounds.DebugWestMostPoint }, new Vector3[2] { MapBounds.EastMostOrigin, MapBounds.WestMostOrigin }, Color.red);
            }
        }

        private void DebugBound(Vector3 relevantAxis, Vector3[] sideAxises, bool[] debugExtents, Vector3[] origins_, Color color)
        {
            Gizmos.color = color;

            Vector3[] origins;
            if (debugExtents[1] && debugExtents[0])
            {
                origins = origins_;
            }
            else
            {
                if (debugExtents[1]) { origins = new Vector3[1] { origins_[1] }; }
                else { origins = new Vector3[1] { origins_[0] }; }
            }

            var firstSideAxis = sideAxises[0] * DebugDist;
            var secondSideAxis = sideAxises[1] * DebugDist;
            for (int offset = -DebugPlaneLineExtents; offset <= DebugPlaneLineExtents; offset += DebugPlaneLineIncrement)
            {
                foreach (var origin in origins)
                {
                    var firstSideAxisOffsetVec = sideAxises[1] * offset;
                    Gizmos.DrawLine(origin - firstSideAxis + firstSideAxisOffsetVec, origin + firstSideAxis + firstSideAxisOffsetVec);

                    var secondSideAxisOffsetVec = sideAxises[0] * offset;
                    Gizmos.DrawLine(origin - secondSideAxis + secondSideAxisOffsetVec, origin + secondSideAxis + secondSideAxisOffsetVec);
                }
            }
        }
    }

    [Serializable]
    public struct MapBounds
    {

        public int HighestPoint;
        public bool DebugHighestPoint;
        [HideInInspector] public Vector3 HighestOrigin;

        public int LowestPoint;
        public bool DebugLowestPoint;
        [HideInInspector] public Vector3 LowestOrigin;

        public int NorthMostPoint;
        public bool DebugNorthMostPoint;
        [HideInInspector] public Vector3 NorthMostOrigin;

        public int SouthMostPoint;
        public bool DebugSouthMostPoint;
        [HideInInspector] public Vector3 SouthMostOrigin;

        public int EastMostPoint;
        public bool DebugEastMostPoint;
        [HideInInspector] public Vector3 EastMostOrigin;

        public int WestMostPoint;
        public bool DebugWestMostPoint;
        [HideInInspector] public Vector3 WestMostOrigin;
    }
}