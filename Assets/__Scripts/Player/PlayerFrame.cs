using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Netcode;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Services.Authentication;

using Controller;



namespace GameManagement
{
    /// <summary>
    /// Class that links all the player s classes for better communication between scripts and ease of access
    /// </summary>
    [DefaultExecutionOrder(-98)]
    public sealed class PlayerFrame : NetworkBehaviour
    {

        #region Spawn Logic

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                EnablePlayerServerRpc(AuthenticationService.Instance.Profile);
            }
            else
            {
                InitPlayerFrameRemote();
            }

            ManageFiles(IsOwner);
        }

        [Rpc(SendTo.Server)]
        private void EnablePlayerServerRpc(string playerName)
        {
            EnablePlayerClientRpc(playerName);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EnablePlayerClientRpc(string playerName)
        {
            if (IsOwner)
            {
                InitPlayerFrameLocal(playerName);
            }
            else
            {
                InitPlayerFrameRemote();
            }
        }

        #endregion

        #region Despawn Logic

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        #endregion

        #region Manage Files

        [Serializable]
        public struct BehaviourGatherer
        {
            public List<Component> componentsToKill;
            public List<GameObject> gameObjectsToKill;
            public List<Component> componentsToDisable;
            public List<GameObject> gameObjectsToDisable;
        }

        [SerializeField] private BehaviourGatherer handleOnRemotePlayer;

        [Space(8)]
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
                Destroy(component);
            }

            foreach (var gameObj in handleOnLocalPlayer.gameObjectsToKill)
            {
                Destroy(gameObj);
            }

            foreach (var component in handleOnLocalPlayer.componentsToDisable)
            {
                if (component is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
            }

            foreach (var gameObj in handleOnLocalPlayer.gameObjectsToDisable)
            {
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
                if (component is Behaviour behaviour)
                {
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

        [field: Space(12)]
        [field: SerializeField] public ChampionStats ChampionStats { get; set; }
        [HideInInspector] public PlayerCombat Combat;
        [HideInInspector] public PlayerMovement Movement;
        [HideInInspector] public ClientNetworkTransform ClientNetworkTransform;
        [HideInInspector] public WeaponHandler WeaponHandler;
        [HideInInspector] public PlayerHealthNetworked Health;

        public FixedString64Bytes Name => playerName.Value;
        private readonly NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
        private readonly NetworkVariable<bool> playerNameSetOnOwner = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);



        private ushort playerIndex;
        //[HideInInspector]
        public ushort TeamID;
        [HideInInspector] public bool IsOnline;

        public bool Alive => Health.Alive;

        public void InitPlayerFrameLocal(string playerName_)
        {
            InitPlayerCommon();

            Combat = GetComponent<PlayerCombat>();
            Combat.InitPlayerFrame(this);

            Movement = GetComponent<PlayerMovement>();
            Movement.InitPlayerFrame(this);

            ToggleCursor(false);

            playerName.Value = playerName_;
            //playerName.OnValueChanged +=
            playerNameSetOnOwner.Value = true;

            if (!TryGetComponent<NetworkObject>(out var _))
            {
                throw new Exception("Does not have a network object");
            }

            gameObject.name = playerName_;

            transform.position = Vector3.up * 5;
        }

        public void InitPlayerFrameRemote()
        {
            InitPlayerCommon();

            StartCoroutine(SetPlayerNameInHierarchy());
        }

        public IEnumerator SetPlayerNameInHierarchy()
        {
            yield return new WaitUntil(() => playerNameSetOnOwner.Value);

            gameObject.name = playerName.Value.ToString();
        }


        private void InitPlayerCommon()
        {
            WeaponHandler = GetComponent<WeaponHandler>();
            WeaponHandler.InitPlayerFrame(this);

            Health = GetComponent<PlayerHealthNetworked>();
            Health.InitPlayerFrame(this);

            ClientNetworkTransform = GetComponent<ClientNetworkTransform>();
        }

        public string GetInfos()
        {
            return $"{{Player: {playerName.Value} | Team: {TeamID}}}";
        }

        #region Toggle Controls

        public void ToggleGameControls(bool towardOn)
        {
            ToggleCameraInputs(towardOn);
            ToggleActionInputs(towardOn);
        }

        public void ToggleCameraInputs(bool towardOn)
        {
            transform.GetChild(0).GetComponent<FollowRotationCamera>().enabled = towardOn;
        }

        public void ToggleActionInputs(bool towardOn)
        {
            GetComponent<PlayerCombat>().enabled = towardOn;
            GetComponent<PlayerMovement>().enabled = towardOn;
        }

        public void ToggleCursor(bool towardOn)
        {
            Cursor.lockState = towardOn ? CursorLockMode.None : CursorLockMode.Locked; //
            Cursor.visible = !towardOn;
            // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently
        }

        #endregion

        #region Team Logic

        [Rpc(SendTo.Server)]
        public void RequestSetTeamServerRpc(ushort teamID)
        {
            SetTeamClientRpc(teamID);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SetTeamClientRpc(ushort teamID)
        {
            TeamID = teamID;
        }

        public void SetTeam(ushort teamID)
        {
            TeamID = teamID;
        }

        #endregion
    }
}
