using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponKickbackGatherer
    {
        private List<WeaponKickback> weaponKickbacks = new();
        private int currentIndex = 0;

        private Transform transform;
        public WeaponKickback CurrentKickbackScript => weaponKickbacks[currentIndex++ % weaponKickbacks.Count];

        public WeaponKickbackGatherer(Transform transform_)
        {
            transform = transform_;
        }

        public void GatherWeaponKickbacks()
        {
            SearchWeaponKickbacksRecursively();
        }

        private void SearchWeaponKickbacksRecursively(Transform transform_ = null)
        {
            transform_ = transform_ ?? transform;

            if (transform.gameObject.TryGetComponent<WeaponKickback>(out var weaponKickback))
            {
                weaponKickbacks.Add(weaponKickback);
                return;
            }

            for (int childIndex = 0; childIndex < transform_.childCount; childIndex++)
            {
                SearchWeaponKickbacksRecursively(transform_.GetChild(childIndex));
            }
        }

        private void OnEnable()
        {
            GatherWeaponKickbacks();
            currentIndex = 0;
        }
    }
}