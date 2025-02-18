//#define NETWORK_LIST_AMALGAMATION

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Assertions;

namespace WeaponHandling
{
    /// <summary>
    /// put it on the root node of the weapon (socket)
    /// </summary>
    public sealed class WeaponSpread : NetworkBehaviour // is only requested clientside so no need to be replicated
    {
        private NetworkVariable<float> currentSpreadAngle = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> directionWithSpread = new NetworkVariable<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
#if NETWORK_LIST_AMALGAMATION
        private NetworkVariable<List<Vector3>> shotgunDirectionsWithSpread = new NetworkVariable<List<Vector3>>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
#else
        //private NetworkList<Vector3> shotgunDirectionsWithSpread = new NetworkList<Vector3>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
#endif
        // make that a List<NetworkVariable<Vector3>> XD
        public Vector3 GetDirectionWithSpread(int barrelEndIndex)
        {
            SetDirectionWithSpreadServerRpc(barrelEndIndex);
            return directionWithSpread.Value;
        }
        public Vector3 GetDirectionWithSpreadFromServer(int barrelEndIndex)
        {
            SetDirectionWithSpreadFromServer(barrelEndIndex);
            return directionWithSpread.Value;
        }

//        public IEnumerable<Vector3> GetShotgunDirectionsWithSpread(int barrelEndIndex)
//        {
//            SetShotgunDirectionsWithSpreadServerRpc(barrelEndIndex);
//#if NETWORK_LIST_AMALGAMATION
//            var count = shotgunDirectionsWithSpread.Value.Count;
//#else
//            var count = shotgunDirectionsWithSpread.Count;
//#endif
//            for (int i = 0; i < count; i++)
//            {
//#if NETWORK_LIST_AMALGAMATION
//                yield return shotgunDirectionsWithSpread.Value[i];
//#else
//                yield return shotgunDirectionsWithSpread[i];
//#endif
//            }
//        }
//        public IEnumerable<Vector3> GetShotgunDirectionsWithSpreadFromServer(int barrelEndIndex)
//        {
//            SetShotgunDirectionsWithSpreadFromServer(barrelEndIndex);
//#if NETWORK_LIST_AMALGAMATION
//            var count = shotgunDirectionsWithSpread.Value.Count;
//#else
//            var count = shotgunDirectionsWithSpread.Count;
//#endif
//            for (int i = 0; i < count; i++)
//            {
//#if NETWORK_LIST_AMALGAMATION
//                yield return shotgunDirectionsWithSpread.Value[i];
//#else
//                yield return shotgunDirectionsWithSpread[i];
//#endif
//            }
//        }


        private SimpleShotStats AimingSimpleShotStats => weaponHandler.CurrentWeaponSO.AimingSimpleShotStats;
        private SimpleShotStats HipfireSimpleShotStats => weaponHandler.CurrentWeaponSO.SimpleShotStats;

        private WeaponHandler weaponHandler;
        private bool IsAiming => weaponHandler.IsAiming;


        private void Awake()
        {
            weaponHandler = GetComponentInParent<WeaponHandler>();
        }

        public void SetupData(WeaponHandler weaponHandler_)
        {
            weaponHandler = weaponHandler_;
        }

        [Rpc(SendTo.Server)]
        public void ApplySpreadServerRpc(float chargeRatio)
        {
            currentSpreadAngle.Value += (IsAiming ? AimingSimpleShotStats.SpreadAngleAddedPerShot : HipfireSimpleShotStats.SpreadAngleAddedPerShot) * chargeRatio;
        }

        public void ApplySpreadFromServer(float chargeRatio)
        {
            currentSpreadAngle.Value += (IsAiming ? AimingSimpleShotStats.SpreadAngleAddedPerShot : HipfireSimpleShotStats.SpreadAngleAddedPerShot) * chargeRatio;
        }

