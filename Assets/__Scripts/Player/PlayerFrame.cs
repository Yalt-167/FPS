using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

using Controller;
using Unity.Services.Authentication;


namespace GameManagement
{
    /// <summary>
    /// Class that links all the player s classes for better communication between scripts and ease of access
    /// </summary>
    [DefaultExecutionOrder(-98)]
    public sealed class PlayerFrame : NetworkBehaviour
    {

        #region Spawn Logic

        

        #endregion

        [field: SerializeField] public ChampionStats ChampionStats { get; set; }

        private PlayerCombat playerCombat;

        private PlayerMovement playerMovement;
        public bool IsOnline;

        public FixedString64Bytes Name;

        public ClientNetworkTransform ClientNetworkTransform;
        //public HandlePlayerNetworkBehaviour BehaviourHandler;
        public WeaponHandler WeaponHandler;
        public PlayerHealthNetworked Health;

        public FixedString64Bytes PlayerName => playerName.Value;
        private readonly NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);

        private ushort playerIndex;
        public ushort TeamID;

        public bool Alive => Health.Alive;

        public void InitPlayerFrameLocal(string playerName_)
        {
            InitPlayerCommon();

            playerCombat = GetComponent<PlayerCombat>();
            playerCombat.InitPlayerFrame(this);

            playerMovement = GetComponent<PlayerMovement>();
            playerMovement.InitPlayerFrame(this);

            ToggleCursor(false);

            playerName.Value = playerName_;

            if (!TryGetComponent<NetworkObject>(out var _))
            {
                throw new Exception("Does not have a network object");
            }
        }

        public void InitPlayerFrameRemote()
        {
            InitPlayerCommon();
        }

        private void InitPlayerCommon()
        {
            //playerCombat = GetComponent<PlayerCombat>();
            //playerCombat.InitPlayerFrame(this);

            WeaponHandler = GetComponent<WeaponHandler>();
            WeaponHandler.InitPlayerFrame(this);

            Health = GetComponent<PlayerHealthNetworked>();
            Health.InitPlayerFrame(this);

            //playerMovement = GetComponent<Controller.PlayerMovement>();
            //playerMovement.InitPlayerFrame(this);
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
            transform.GetChild(0).GetComponent<Controller.FollowRotationCamera>().enabled = towardOn;
        }

        public void ToggleActionInputs(bool towardOn)
        {
            GetComponent<PlayerCombat>().enabled = towardOn;
            GetComponent<Controller.PlayerMovement>().enabled = towardOn;
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

        #endregion
    }
}
