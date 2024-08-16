#define LOG_EVENTS
#define LOG_METHOD_CALLS

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;

using static DebugUtility;



namespace GameManagement
{
    public sealed class PlayerSpawnHandler : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                EnablePlayerServerRpc(AuthenticationService.Instance.Profile);
            }

            ManageFiles();
            enabled = false;
        }

        [Rpc(SendTo.Server)]
        private void EnablePlayerServerRpc(string playerName)
        {
            ActivatePlayerPackArchitectureClientRpc(playerName);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ActivatePlayerPackArchitectureClientRpc(string playerName)
        {
            if (IsOwner)
            {
                GetComponent<PlayerFrame>().InitPlayerFrameLocal(playerName);
            }
            else
            {
                GetComponent<PlayerFrame>().InitPlayerFrameRemote();
            }
        }

        #region Handle Files

        [Serializable]
        public struct BehaviourGatherer
        {
            public List<Component> componentsToKill;
            public List<GameObject> gameObjectsToKill;
            public List<Component> componentsToDisable;
            public List<GameObject> gameObjectsToDisable;
        }

        [SerializeField] private BehaviourGatherer handleOnRemotePlayer;

        [Space(20)]
        [SerializeField] private BehaviourGatherer handleOnLocalPlayer;

        [Rpc(SendTo.Server)]
        public void ManageFilesAllServerRpc()
        {
            ManageFilesAllClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ManageFilesAllClientRpc()
        {
            var relevantStruct = IsOwner ? handleOnRemotePlayer : handleOnLocalPlayer;

            foreach (var component in relevantStruct.componentsToKill)
            {
                Destroy(component);
            }

            foreach (var gameObj in relevantStruct.gameObjectsToKill)
            {
                Destroy(gameObj);
            }

            foreach (var component in relevantStruct.componentsToDisable)
            {
                if (component.TryGetComponent<MonoBehaviour>(out var comp))
                {
                    comp.enabled = false;
                }
            }

            foreach (var gameObj in relevantStruct.gameObjectsToDisable)
            {
                gameObj.SetActive(false);
            }
        }

        public void ManageFiles()
        {
            ManageFiles(IsOwner);
        }

        public void ManageFiles(bool isOwner)
        {
            _ = isOwner ? ManageLocalPlayerFiles() : ManageRemotePlayerFiles();
        }

        public object ManageLocalPlayerFiles()
        {
            foreach (var component in handleOnLocalPlayer.componentsToKill)
            {
                //print($"Destroyed {component.name}");
                Destroy(component);
            }

            foreach (var gameObj in handleOnLocalPlayer.gameObjectsToKill)
            {
                //print($"Destroyed {gameObj.name}");
                Destroy(gameObj);
            }

            foreach (var component in handleOnLocalPlayer.componentsToDisable)
            {
                //print($"Tried deactivating {component.name}");
                if (component is Behaviour behaviour)
                {
                    //print("managed to");
                    behaviour.enabled = false;
                }
            }

            foreach (var gameObj in handleOnLocalPlayer.gameObjectsToDisable)
            {
                //print($"Deactivated {gameObj.name}");
                gameObj.SetActive(false);
            }

            return null;
        }

        public object ManageRemotePlayerFiles()
        {
            foreach (var component in handleOnRemotePlayer.componentsToKill)
            {
                Destroy(component);
            }

            foreach (var gameObj in handleOnRemotePlayer.gameObjectsToKill)
            {
                Destroy(gameObj);
            }

            foreach (var component in handleOnRemotePlayer.componentsToDisable)
            {
                //print($"Tried deactivating {component.name}");
                if (component is Behaviour behaviour)
                {
                    //print("managed to");
                    behaviour.enabled = false;
                }
            }

            foreach (var gameObj in handleOnRemotePlayer.gameObjectsToDisable)
            {
                gameObj.SetActive(false);
            }

            return null;
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


// make the player prefab a child of the login instead and use its NetworkObject