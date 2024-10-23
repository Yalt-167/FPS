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
        private NetworkVariable<SimpleShotStats> aimingSimpleShotStats = new NetworkVariable<SimpleShotStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<SimpleShotStats> hipfireSimpleShotStats = new NetworkVariable<SimpleShotStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        private Transform barrelEnd;
        private WeaponHandler weaponHandler;
        private bool IsAiming => weaponHandler.IsAiming;

        private void Awake()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
        }

        public IEnumerator SetupData(SimpleShotStats aimingSimpleShotStats_, SimpleShotStats hipfireSimpleShotStats_)
        {
            yield return new WaitUntil(() => IsSpawned);

            SetDataServerRpc(aimingSimpleShotStats_, hipfireSimpleShotStats_);
        }

        [Rpc(SendTo.Server)]
        public void SetDataServerRpc(SimpleShotStats aimingSimpleShotStats_, SimpleShotStats hipfireSimpleShotStats_)
        {
            aimingSimpleShotStats.Value = aimingSimpleShotStats_;
            hipfireSimpleShotStats.Value = hipfireSimpleShotStats_;
        }

        [Rpc(SendTo.Server)]
        public void ApplySpreadServerRpc()
        {
            currentSpreadAngle.Value += IsAiming ? aimingSimpleShotStats.Value.SpreadAngleAddedPerShot : hipfireSimpleShotStats.Value.SpreadAngleAddedPerShot;
        }

        [Rpc(SendTo.Server)]
        public void HandleSpreadServerRpc()
        {
            currentSpreadAngle.Value = Mathf.Lerp(currentSpreadAngle.Value, 0f, (IsAiming ? aimingSimpleShotStats.Value.SpreadRegulationSpeed : hipfireSimpleShotStats.Value.SpreadRegulationSpeed) * Time.time);
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
    }
}