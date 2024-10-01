using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the root/socket/handle of the weapon for it to handle the ADS logic
    /// </summary>
    public sealed class WeaponADS
    {
        [SerializeField] private Transform basePosition;
        [SerializeField] private Transform ADSPosition;
        [SerializeField] private Transform weaponSocket;

        private float weaponADSspeed;
        private float weaponUnADSspeed;
        [SerializeField] private float lerpLeniency;

        private bool isADSing;


        public void ToggleADS(bool towardOn)
        {
            isADSing = towardOn;
        }

        private void Update()
        {
            HandleLerp();
        }


        private void HandleLerp()
        {
            var startingPoint = weaponSocket.position;

            var (targetPosition, transitionSpeed) = isADSing ? (ADSPosition.position, weaponADSspeed) : (basePosition.position, weaponUnADSspeed);
            weaponSocket.position = Vector3.Lerp(startingPoint, targetPosition, transitionSpeed);
        }
    }
}


//public sealed class ADSAnchor: MonoBehaviour
//{
//    [SerializeField] private Transform[] anchors;

//    private int currentAnchorIndex = 0;
//    public Transform Anchor => anchors[currentAnchorIndex++ % anchors.Length];

//}