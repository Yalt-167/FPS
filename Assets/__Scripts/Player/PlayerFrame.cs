using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using Unity.Netcode;

using Controller;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Collections;


namespace GameManagement
{
    /// <summary>
    /// Class that links all the player s classes for better communication between scripts and ease of access
    /// </summary>
    [DefaultExecutionOrder(-98)]
    public sealed class PlayerFrame : NetworkBehaviour
    {
        [field: SerializeField] public ChampionStats ChampionStats { get; set; }

        private PlayerCombat playerCombat;
        private WeaponHandler weaponHandler;

        private PlayerHealthNetworked playerHealth;

        private PlayerMovement playerMovement;

        public FixedString64Bytes PlayerName => playerName.Value;
        private readonly NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);


        private ushort playerIndex;
        public ushort TeamID;

        public bool Alive => playerHealth.Alive;

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

            weaponHandler = GetComponent<WeaponHandler>();
            weaponHandler.InitPlayerFrame(this);

            playerHealth = GetComponent<PlayerHealthNetworked>();
            playerHealth.InitPlayerFrame(this);

            //playerMovement = GetComponent<Controller.PlayerMovement>();
            //playerMovement.InitPlayerFrame(this);
        }

        public NetworkedPlayerPrimitive AsPrimitive(/*ulong requestingClientID*/)
        {
            return new(playerName.Value, NetworkObjectId);
        }

        public NetworkedPlayer AsNetworkedPlayer(ushort index)
        {
            playerIndex = index;

            return new NetworkedPlayer(playerName.Value, 0, NetworkObject, GetComponent<ClientNetworkTransform>(), weaponHandler, playerHealth);
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
