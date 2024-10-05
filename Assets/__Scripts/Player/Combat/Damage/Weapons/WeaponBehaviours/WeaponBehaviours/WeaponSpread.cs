using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponSpread : WeaponReplicatedBehaviour // is only requested clientside so no need to be replicated
    {
        public /*override*/ void Setup(Weapon weapon)
        {
            throw new NotImplementedException();
        }


        private float currentSpreadAngle;
        private SimpleShotStats aimingSimpleShotStats;
        private SimpleShotStats simpleShotStats;

        private bool IsAiming => throw new NotImplementedException(); // link what needs to be

        private bool isAiming;


        private void FixedUpdate()
        {
            HandleSpead();
        }

        private void ApplySpread()
        {
            currentSpreadAngle += isAiming ? aimingSimpleShotStats.SpreadAngleAddedPerShot : simpleShotStats.SpreadAngleAddedPerShot;
        }

        private void HandleSpead()
        {
            currentSpreadAngle = Mathf.Lerp(currentSpreadAngle, 0f, (isAiming ? aimingSimpleShotStats.SpreadRegulationSpeed : simpleShotStats.SpreadRegulationSpeed) * Time.time);
        }


        private Vector3 GetDirectionWithSpread(float spreadAngle, Transform directionTransform)
        {
            var spreadStrength = spreadAngle / 45f;
            /*Most fucked explanantion to ever cross the realm of reality
             / 45f -> to get value which we can use in a vector instead of an angle
            ex in 2D:  a vector that has a 45 angle above X has a (1, 1) direction
            while the X has a (1, 0)
            so we essentially brought the 45 to a value we could use as a direction in the vector
             */
            // perhaps do directionTransform.forward * 45 instead of other / 45 (for performances purposes)
            return (
                    directionTransform.forward + directionTransform.TransformDirection(
                        new Vector3(
                            UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                            UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                            0
                        )
                    )
                ).normalized;
        }

        //private Vector3 GetDirectionWithSpread(float spreadAngle, Vector3 direction)
        //{
        //    var spreadStrength = spreadAngle / 45f;
        //    /*Most fucked explanantion to ever cross the frontier of reality
        //     / 45f -> to get value which we can use iun a vector instead of an angle
        //    ex in 2D:  a vector that has a 45 angle above X has a (1, 1) direction
        //    while the X has a (1, 0)
        //    so we essentially brought the 45 to a value we could use as a direction in the vector
        //     */
        //    // perhaps do directionTransform.forward * 45 instead of other / 45 (for performances purposes)
        //    return (
        //            direction + direction.TransformDirection(
        //                new Vector3(
        //                    Random.Range(-spreadStrength, spreadStrength),
        //                    Random.Range(-spreadStrength, spreadStrength),
        //                    0
        //                )
        //            )
        //        ).normalized;
        //}
    }
}