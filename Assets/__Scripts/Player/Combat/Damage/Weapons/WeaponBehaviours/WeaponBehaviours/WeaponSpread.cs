using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponSpread : WeaponReplicatedBehaviour // is only requested clientside so no need to be replicated
    {

        private NetworkVariable<float> currentSpreadAngle = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> directionWithSpread = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        // as networkVar to prevent tampering with those
        private SimpleShotStats aimingSimpleShotStats;
        private SimpleShotStats simpleShotStats;

        private Transform barrelEnd;

        private bool IsAiming => throw new NotImplementedException(); // link what needs to be

        private bool isAiming;


        [Rpc(SendTo.ClientsAndHost)]
        public void ApplySpreadClientRpc()
        {
            currentSpreadAngle.Value += isAiming ? aimingSimpleShotStats.SpreadAngleAddedPerShot : simpleShotStats.SpreadAngleAddedPerShot;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void HandleSpreadClientRpc()
        {
            currentSpreadAngle.Value = Mathf.Lerp(currentSpreadAngle.Value, 0f, (isAiming ? aimingSimpleShotStats.SpreadRegulationSpeed : simpleShotStats.SpreadRegulationSpeed) * Time.time);
        }




        [Rpc(SendTo.Server)]
        public void SetDirectionWithSpreadServerRpc()
        {
            var spreadStrength = currentSpreadAngle.Value / 45f;
            /*Most fucked explanantion to ever cross the realm of reality
             / 45f -> to get value which we can use in a vector instead of an angle
            ex in 2D:  a vector that has a 45 angle above X has a (1, 1) direction
            while the X has a (1, 0)
            so we essentially brought the 45 to a value we could use as a direction in the vector
             */
            // perhaps do directionTransform.forward * 45 instead of other / 45 (for performances purposes)
            directionWithSpread.Value = (
                barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(
                        UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                        UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                        0
                    )
                )
            ).normalized;


            // should be the same unless im retarded (no way huh? ^^)
            directionWithSpread.Value = (
                barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(
                        UnityEngine.Random.Range(-currentSpreadAngle.Value, currentSpreadAngle.Value),
                        UnityEngine.Random.Range(-currentSpreadAngle.Value, currentSpreadAngle.Value),
                        45f
                    )
                )
            ).normalized;
        }

        //private Vector3 GetDirectionWithSpread(float spreadAngle, Vector3 direction)
        //{
        //    var spreadStrength = spreadAngle / 45f;
        //    /*Most fucked explanantion to ever cross the realm of reality
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