using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class BarrelEndsGatherer
    {
        private List<Transform> barrelEnds = new();
        private int currentIndex = 0;

        private Transform transform;
        public Transform CurrentBarrelEnd => barrelEnds[currentIndex++ % barrelEnds.Count];

        private static readonly string barrelEnd = "barrelEnd";

        public BarrelEndsGatherer(Transform transform_)
        {
            transform = transform_;
        }

        public void GatherBarrelEnds()
        {
            SearchBarrelEndsRecursively();
        }

        private void SearchBarrelEndsRecursively(Transform transform_ = null)
        {
            transform_ = transform_ ?? transform;

            if (transform.name == barrelEnd)
            {
                barrelEnds.Add(transform);
                return;
            }

            for (int childIndex = 0; childIndex < transform_.childCount; childIndex++)
            {
                SearchBarrelEndsRecursively(transform_.GetChild(childIndex));
            }
        }

        private void OnEnable()
        {
            GatherBarrelEnds();
            currentIndex = 0;
        }

    }
}