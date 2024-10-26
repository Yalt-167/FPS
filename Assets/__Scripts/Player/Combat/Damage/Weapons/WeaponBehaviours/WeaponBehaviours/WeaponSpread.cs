using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace WeaponHandling
{
    /// <summary>
    /// put it on the root node of the weapon (socket)
    /// </summary>
    public sealed class WeaponSpread : NetworkBehaviour // is only requested clientside so no need to be replicated
    {
        [SerializeField] private bool isShotgun;

        private NetworkVariable<float> currentSpreadAngle = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> directionWithSpread = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkList<Vector3> shotgunDirectionsWithSpread = new NetworkList<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        public Vector3 GetDirectionWithSpread(int barrelEndIndex)
        {
            SetDirectionWithSpreadServerRpc(barrelEndIndex);
            return directionWithSpread.Value;
        }

        public IEnumerable<Vector3> GetShotgunDirectionsWithSpread(int barrelEndIndex)
        {
            SetShotgunDirectionsWithSpreadServerRpc(barrelEndIndex);
            var count = shotgunDirectionsWithSpread.Count;
            for (int i = 0; i < count; i++)
            {
                yield return shotgunDirectionsWithSpread[i];
            }
        }

        private NetworkVariable<SimpleShotStats> aimingSimpleShotStats = new NetworkVariable<SimpleShotStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<SimpleShotStats> hipfireSimpleShotStats = new NetworkVariable<SimpleShotStats>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

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
        public void ApplySpreadServerRpc(float chargeRatio)
        {
            currentSpreadAngle.Value += (IsAiming ? aimingSimpleShotStats.Value.SpreadAngleAddedPerShot : hipfireSimpleShotStats.Value.SpreadAngleAddedPerShot) * chargeRatio;
        }

        [Rpc(SendTo.Server)]
        public void HandleSpreadServerRpc()
        {
            currentSpreadAngle.Value = Mathf.Lerp(currentSpreadAngle.Value, 0f, (IsAiming ? aimingSimpleShotStats.Value.SpreadRegulationSpeed : hipfireSimpleShotStats.Value.SpreadRegulationSpeed) * Time.time);
        }



        private Vector3 GetDirectionWithSpreadInternal(float spreadStrength, Transform directionTransform)
        {
            
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

        [Rpc(SendTo.Server)]
        private void SetDirectionWithSpreadServerRpc(int barrelEndIndex)
        {
            /*
             Most fucked explanantion to ever cross the realm of reality
             / 45f -> to get value which we can use in a vector instead of an angle
            ex in 2D:  a vector that has a 45 angle above X has a (1, 1) direction
            while the X has a (1, 0)
            so we essentially brought the 45 to a value we could use as a direction in the vector
             */
            var spreadStrength = currentSpreadAngle.Value / 45f;
            directionWithSpread.Value = GetDirectionWithSpreadInternal(spreadStrength, weaponHandler.BarrelEnds[barrelEndIndex].transform);

            //    (
            //    barrelEnd.forward + barrelEnd.TransformDirection(
            //        new Vector3(
            //            UnityEngine.Random.Range(-spreadStrength, spreadStrength),
            //            UnityEngine.Random.Range(-spreadStrength, spreadStrength),
            //            0
            //        )
            //    )
            //).normalized;


            //// should be the same unless im retarded (no way huh? ^^)
            //directionWithSpread.Value = (
            //    barrelEnd.forward + barrelEnd.TransformDirection(
            //        new Vector3(
            //            UnityEngine.Random.Range(-currentSpreadAngle.Value, currentSpreadAngle.Value),
            //            UnityEngine.Random.Range(-currentSpreadAngle.Value, currentSpreadAngle.Value),
            //            45f
            //        )
            //    )
            //).normalized;
        }


        [Rpc(SendTo.Server)]
        private void SetShotgunDirectionsWithSpreadServerRpc(int barrelEndIndex)
        {
            var barrelEnd = weaponHandler.BarrelEnds[barrelEndIndex].transform;

            shotgunDirectionsWithSpread.Clear();

            var relevantSpread = IsAiming ? weaponHandler.CurrentWeapon.ShotgunStats.AimingPelletsSpreadAngle : weaponHandler.CurrentWeapon.ShotgunStats.PelletsSpreadAngle;
            relevantSpread /= 45f;

            var pelletCount = weaponHandler.CurrentWeapon.ShotgunStats.PelletsCount;
            for (int i = 0; i < pelletCount; i++)
            {
                shotgunDirectionsWithSpread.Add(GetDirectionWithSpreadInternal(relevantSpread, barrelEnd));
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }
}