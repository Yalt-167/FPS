using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Netcode;
using Unity.Collections;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Services.Authentication;

using Controller;
using SaveAndLoad;
using System.Reflection;


namespace GameManagement
{
    /// <summary>
    /// Class that links all the player s classes for better communication between scripts and ease of access
    /// </summary>
    [DefaultExecutionOrder(-98)]
    public sealed class PlayerFrame : NetworkBehaviour
    {
        public static PlayerFrame LocalPlayer { get; private set; }

        #region Spawn Logic

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                EnablePlayerServerRpc(AuthenticationService.Instance.Profile);

                if (IsOwner)
                {
                    CallOnPlayerScriptsRecursively<IHaveSomethingToSave>("Load");
                }
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
            if (IsOwner)
            {
                CallOnPlayerScriptsRecursively<IHaveSomethingToSave>("Save");
            }
            
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

        private void CallOnPlayerScriptsRecursively<T>(string methodName, Transform transform_ = null)
        {
            transform_ = transform_ ?? transform;

            MethodInfo methodInfo = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            foreach (var monoBehaviour in transform_.GetComponents<MonoBehaviour>())
            {
                if (monoBehaviour is T monoBehaviourAsT)
                {
                    methodInfo.Invoke(monoBehaviourAsT, new object[] { });
                }
            }

            for (int childIndex = 0; childIndex < transform_.childCount; childIndex++)
            {
                CallOnPlayerScriptsRecursively<T>(methodName, transform_.GetChild(childIndex));
            }
        }

        private void CallOnPlayerScriptsRecursively<T>(string methodName, object[] params_, Transform transform_ = null)
        {
            transform_ = transform_ ?? transform;

            MethodInfo methodInfo = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            foreach (var monoBehaviour in transform_.GetComponents<MonoBehaviour>())
            {
                if (monoBehaviour is T monoBehaviourAsT)
                {
                    methodInfo.Invoke(monoBehaviourAsT, params_);
                }
            }

            for (int childIndex = 0; childIndex < transform_.childCount; childIndex++)
            {
                CallOnPlayerScriptsRecursively<T>(methodName, params_, transform_.GetChild(childIndex));
            }
        }

        private void TryCallOnPlayerMonoBehavioursRecursively(string methodName, object[] parameters, Transform transform_ = null)
        {
            transform_ = transform_ ?? transform;

            foreach (var monoBehaviour in transform_.GetComponents<MonoBehaviour>())
            {
                MethodInfo methodInfo = monoBehaviour.GetType().GetMethod(methodName);

                if (methodInfo == null) { continue; }

                methodInfo.Invoke(monoBehaviour, parameters);
            }

            for (int childIndex = 0; childIndex < transform_.childCount; childIndex++)
            {
                TryCallOnPlayerMonoBehavioursRecursively(methodName, parameters, transform_.GetChild(childIndex));
            }
        }

        #endregion

        [field: Space(12)]
        [field: SerializeField] public ChampionStats ChampionStats { get; set; }
        [HideInInspector] public PlayerCombat Combat;
        [HideInInspector] public PlayerMovement Movement;
        [HideInInspector] public ClientNetworkTransform ClientNetworkTransform;
        [HideInInspector] public WeaponHandler WeaponHandler;
        [HideInInspector] public PlayerHealthNetworked Health;
        [HideInInspector] private Camera playerCamera;
        [HideInInspector] private FollowRotationCamera followRotationCamera;
        [HideInInspector] private Transform rootTransformHUD;

        public FixedString64Bytes Name => playerName.Value;
        private readonly NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);



        public ushort PlayerIndex;
        //[HideInInspector]
        public ushort TeamNumber { get; private set; }
        [HideInInspector] public bool IsOnline;

        public bool Alive => Health.Alive;

        public void InitPlayerFrameLocal(string playerName_)
        {
            InitPlayerCommon();

            Combat = GetComponent<PlayerCombat>();

            Movement = GetComponent<PlayerMovement>();

            ToggleCursor(false);

            playerName.Value = playerName_;

            if (!TryGetComponent<NetworkObject>(out var _))
            {
                throw new Exception("Does not have a network object");
            }

            gameObject.name = playerName_;

            rootTransformHUD = transform.GetChild(4);

            transform.position = Vector3.up * 5;

            SetMenuInputMode();
            ToggleHUD(false);   

            LocalPlayer = this;
        }

        public void InitPlayerFrameRemote()
        {
            InitPlayerCommon();

            StartCoroutine(SetPlayerNameInHierarchy());
        }

        public IEnumerator SetPlayerNameInHierarchy()
        {
            yield return new WaitUntil(() => !string.IsNullOrEmpty(playerName.Value.ToString()));

            gameObject.name = playerName.Value.ToString();
        }

        private void InitPlayerCommon()
        {
            WeaponHandler = GetComponent<WeaponHandler>();

            Health = GetComponent<PlayerHealthNetworked>();

            ClientNetworkTransform = GetComponent<ClientNetworkTransform>();

            playerCamera = transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Camera>();
            followRotationCamera = transform.GetChild(0).GetComponent<FollowRotationCamera>();
        }

        public string GetInfos()
        {
            return $"{{Player: {playerName.Value} | Team: {TeamNumber}}}";
        }

        #region Toggle Controls

        public void ToggleGameControls(bool towardOn)
        {
            ToggleCameraInputs(towardOn);
            ToggleActionInputs(towardOn);
        }

        public void ToggleCameraInputs(bool towardOn)
        {
            followRotationCamera.enabled = towardOn;
        }

        public void ToggleActionInputs(bool towardOn)
        {
            if (!IsOwner)
            {
                Debug.LogError("ToggleActionInputs was called on a remote player");
                return;
            }

            Combat.enabled = towardOn;
            Movement.enabled = towardOn;
        }

        public void ToggleCursor(bool towardOn)
        {
            Cursor.lockState = towardOn ? CursorLockMode.Confined : CursorLockMode.Locked; //
            Cursor.visible = towardOn;
            // those two may cause issues when destroying the script on remote players // chekc when loggin concurrently
        }

        public void SetMenuInputMode()
        {
            ToggleGameControls(towardOn: false);
            ToggleCursor(towardOn: true);
        }

        public void SetGameplayInputMode()
        {
            ToggleCursor(towardOn: false);
            ToggleGameControls(towardOn: true);
        }

        public void ToggleCamera(bool towardOn)
        {
            playerCamera.enabled = towardOn;
        }

        public void ToggleHUD(bool towardOn)
        {
            rootTransformHUD.gameObject.SetActive(towardOn);
        }

        #endregion

        #region Team Logic

        public void SetTeam(ushort teamNumber)
        {
            TeamNumber = teamNumber;
        }

        #endregion
    }
}
