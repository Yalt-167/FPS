using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the root/socket/handle of the weapon for it to handle the ADS logic
    /// </summary>
    public sealed class WeaponADS : WeaponBehaviour // moves the transfom which is already propagated + may do some clientside camera adjustment so no need to replicate
    {
        public override void Setup(Weapon weapon)
        {

        }


        [SerializeField] private Transform basePosition;
        [SerializeField] private Transform ADSPosition;
        [SerializeField] private Transform weaponSocket;

        private readonly int baseFOV = 60;

        private AimAndScopeStats aimingStats;
#if false
        public struct AimAndScopeStats
		{
            public float AimingFOV; //perhaps just a cameraMove instead
            [Range(1, 5)][Tooltip("Set to 1 for no Scope")] public float ScopeMagnification;
            public float TimeToADS;
            public float TimeToUnADS;
        } 
#endif
        [SerializeField] private float sqrLerpLeniency;

        private bool isADSing;


        public void ToggleADS(bool towardOn)
        {
            isADSing = towardOn;
        }

        private void FixedUpdate()
        {
            HandleWeaponLerp();
            HandleFOVLerp();
        }


        private void HandleWeaponLerp()
        {
            var startingPoint = weaponSocket.position;

            var (targetPosition, transitionSpeed) = isADSing ? (ADSPosition.position, aimingStats.TimeToADS) : (basePosition.position, aimingStats.TimeToUnADS);
            weaponSocket.position = Vector3.Lerp(startingPoint, targetPosition, transitionSpeed);

            if ((weaponSocket.transform.position - targetPosition).sqrMagnitude < sqrLerpLeniency)
            {
                weaponSocket.transform.position = targetPosition;
            }
        }

        private void HandleFOVLerp()
        {

        }
    }
}


//public sealed class ADSAnchor: MonoBehaviour
//{
//    [SerializeField] private Transform[] anchors;

//    private int currentAnchorIndex = 0;
//    public Transform Anchor => anchors[currentAnchorIndex++ % anchors.Length];

//}