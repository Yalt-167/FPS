using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// Add this script on the root/socket/handle of the weapon for it to handle the ADS logic
    /// </summary>
    public sealed class WeaponADS : MonoBehaviour // moves the transfom which is already propagated + may do some clientside camera adjustment so no need to replicate
    {
        [SerializeField] private Transform basePosition;
        [SerializeField] private Transform ADSPosition;
        [SerializeField] private bool doDebugPosition;

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

        public void SetupData(AimAndScopeStats aimingStats_)
        {
            AimAndScopeStats aimingStats = aimingStats_;
        }

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
            var startingPoint = transform.position;

            var (targetPosition, transitionSpeed) = isADSing ? (ADSPosition, aimingStats.TimeToADS) : (basePosition, aimingStats.TimeToUnADS);
            transform.position = Vector3.Lerp(startingPoint, targetPosition.position, transitionSpeed);

            if ((transform.transform.position - targetPosition.position).sqrMagnitude < sqrLerpLeniency)
            {
                transform.transform.position = targetPosition.position;
            }
        }

        private void HandleFOVLerp()
        {

        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (basePosition != null) Gizmos.DrawWireSphere(basePosition.position, .1f);

            Gizmos.color = Color.yellow;
            if (ADSPosition != null) Gizmos.DrawWireSphere(ADSPosition.position, .1f);
        }
    }
}


//public sealed class ADSAnchor: MonoBehaviour
//{
//    [SerializeField] private Transform[] anchors;

//    private int currentAnchorIndex = 0;
//    public Transform Anchor => anchors[currentAnchorIndex++ % anchors.Length];

//}