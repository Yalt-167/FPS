//#define DEBUG_MULTIPLAYER
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Unity.Netcode;

using UnityEditor;

namespace GameManagement
{
    [DefaultExecutionOrder(-99)]
    public sealed class GameNetworkManager : NetworkManager
    {
        #region Managers

        public static GameNetworkManager Manager { get; private set; }

        [SerializeField] private GameObject gameManagerPrefab;
        public GameObject GameManagerInstance { get; private set; }

        [SerializeField] private GameObject teamSelectorPrefab;
        private GameObject teamSelectorInstance;
        public TeamSelector TeamSelectionScreen { get; private set; }

        #endregion

        #region Unity Handled

        private void Awake()
        {
            Manager = this;

            OnServerStarted += CreateManagerInstance;
            OnServerStopped += KillManagerInstance;

        }

        #endregion

        #region Initialization & Cleanup

        private void CreateManagerInstance()
        {
            GameManagerInstance = Instantiate(gameManagerPrefab);
            GameManagerInstance.GetComponent<NetworkObject>().Spawn();
        }

        private void KillManagerInstance(bool _)
        {
            Destroy(GameManagerInstance);
        }


        public static void AssignGameManagerInstance(GameObject gameManagerInstance)
        {
            Manager.GameManagerInstance = gameManagerInstance;
        }

        public static void AssignTeamSelectorInstance(GameObject teamSelectorInstance)
        {
            Manager.teamSelectorInstance = teamSelectorInstance;
            Manager.TeamSelectionScreen = teamSelectorInstance.GetComponent<TeamSelector>();
        }

        #endregion

        #region Network Objects Spawning

        [Rpc(SendTo.Server)]
        public void RequestNetworkObjectClientSpawnServerRpc(GameObject networkObjectPrefab, Vector3 position, Quaternion orientation)
        {
            if (networkObjectPrefab.TryGetComponent<NetworkObject>(out var _))
            {
                SpawnNetworkObjectClientRpc(networkObjectPrefab, position, orientation);
            }
            else
            {
                Debug.LogError("NetworkPrefab is missing a NetworkObject component.");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SpawnNetworkObjectClientRpc(GameObject networkObjectPrefab, Vector3 position, Quaternion orientation)
        {
            // Actually spawn the obj on the network
            Instantiate(networkObjectPrefab, position, orientation).GetComponent<NetworkObject>().Spawn();
        }

        #endregion

        #region Network Objects Monitoring

        [MenuItem("Developer/Debug/DebugNetworkObjects")]
        public static void DebugNetworkObjects()
        {
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                Debug.Log($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
            }
        }

        [MenuItem("Developer/Debug/DebugNetworkBehaviours")]
        public static void DebugNetworkBehaviours()
        {
            foreach (KeyValuePair<ulong, NetworkObject> element in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                Debug.Log($"NetworkObjectId: {element.Key} | Obj: {element.Value.name}");
                foreach (NetworkBehaviour networkBehaviour in element.Value.GetComponents<NetworkBehaviour>())
                {
                    Debug.Log($"Behaviour: {networkBehaviour.GetType()} | NetworkBehaviourId: {networkBehaviour.NetworkBehaviourId}");
                }
            }
        }

        #endregion

        #region Team Selection Menu

        public static void SpawnTeamSelectionMenuOnAllClients(ushort teamsCount, ushort teamsSize)
        {
            Manager.SpawnTeamSelectionMenuInternal(teamsCount, teamsSize);
        }

        private void SpawnTeamSelectionMenuInternal(ushort teamsCount, ushort teamsSize)
        {
            if (teamSelectorInstance == null)
            {
                teamSelectorInstance = Instantiate(teamSelectorPrefab);
                teamSelectorInstance.GetComponent<NetworkObject>().Spawn();
            }

            TeamSelectionScreen = teamSelectorInstance.GetComponent<TeamSelector>();
            TeamSelectionScreen.SetDataServerRpc(teamsCount, teamsSize);
            TeamSelectionScreen.ToggleTeamSelectionScreenServerRpc(towardOn__: true);
        }

        #endregion
    }
}


public struct NetworkSerializableString : INetworkSerializable
{
    public string Value;
    public NetworkSerializableString(string value) { Value = value; }
    public static implicit operator string(NetworkSerializableString value) { return value.Value; }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }
    public readonly override string ToString()
    {
        return Value;
    }
}

//public struct NetworkSerializableData<T> : INetworkSerializable where T : struct
//{
//    public T Data;
//    public NetworkSerializableData(T value) { Data = value; }
//    public static implicit operator T(NetworkSerializableData<T> value) { return value.Data; }
//    public void NetworkSerialize<Type>(BufferSerializer<Type> serializer) where Type : IReaderWriter
//    {
//        serializer.SerializeValue(ref Data);
//    }
//    public override string ToString()
//    {
//        return Data.ToString();
//    }
//}