        [Rpc(SendTo.Server)]
        public void HandleSpreadServerRpc()
        {
            currentSpreadAngle.Value = Mathf.Lerp(currentSpreadAngle.Value, 0f, (IsAiming ? AimingSimpleShotStats.SpreadRegulationSpeed : HipfireSimpleShotStats.SpreadRegulationSpeed) * Time.deltaTime);
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
        private void SetDirectionWithSpreadFromServer(int barrelEndIndex)
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

//        [Rpc(SendTo.Server)]
//        private void SetShotgunDirectionsWithSpreadServerRpc(int barrelEndIndex)
//        {
//            var barrelEnd = weaponHandler.BarrelEnds[barrelEndIndex].transform;

//#if NETWORK_LIST_AMALGAMATION
//            shotgunDirectionsWithSpread.Value.Clear();
//#else
//            shotgunDirectionsWithSpread.Clear();
//#endif

//            var relevantSpread = IsAiming ? weaponHandler.CurrentWeapon.ShotgunStats.AimingPelletsSpreadAngle : weaponHandler.CurrentWeapon.ShotgunStats.HipfirePelletsSpreadAngle;
//            relevantSpread /= 45f;

//            var pelletCount = weaponHandler.CurrentWeapon.ShotgunStats.PelletsCount;
//            for (int i = 0; i < pelletCount; i++)
//            {
//#if NETWORK_LIST_AMALGAMATION
//                shotgunDirectionsWithSpread.Value.Add(GetDirectionWithSpreadInternal(relevantSpread, barrelEnd));
//#else
//                shotgunDirectionsWithSpread.Add(GetDirectionWithSpreadInternal(relevantSpread, barrelEnd));
//#endif
//            }
//        }
//        private void SetShotgunDirectionsWithSpreadFromServer(int barrelEndIndex)
//        {
//            var barrelEnd = weaponHandler.BarrelEnds[barrelEndIndex].transform;

//#if NETWORK_LIST_AMALGAMATION
//            shotgunDirectionsWithSpread.Value.Clear();
//#else
//            shotgunDirectionsWithSpread.Clear();
//#endif

//            var relevantSpread = IsAiming ? weaponHandler.CurrentWeapon.ShotgunStats.AimingPelletsSpreadAngle : weaponHandler.CurrentWeapon.ShotgunStats.HipfirePelletsSpreadAngle;
//            relevantSpread /= 45f;

//            var pelletCount = weaponHandler.CurrentWeapon.ShotgunStats.PelletsCount;
//            for (int i = 0; i < pelletCount; i++)
//            {
//#if NETWORK_LIST_AMALGAMATION
//                shotgunDirectionsWithSpread.Value.Add(GetDirectionWithSpreadInternal(relevantSpread, barrelEnd));
//#else
//                shotgunDirectionsWithSpread.Add(GetDirectionWithSpreadInternal(relevantSpread, barrelEnd));
//#endif
//            }
//        }


        [UnityEditor.MenuItem("Developer/TestComputeShotgunSpread")]
        public static void TestComputeShotgunSpread()
        {
            MyDebug.DebugUtility.PrintIterable(new Vector3[] { });
        }

        // make shotgun spread constant instead
        public Vector3[] ComputeShotgunSpread(int barrelEndIndex)
        {
            var barrelEnd = weaponHandler.BarrelEnds[barrelEndIndex].transform;

            var relevantSpread = IsAiming ? weaponHandler.CurrentWeaponSO.ShotgunStats.AimingPelletsSpreadAngle : weaponHandler.CurrentWeaponSO.ShotgunStats.HipfirePelletsSpreadAngle;
            relevantSpread /= 45f;

            Vector3[] values = new Vector3[weaponHandler.CurrentWeaponSO.ShotgunStats.PelletsCount];

            //_ = values.Length % 4 == 0 ? (object)null : throw new System.Exception("Make sure the pellets count is divisible by 4");
            Assert.IsTrue(values.Length % 4 == 0);

            float spreadThisIteration;
            for (int i = 0; i < values.Length / 4; i++)
            {
                spreadThisIteration = relevantSpread / i;
                values[i * 4] = barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(spreadThisIteration, spreadThisIteration, 0f
                        )
                ).normalized;
                values[i * 4 + 1] = barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(spreadThisIteration, -spreadThisIteration, 0f
                        )
                ).normalized;
                values[i * 4 + 2] = barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(-spreadThisIteration, spreadThisIteration, 0f
                        )
                ).normalized;
                values[i * 4 + 3] = barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(-spreadThisIteration, -spreadThisIteration, 0f
                        )
                ).normalized;
            }

            return values;
        }


        public IEnumerable<Vector3> ComputeShotgunSpreadEnumerable(int barrelEndIndex)
        {
            var barrelEnd = weaponHandler.BarrelEnds[barrelEndIndex].transform;

            var relevantSpread = IsAiming ? weaponHandler.CurrentWeaponSO.ShotgunStats.AimingPelletsSpreadAngle : weaponHandler.CurrentWeaponSO.ShotgunStats.HipfirePelletsSpreadAngle;
            relevantSpread /= 45f;

            Assert.IsTrue(weaponHandler.CurrentWeaponSO.ShotgunStats.PelletsCount % 4 == 0);
            int length = weaponHandler.CurrentWeaponSO.ShotgunStats.PelletsCount / 4;

            //_ = values.Length % 4 == 0 ? (object)null : throw new System.Exception("Make sure the pellets count is divisible by 4");

            float spreadThisIteration;
            for (int i = 0; i < length / 4; i++)
            {
                spreadThisIteration = relevantSpread / i;
                yield return barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(spreadThisIteration, spreadThisIteration, 0f
                        )
                ).normalized;
                yield return barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(spreadThisIteration, -spreadThisIteration, 0f
                        )
                ).normalized;
                yield return barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(-spreadThisIteration, spreadThisIteration, 0f
                        )
                ).normalized;
                yield return barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(-spreadThisIteration, -spreadThisIteration, 0f
                        )
                ).normalized;
            }
        }
    }
}
