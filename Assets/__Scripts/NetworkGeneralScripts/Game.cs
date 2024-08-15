//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Unity.Netcode;
using UnityEditor;
using UnityEngine;

using Random = UnityEngine.Random;

namespace GameManagement
{
    public sealed class Game : NetworkBehaviour
    {
        public static Game Manager;

        #region Player Combat List

        private readonly List<WeaponHandler> networkedWeaponHandlers = new();

        public void AddNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
        {
            networkedWeaponHandlers.Add(networkedWeaponHandler);
        }

        public void DiscardNetworkedWeaponHandler(WeaponHandler networkedWeaponHandler)
        {
            networkedWeaponHandlers.Remove(networkedWeaponHandler);
        }

        public WeaponHandler GetNetworkedWeaponHandlerFromNetworkObjectID(ulong playerObjectID) // ? make it so they are sorted ? -> slower upon adding it but faster to select it still
        {
            foreach (var networkedWeaponHandler in networkedWeaponHandlers)
            {
                if (networkedWeaponHandler.NetworkObjectId == playerObjectID)
                {
                    return networkedWeaponHandler;
                }
            }

            return null;
        }

        public IEnumerable<WeaponHandler> GetNetworkedWeaponHandlers()
        {
            foreach (var networkHandler in networkedWeaponHandlers)
            {
                yield return networkHandler;
            }
        }


        #endregion

        #region Respawn Logic

        private readonly Dictionary<ushort, List<SpawnPoint>> spawnPoints = new();

        public void AddRespawnPoint(SpawnPoint spawnPoint)
        {
            if (!spawnPoints.ContainsKey(spawnPoint.TeamID))
            {
                spawnPoints.Add(spawnPoint.TeamID, new());
            }

            spawnPoints[spawnPoint.TeamID].Add(spawnPoint);
        }


        public void DiscardRespawnPoint(SpawnPoint spawnPoint)
        {
            spawnPoints[spawnPoint.TeamID].Remove(spawnPoint);
        }

        public Vector3 GetSpawnPosition(ushort teamID)
        {
            var spawnPointExists = spawnPoints.TryGetValue(teamID, out var relevantSpawnPoints);

            if (!spawnPointExists) { return NoSpawnpointAvailableForThisTeam(teamID); }

            // filtering the active ones
            var activeRelevantSpawnPoints = (List<SpawnPoint>)relevantSpawnPoints.Where(predicate: (spawnPoint) => spawnPoint.Active);

            if (activeRelevantSpawnPoints.Count == 0) { return NoSpawnpointAvailableForThisTeam(teamID); }

            return activeRelevantSpawnPoints[Random.Range(0, activeRelevantSpawnPoints.Count - 1)].SpawnPosition;
        }

        private Vector3 NoSpawnpointAvailableForThisTeam(ushort teamID)
        {
            Debug.Log($"There s no checkpoint available for this player with team ID: {teamID}");
            return Vector3.zero;
        }

        #endregion

        #region Game Start

        private bool gameStarted;

        [MenuItem("Developer/StartGame")]
        public static void StaticStartGame()
        {
            if (Manager == null)
            {
                //GameNetworkManager.Manager.
            }

            Manager.StartGameServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void StartGameServerRpc()
        {
            if (!gameStarted)
            {
                StartGameClientRpc();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void StartGameClientRpc()
        {
            gameStarted = true;
            GameNetworkManager.Manager.RetrievePlayerList();
        }

        #endregion

        #region Debug

        //[SerializeField] private bool debugBottomPlane;

        //private void OnDrawGizmos()
        //{
        //    if (debugBottomPlane)
        //    {
        //        Gizmos.color = Color.red;
        //        var origin = new Vector3(transform.position.x, -30, transform.position.z);
        //        var debugDist = 100f; // how far the plane with be rendered

        //        var sideward = Vector3.right * debugDist;
        //        var forward = Vector3.forward * debugDist;
        //        for (int offset = -100; offset <= 100; offset += 10)
        //        {
        //            var forwardOffsetVec = new Vector3(0, 0, offset);
        //            Gizmos.DrawLine(origin - sideward + forwardOffsetVec, origin + sideward + forwardOffsetVec);

        //            var sidewardOffsetVec = new Vector3(offset, 0, 0);
        //            Gizmos.DrawLine(origin - forward + sidewardOffsetVec, origin + forward + sidewardOffsetVec);
        //        }
        //    }
        //}

        #endregion

    }
}